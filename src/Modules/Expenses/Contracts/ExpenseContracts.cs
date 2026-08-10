using BuildingBlocks.Export;
using BuildingBlocks.Grids;
using BuildingBlocks.Ledger;
using Expenses.Domain;

namespace Expenses.Contracts;

/// <summary>Create request.</summary>
public sealed record CreateExpenseRequest(
    Guid ExpenseTypeId,
    decimal Amount,
    DateOnly Date,
    string Payee,
    string? Notes,
    OwnerType OwnerType = OwnerType.General,
    Guid? OwnerId = null
);

/// <summary>Update request.</summary>
public sealed record UpdateExpenseRequest(
    Guid ExpenseTypeId,
    decimal Amount,
    DateOnly Date,
    string Payee,
    string? Notes,
    OwnerType OwnerType = OwnerType.General,
    Guid? OwnerId = null
);

/// <summary>Response.</summary>
public sealed record ExpenseResponse(
    Guid Id,
    Guid ExpenseTypeId,
    string ExpenseTypeName,
    decimal Amount,
    DateOnly Date,
    string Payee,
    string? Notes,
    OwnerType OwnerType,
    Guid? OwnerId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

/// <summary>Export request.</summary>
public sealed record ExpenseExportRequest(GridQuery Grid, ExportFormat Format);

/// <summary>Bulk delete request.</summary>
public sealed record BulkDeleteExpensesRequest(IReadOnlyList<Guid> Ids);

/// <summary>Bulk delete response.</summary>
public sealed record BulkDeleteExpensesResponse(int Deleted);

/// <summary>Expense type request.</summary>
public sealed record ExpenseTypeRequest(string Name, ExpenseScope Scope, bool IsActive = true);

/// <summary>Expense type response.</summary>
public sealed record ExpenseTypeResponse(Guid Id, string Name, ExpenseScope Scope, bool IsActive);

/// <summary>Expense report request.</summary>
public sealed record ExpenseReportRequest(
    DateOnly? From,
    DateOnly? To,
    int? Year,
    int? Month,
    Guid? ExpenseTypeId
);

/// <summary>Expense report summary.</summary>
public sealed record ExpenseReportSummary(
    decimal Total,
    int Count,
    decimal Average,
    string? TopCategory,
    decimal TopCategoryAmount,
    decimal TopCategoryShare
);

/// <summary>Expense report breakdown row.</summary>
public sealed record ExpenseReportBreakdown(string Label, decimal Amount, decimal Share);

/// <summary>Expense report detail row.</summary>
public sealed record ExpenseReportRow(
    Guid Id,
    string ExpenseTypeName,
    decimal Amount,
    DateOnly Date,
    string Payee,
    string? Notes
);

/// <summary>Expense report response.</summary>
public sealed record ExpenseReportResponse(
    ExpenseReportSummary Summary,
    IReadOnlyList<ExpenseReportBreakdown> ByCategory,
    IReadOnlyList<ExpenseReportBreakdown> ByMonth,
    IReadOnlyList<ExpenseReportRow> Items,
    bool ItemsTruncated
);

/// <summary>Expense report export request.</summary>
public sealed record ExpenseReportExportRequest(
    DateOnly? From,
    DateOnly? To,
    int? Year,
    int? Month,
    Guid? ExpenseTypeId,
    ExportFormat Format
);
