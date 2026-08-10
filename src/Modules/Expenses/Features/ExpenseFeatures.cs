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
    Guid? ExpenseTypeId,
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
    Guid? ExpenseTypeId,
    decimal Amount,
    DateOnly Date,
    string Payee,
    string? Notes,
    OwnerType OwnerType,
    Guid? OwnerId
) : ICommand<ExpenseResponse>;

/// <summary>Delete command.</summary>
public sealed record DeleteExpenseCommand(Guid Id) : ICommand<bool>;

/// <summary>Bulk delete command.</summary>
public sealed record BulkDeleteExpensesCommand(IReadOnlyList<Guid> Ids)
    : ICommand<BulkDeleteExpensesResponse>;

/// <summary>Get query.</summary>
public sealed record GetExpenseByIdQuery(Guid Id) : IQuery<ExpenseResponse>;

/// <summary>Grid query.</summary>
public sealed record GetExpensesGridQuery(GridQuery Grid) : IQuery<PagedResult<ExpenseResponse>>;

/// <summary>Report query.</summary>
public sealed record GetExpenseReportQuery(
    DateOnly? From,
    DateOnly? To,
    int? Year,
    int? Month,
    Guid? ExpenseTypeId
) : IQuery<ExpenseReportResponse>;

/// <summary>Create validator.</summary>
public sealed class CreateExpenseValidator : AbstractValidator<CreateExpenseCommand>
{
    /// <summary>Rules.</summary>
    public CreateExpenseValidator()
    {
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
    public Guid? ExpenseTypeId { get; init; }
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

    /// <summary>Display label for grid/report when no expense type is selected.</summary>
    public static string DisplayTypeName(ExpenseType? type, string payee) =>
        type?.Name ?? (string.IsNullOrWhiteSpace(payee) ? "Others" : payee.Trim());

    public static IQueryable<ExpenseGridRow> Query(IMainDbContext db) =>
        from e in db.Set<Expense>().AsNoTracking()
        join t in db.Set<ExpenseType>().AsNoTracking() on e.ExpenseTypeId equals t.Id into types
        from t in types.DefaultIfEmpty()
        orderby e.Date descending //default ordering
        select new ExpenseGridRow
        {
            Id = e.Id,
            ExpenseTypeId = e.ExpenseTypeId,
            ExpenseTypeName = t != null ? t.Name : (e.Payee.Trim().Length == 0 ? "Others" : e.Payee),
            Amount = e.Amount,
            Date = e.Date,
            Payee = e.Payee,
            Notes = e.Notes,
            OwnerType = e.OwnerType,
            OwnerId = e.OwnerId,
            CreatedAtUtc = e.CreatedAtUtc,
            UpdatedAtUtc = e.UpdatedAtUtc,
        };
}

/// <summary>Create handler.</summary>
public sealed class CreateExpenseHandler(IExpenseRepository repo, IMainDbContext db)
    : ICommandHandler<CreateExpenseCommand, ExpenseResponse>
{
    public async Task<Result<ExpenseResponse>> Handle(CreateExpenseCommand c, CancellationToken ct)
    {
        ExpenseType? t = null;
        if (c.ExpenseTypeId is Guid typeId)
        {
            t = await db.Set<ExpenseType>().FindAsync([typeId], ct);
            if (t is null)
                return ExpenseErrors.TypeRequired;
        }

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
            e.Value.ExpenseTypeId,
            ExpenseGrid.DisplayTypeName(t, e.Value.Payee),
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
        ExpenseType? t = null;
        if (c.ExpenseTypeId is Guid typeId)
        {
            t = await db.Set<ExpenseType>().FindAsync([typeId], ct);
            if (t is null)
                return ExpenseErrors.TypeRequired;
        }

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
            e.ExpenseTypeId,
            ExpenseGrid.DisplayTypeName(t, e.Payee),
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

/// <summary>Bulk delete handler.</summary>
public sealed class BulkDeleteExpensesHandler(IMainDbContext db)
    : ICommandHandler<BulkDeleteExpensesCommand, BulkDeleteExpensesResponse>
{
    /// <summary>Maximum ids accepted in one bulk delete request.</summary>
    public const int MaxIds = 200;

    /// <inheritdoc />
    public async Task<Result<BulkDeleteExpensesResponse>> Handle(
        BulkDeleteExpensesCommand c,
        CancellationToken ct
    )
    {
        if (c.Ids is null || c.Ids.Count == 0)
            return ExpenseErrors.BulkIdsRequired;

        var ids = c.Ids.Where(id => id != Guid.Empty).Distinct().Take(MaxIds).ToList();
        if (ids.Count == 0)
            return ExpenseErrors.BulkIdsRequired;

        var entities = await db.Set<Expense>()
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (entities.Count == 0)
            return new BulkDeleteExpensesResponse(0);

        foreach (var entity in entities)
            db.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new BulkDeleteExpensesResponse(entities.Count);
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
        var page = await r.Value.ToPagedResultAsync(q.Grid, ExpenseGrid.Projection, ct)
            .ConfigureAwait(false);
        var totalAmount = await r.Value.SumAsync(x => x.Amount, ct).ConfigureAwait(false);
        return page with
        {
            Aggregates = new Dictionary<string, decimal> { ["amount"] = totalAmount },
        };
    }
}

/// <summary>Builds expense report aggregates from filtered grid rows.</summary>
public static class ExpenseReportBuilder
{
    /// <summary>Maximum detail rows returned in the report response.</summary>
    public const int MaxItems = 5000;

    /// <summary>Filters expense grid rows for report scope.</summary>
    public static IQueryable<ExpenseGridRow> Filter(
        IMainDbContext db,
        DateOnly? from,
        DateOnly? to,
        int? year,
        int? month,
        Guid? expenseTypeId
    )
    {
        var q = ExpenseGrid.Query(db);
        if (year is >= 1 and var y && month is >= 1 and <= 12 and var m)
        {
            var start = new DateOnly(y, m, 1);
            var end = new DateOnly(y, m, DateTime.DaysInMonth(y, m));
            q = q.Where(x => x.Date >= start && x.Date <= end);
        }
        else
        {
            if (from.HasValue)
                q = q.Where(x => x.Date >= from);
            if (to.HasValue)
                q = q.Where(x => x.Date <= to);
        }

        if (expenseTypeId.HasValue)
            q = q.Where(x => x.ExpenseTypeId == expenseTypeId);
        return q;
    }

    /// <summary>Builds the expense report from filtered rows.</summary>
    public static async Task<ExpenseReportResponse> BuildAsync(
        IQueryable<ExpenseGridRow> query,
        CancellationToken ct
    )
    {
        var rows = await query
            .Select(x => new
            {
                x.Id,
                x.ExpenseTypeName,
                x.Amount,
                x.Date,
                x.Payee,
                x.Notes,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var total = rows.Sum(x => x.Amount);
        var count = rows.Count;
        var average = count == 0 ? 0 : total / count;

        var byCategoryRaw = rows.GroupBy(x => x.ExpenseTypeName)
            .Select(g => new { Label = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Amount)
            .ToArray();
        var byCategory = byCategoryRaw
            .Select(x => new ExpenseReportBreakdown(
                x.Label,
                x.Amount,
                Share(total, x.Amount)
            ))
            .ToArray();

        var top = byCategoryRaw.FirstOrDefault();
        var summary = new ExpenseReportSummary(
            total,
            count,
            average,
            top?.Label,
            top?.Amount ?? 0,
            top is null ? 0 : Share(total, top.Amount)
        );

        var byMonth = rows.GroupBy(x => new { x.Date.Year, x.Date.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var amount = g.Sum(x => x.Amount);
                return new ExpenseReportBreakdown(
                    $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                    amount,
                    Share(total, amount)
                );
            })
            .ToArray();

        var items = rows.OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Amount)
            .Take(MaxItems)
            .Select(x => new ExpenseReportRow(
                x.Id,
                x.ExpenseTypeName,
                x.Amount,
                x.Date,
                x.Payee,
                x.Notes
            ))
            .ToArray();

        return new ExpenseReportResponse(
            summary,
            byCategory,
            byMonth,
            items,
            count > MaxItems
        );
    }

    private static decimal Share(decimal total, decimal amount) =>
        total == 0 ? 0 : Math.Round(amount / total * 100, 1);
}

/// <summary>Report handler.</summary>
public sealed class GetExpenseReportHandler(IMainDbContext db)
    : IQueryHandler<GetExpenseReportQuery, ExpenseReportResponse>
{
    /// <inheritdoc />
    public async Task<Result<ExpenseReportResponse>> Handle(
        GetExpenseReportQuery q,
        CancellationToken ct
    ) =>
        await ExpenseReportBuilder
            .BuildAsync(
                ExpenseReportBuilder.Filter(db, q.From, q.To, q.Year, q.Month, q.ExpenseTypeId),
                ct
            )
            .ConfigureAwait(false);
}
