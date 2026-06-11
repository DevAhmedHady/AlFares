using BuildingBlocks.Authentication;
using BuildingBlocks.Endpoints;
using BuildingBlocks.Export;
using BuildingBlocks.Grids;
using BuildingBlocks.Http;
using BuildingBlocks.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Todos.Contracts;
using Todos.Domain;
using Todos.Features;
using Todos.Persistence;

namespace Todos.Endpoints;

public sealed class TodosEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup(TodosRoutes.Base).WithTags(TodosRoutes.Tag);
        g.MapPost(
                "",
                async (CreateTodoRequest r, IDispatcher d, CancellationToken ct) =>
                    (
                        await d.Send<TodoResponse>(
                            new CreateTodoCommand(
                                r.Title,
                                r.DueDate,
                                r.DueTime,
                                r.Priority,
                                r.Notes
                            ),
                            ct
                        )
                    ).ToHttpResult(v => Results.Created($"{TodosRoutes.Base}/{v.Id}", v))
            )
            .RequirePermission("todos.write");
        g.MapPut(
                "/{id:guid}",
                async (Guid id, UpdateTodoRequest r, IDispatcher d, CancellationToken ct) =>
                    (
                        await d.Send<TodoResponse>(
                            new UpdateTodoCommand(
                                id,
                                r.Title,
                                r.DueDate,
                                r.DueTime,
                                r.Priority,
                                r.Notes
                            ),
                            ct
                        )
                    ).ToHttpResult()
            )
            .RequirePermission("todos.write");
        g.MapPut(
                "/{id:guid}/status",
                async (Guid id, ChangeTodoStatusRequest r, IDispatcher d, CancellationToken ct) =>
                    (
                        await d.Send<TodoResponse>(new ChangeTodoStatusCommand(id, r.Status), ct)
                    ).ToHttpResult()
            )
            .RequirePermission("todos.write");
        g.MapDelete(
                "/{id:guid}",
                async (Guid id, IDispatcher d, CancellationToken ct) =>
                    (await d.Send<bool>(new DeleteTodoCommand(id), ct)).ToHttpResult(_ =>
                        Results.NoContent()
                    )
            )
            .RequirePermission("todos.delete");
        g.MapGet(
                "/{id:guid}",
                async (Guid id, IDispatcher d, CancellationToken ct) =>
                    (await d.Send<TodoResponse>(new GetTodoByIdQuery(id), ct)).ToHttpResult()
            )
            .RequirePermission("todos.read");
        g.MapPost(
                "/grid",
                async (GridQuery r, IDispatcher d, CancellationToken ct) =>
                    (
                        await d.Send<PagedResult<TodoResponse>>(new GetTodosGridQuery(r), ct)
                    ).ToHttpResult()
            )
            .RequirePermission("todos.read");
        g.MapPost("/export", ExportAsync).RequirePermission("todos.export");
    }

    private static async Task<IResult> ExportAsync(
        TodoExportRequest r,
        IMainDbContext db,
        IGridExporterFactory f,
        CancellationToken ct
    )
    {
        var q = db.Set<TodoItem>().AsNoTracking().ApplyGridQuery(r.Grid, TodoGrid.Fields);
        if (q.IsFailure)
            return q.ToHttpResult();
        var rows = await q
            .Value.Take(GridExportLimits.MaxRows)
            .Select(TodoGrid.Projection)
            .ToListAsync(ct);
        ExportColumn[] cols =
        [
            new("Title", "المهمة", GridFieldType.Text),
            new("DueDate", "الاستحقاق", GridFieldType.Date),
            new("DueTime", "الوقت", GridFieldType.Text),
            new("Status", "الحالة", GridFieldType.Enum),
            new("Priority", "الأولوية", GridFieldType.Enum),
        ];
        var bytes = f.For(r.Format).Export(rows, cols, "المهام");
        return Results.File(
            bytes,
            r.Format == ExportFormat.Xlsx
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "application/pdf",
            $"todos.{(r.Format == ExportFormat.Xlsx ? "xlsx" : "pdf")}"
        );
    }
}
