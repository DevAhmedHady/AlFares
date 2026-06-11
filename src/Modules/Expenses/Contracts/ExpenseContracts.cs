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

/// <summary>Expense type request.</summary>
public sealed record ExpenseTypeRequest(string Name, ExpenseScope Scope, bool IsActive = true);

/// <summary>Expense type response.</summary>
public sealed record ExpenseTypeResponse(Guid Id, string Name, ExpenseScope Scope, bool IsActive);
