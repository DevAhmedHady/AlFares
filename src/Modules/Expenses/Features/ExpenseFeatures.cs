using System.Linq.Expressions;
using BuildingBlocks.Grids;
using BuildingBlocks.Ledger;
using BuildingBlocks.Messaging;
using Expenses.Contracts;
using Expenses.Domain;
using Expenses.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Expenses.Features;

/// <summary>Create command.</summary>
public sealed record CreateExpenseCommand(
    Guid ExpenseTypeId,
    decimal Amount,
    DateOnly Date,
    string Payee,
    string? Notes,
    OwnerType OwnerType,
    Guid? OwnerId
) : ICommand<ExpenseResponse>;

/// <summary>Update command.</summary>
public sealed record UpdateExpenseCommand(
    Guid Id,
    Guid ExpenseTypeId,
    decimal Amount,
    DateOnly Date,
    string Payee,
    string? Notes,
    OwnerType OwnerType,
    Guid? OwnerId
) : ICommand<ExpenseResponse>;

/// <summary>Delete command.</summary>
public sealed record DeleteExpenseCommand(Guid Id) : ICommand<bool>;

/// <summary>Get query.</summary>
public sealed record GetExpenseByIdQuery(Guid Id) : IQuery<ExpenseResponse>;

/// <summary>Grid query.</summary>
public sealed record GetExpensesGridQuery(GridQuery Grid) : IQuery<PagedResult<ExpenseResponse>>;

/// <summary>Create validator.</summary>
public sealed class CreateExpenseValidator : AbstractValidator<CreateExpenseCommand>
{
    /// <summary>Rules.</summary>
    public CreateExpenseValidator()
    {
        RuleFor(x => x.ExpenseTypeId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Payee).NotEmpty();
    }
}

/// <summary>Expense grid row. Init-only members (object-initializer projection) keep the join
/// transparent to EF Core, so grid filtering/sorting still translates to SQL. A positional-constructor
/// projection is opaque to a subsequent Where/OrderBy and throws "could not be translated".</summary>
public sealed record ExpenseGridRow
{
    public Guid Id { get; init; }
    public Guid ExpenseTypeId { get; init; }
    public string ExpenseTypeName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateOnly Date { get; init; }
    public string Payee { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public OwnerType OwnerType { get; init; }
    public Guid? OwnerId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

/// <summary>Expense grid.</summary>
public static class ExpenseGrid
{
    public static readonly GridFieldMap<ExpenseGridRow> Fields = new(
        new[]
        {
            (
                new GridField("expenseTypeId", "نوع المصروف", GridFieldType.Text, false),
                (Expression<Func<ExpenseGridRow, object?>>)(x => x.ExpenseTypeId)
            ),
            (
                new GridField(
                    "expenseTypeName",
                    "نوع المصروف",
                    GridFieldType.Text,
                    true,
                    Chartable: true
                ),
                (Expression<Func<ExpenseGridRow, object?>>)(x => x.ExpenseTypeName)
            ),
            (
                new GridField("amount", "المبلغ", GridFieldType.Number, false, Chartable: true),
                (Expression<Func<ExpenseGridRow, object?>>)(x => x.Amount)
            ),
            (
                new GridField("date", "التاريخ", GridFieldType.Date, false, Chartable: true),
                (Expression<Func<ExpenseGridRow, object?>>)(x => x.Date)
            ),
            (
                new GridField("payee", "المستفيد", GridFieldType.Text, true),
                (Expression<Func<ExpenseGridRow, object?>>)(x => x.Payee)
            ),
            (
                new GridField("ownerType", "نوع المالك", GridFieldType.Enum, false),
                (Expression<Func<ExpenseGridRow, object?>>)(x => x.OwnerType)
            ),
            (
                new GridField("ownerId", "المالك", GridFieldType.Text, false),
                (Expression<Func<ExpenseGridRow, object?>>)(x => x.OwnerId)
            ),
            (
                new GridField("createdAt", "تاريخ الإنشاء", GridFieldType.Date, false),
                (Expression<Func<ExpenseGridRow, object?>>)(x => x.CreatedAtUtc)
            ),
        }
    );

    public static IQueryable<ExpenseGridRow> Query(IMainDbContext db) =>
        from e in db.Set<Expense>().AsNoTracking()
        join t in db.Set<ExpenseType>().AsNoTracking() on e.ExpenseTypeId equals t.Id
        orderby e.Date descending //default ordering
        select new ExpenseGridRow
        {
            Id = e.Id,
            ExpenseTypeId = e.ExpenseTypeId,
            ExpenseTypeName = t.Name,
            Amount = e.Amount,
            Date = e.Date,
            Payee = e.Payee,
            Notes = e.Notes,
            OwnerType = e.OwnerType,
            OwnerId = e.OwnerId,
            CreatedAtUtc = e.CreatedAtUtc,
            UpdatedAtUtc = e.UpdatedAtUtc,
        };

    public static readonly Expression<Func<ExpenseGridRow, ExpenseResponse>> Projection = x =>
        new(
            x.Id,
            x.ExpenseTypeId,
            x.ExpenseTypeName,
            x.Amount,
            x.Date,
            x.Payee,
            x.Notes,
            x.OwnerType,
            x.OwnerId,
            x.CreatedAtUtc,
            x.UpdatedAtUtc
        );
}

/// <summary>Create handler.</summary>
public sealed class CreateExpenseHandler(IExpenseRepository repo, IMainDbContext db)
    : ICommandHandler<CreateExpenseCommand, ExpenseResponse>
{
    public async Task<Result<ExpenseResponse>> Handle(CreateExpenseCommand c, CancellationToken ct)
    {
        var t = await db.Set<ExpenseType>().FindAsync([c.ExpenseTypeId], ct);
        if (t is null)
            return ExpenseErrors.TypeRequired;
        var e = Expense.Create(
            c.ExpenseTypeId,
            c.Amount,
            c.Date,
            c.Payee,
            c.Notes,
            c.OwnerType,
            c.OwnerId
        );
        if (e.IsFailure)
            return e.Error;
        repo.Add(e.Value);
        await repo.SaveChangesAsync(ct);
        return new ExpenseResponse(
            e.Value.Id,
            t.Id,
            t.Name,
            e.Value.Amount,
            e.Value.Date,
            e.Value.Payee,
            e.Value.Notes,
            e.Value.OwnerType,
            e.Value.OwnerId,
            e.Value.CreatedAtUtc,
            e.Value.UpdatedAtUtc
        );
    }
}

/// <summary>Update handler.</summary>
public sealed class UpdateExpenseHandler(IExpenseRepository repo, IMainDbContext db)
    : ICommandHandler<UpdateExpenseCommand, ExpenseResponse>
{
    public async Task<Result<ExpenseResponse>> Handle(UpdateExpenseCommand c, CancellationToken ct)
    {
        var e = await repo.GetByIdAsync(c.Id, ct);
        if (e is null)
            return ExpenseErrors.NotFound(c.Id);
        var t = await db.Set<ExpenseType>().FindAsync([c.ExpenseTypeId], ct);
        if (t is null)
            return ExpenseErrors.TypeRequired;
        var r = e.Update(
            c.ExpenseTypeId,
            c.Amount,
            c.Date,
            c.Payee,
            c.Notes,
            c.OwnerType,
            c.OwnerId
        );
        if (r.IsFailure)
            return r.Error;
        await repo.SaveChangesAsync(ct);
        return new ExpenseResponse(
            e.Id,
            t.Id,
            t.Name,
            e.Amount,
            e.Date,
            e.Payee,
            e.Notes,
            e.OwnerType,
            e.OwnerId,
            e.CreatedAtUtc,
            e.UpdatedAtUtc
        );
    }
}

/// <summary>Delete handler.</summary>
public sealed class DeleteExpenseHandler(IExpenseRepository repo)
    : ICommandHandler<DeleteExpenseCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteExpenseCommand c, CancellationToken ct)
    {
        var e = await repo.GetByIdAsync(c.Id, ct);
        if (e is null)
            return ExpenseErrors.NotFound(c.Id);
        repo.Remove(e);
        await repo.SaveChangesAsync(ct);
        return true;
    }
}

/// <summary>Get handler.</summary>
public sealed class GetExpenseByIdHandler(IMainDbContext db)
    : IQueryHandler<GetExpenseByIdQuery, ExpenseResponse>
{
    public async Task<Result<ExpenseResponse>> Handle(GetExpenseByIdQuery q, CancellationToken ct)
    {
        var x = await ExpenseGrid
            .Query(db)
            .Where(x => x.Id == q.Id)
            .Select(ExpenseGrid.Projection)
            .SingleOrDefaultAsync(ct);
        return x is null ? ExpenseErrors.NotFound(q.Id) : x;
    }
}

/// <summary>Grid handler.</summary>
public sealed class GetExpensesGridHandler(IMainDbContext db)
    : IQueryHandler<GetExpensesGridQuery, PagedResult<ExpenseResponse>>
{
    public async Task<Result<PagedResult<ExpenseResponse>>> Handle(
        GetExpensesGridQuery q,
        CancellationToken ct
    )
    {
        var r = ExpenseGrid.Query(db).ApplyGridQuery(q.Grid, ExpenseGrid.Fields);
        if (r.IsFailure)
            return r.Error;
        return await r.Value.ToPagedResultAsync(q.Grid, ExpenseGrid.Projection, ct);
    }
}
