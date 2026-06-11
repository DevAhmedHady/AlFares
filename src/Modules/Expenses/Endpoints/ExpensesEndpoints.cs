using BuildingBlocks.Authentication;
using BuildingBlocks.Endpoints;
using BuildingBlocks.Export;
using BuildingBlocks.Grids;
using BuildingBlocks.Http;
using BuildingBlocks.Messaging;
using Expenses.Contracts;
using Expenses.Domain;
using Expenses.Features;
using Expenses.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Expenses.Endpoints;

/// <summary>Expense endpoints.</summary>
public sealed class ExpensesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup(ExpensesRoutes.Base).WithTags(ExpensesRoutes.Tag);
        g.MapPost(
                "",
                async (CreateExpenseRequest r, IDispatcher d, CancellationToken ct) =>
                    (
                        await d.Send<ExpenseResponse>(
                            new CreateExpenseCommand(
                                r.ExpenseTypeId,
                                r.Amount,
                                r.Date,
                                r.Payee,
                                r.Notes,
                                r.OwnerType,
                                r.OwnerId
                            ),
                            ct
                        )
                    ).ToHttpResult(v => Results.Created($"{ExpensesRoutes.Base}/{v.Id}", v))
            )
            .RequirePermission("expenses.write");
        g.MapPut(
                "/{id:guid}",
                async (Guid id, UpdateExpenseRequest r, IDispatcher d, CancellationToken ct) =>
                    (
                        await d.Send<ExpenseResponse>(
                            new UpdateExpenseCommand(
                                id,
                                r.ExpenseTypeId,
                                r.Amount,
                                r.Date,
                                r.Payee,
                                r.Notes,
                                r.OwnerType,
                                r.OwnerId
                            ),
                            ct
                        )
                    ).ToHttpResult()
            )
            .RequirePermission("expenses.write");
        g.MapDelete(
                "/{id:guid}",
                async (Guid id, IDispatcher d, CancellationToken ct) =>
                    (await d.Send<bool>(new DeleteExpenseCommand(id), ct)).ToHttpResult(_ =>
                        Results.NoContent()
                    )
            )
            .RequirePermission("expenses.delete");
        g.MapGet(
                "/{id:guid}",
                async (Guid id, IDispatcher d, CancellationToken ct) =>
                    (await d.Send<ExpenseResponse>(new GetExpenseByIdQuery(id), ct)).ToHttpResult()
            )
            .RequirePermission("expenses.read");
        g.MapPost(
                "/grid",
                async (GridQuery r, IDispatcher d, CancellationToken ct) =>
                    (
                        await d.Send<PagedResult<ExpenseResponse>>(new GetExpensesGridQuery(r), ct)
                    ).ToHttpResult()
            )
            .RequirePermission("expenses.read");
        g.MapPost("/export", ExportAsync).RequirePermission("expenses.export");
        g.MapGet("/types", ListTypes).RequirePermission("expenses.read");
        g.MapPost("/types", CreateType).RequirePermission("expenses.write");
        g.MapPut("/types/{id:guid}", UpdateType).RequirePermission("expenses.write");
        g.MapDelete("/types/{id:guid}", DeleteType).RequirePermission("expenses.delete");
        g.MapPost("/types/grid", TypeGrid).RequirePermission("expenses.read");
    }

    private static async Task<IResult> ListTypes(
        ExpenseScope? scope,
        IMainDbContext db,
        CancellationToken ct
    )
    {
        var q = db.Set<ExpenseType>().AsNoTracking().Where(x => x.IsActive);
        if (scope.HasValue)
            q = q.Where(x => x.Scope == scope);
        return Results.Ok(
            await q.OrderBy(x => x.Name)
                .Select(x => new ExpenseTypeResponse(x.Id, x.Name, x.Scope, x.IsActive))
                .ToListAsync(ct)
        );
    }

    private static async Task<IResult> CreateType(
        ExpenseTypeRequest r,
        IMainDbContext db,
        CancellationToken ct
    )
    {
        var x = ExpenseType.Create(r.Name, r.Scope);
        if (x.IsFailure)
            return Results.BadRequest(x.Error);
        db.Set<ExpenseType>().Add(x.Value);
        await db.SaveChangesAsync(ct);
        return Results.Created(
            $"{ExpensesRoutes.Base}/types/{x.Value.Id}",
            new ExpenseTypeResponse(x.Value.Id, x.Value.Name, x.Value.Scope, x.Value.IsActive)
        );
    }

    private static async Task<IResult> UpdateType(
        Guid id,
        ExpenseTypeRequest r,
        IMainDbContext db,
        CancellationToken ct
    )
    {
        var x = await db.Set<ExpenseType>().FindAsync([id], ct);
        if (x is null)
            return Results.NotFound();
        var result = x.Update(r.Name, r.Scope, r.IsActive);
        if (result.IsFailure)
            return Results.BadRequest(result.Error);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new ExpenseTypeResponse(x.Id, x.Name, x.Scope, x.IsActive));
    }

    private static async Task<IResult> DeleteType(Guid id, IMainDbContext db, CancellationToken ct)
    {
        var x = await db.Set<ExpenseType>().FindAsync([id], ct);
        if (x is null)
            return Results.NotFound();
        if (await db.Set<Expense>().AnyAsync(e => e.ExpenseTypeId == id, ct))
            return Results.Conflict();
        db.Remove(x);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> TypeGrid(
        GridQuery r,
        IMainDbContext db,
        CancellationToken ct
    )
    {
        var map = new GridFieldMap<ExpenseType>(
            new[]
            {
                (
                    new GridField("name", "الاسم", GridFieldType.Text, true),
                    (System.Linq.Expressions.Expression<Func<ExpenseType, object?>>)(x => x.Name)
                ),
                (new GridField("scope", "النطاق", GridFieldType.Enum, false), x => x.Scope),
                (new GridField("isActive", "نشط", GridFieldType.Boolean, false), x => x.IsActive),
            }
        );
        var q = db.Set<ExpenseType>().AsNoTracking().ApplyGridQuery(r, map);
        if (q.IsFailure)
            return q.ToHttpResult();
        return Results.Ok(
            await q.Value.ToPagedResultAsync(
                r,
                x => new ExpenseTypeResponse(x.Id, x.Name, x.Scope, x.IsActive),
                ct
            )
        );
    }

    private static async Task<IResult> ExportAsync(
        ExpenseExportRequest r,
        IMainDbContext db,
        IGridExporterFactory factory,
        CancellationToken ct
    )
    {
        var q = ExpenseGrid.Query(db).ApplyGridQuery(r.Grid, ExpenseGrid.Fields);
        if (q.IsFailure)
            return q.ToHttpResult();
        var rows = await q
            .Value.Take(GridExportLimits.MaxRows)
            .Select(ExpenseGrid.Projection)
            .ToListAsync(ct);
        ExportColumn[] cols =
        [
            new("ExpenseTypeName", "نوع المصروف", GridFieldType.Text),
            new("Amount", "المبلغ", GridFieldType.Number),
            new("Date", "التاريخ", GridFieldType.Date),
            new("Payee", "المستفيد", GridFieldType.Text),
        ];
        var bytes = factory.For(r.Format).Export(rows, cols, "المصروفات");
        return Results.File(
            bytes,
            r.Format == ExportFormat.Xlsx
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "application/pdf",
            $"expenses.{(r.Format == ExportFormat.Xlsx ? "xlsx" : "pdf")}"
        );
    }
}
