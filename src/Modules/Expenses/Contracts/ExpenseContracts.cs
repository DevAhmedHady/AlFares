using BuildingBlocks.Export; using BuildingBlocks.Grids;
namespace Expenses.Contracts;
/** <summary>Create request.</summary> */ public sealed record CreateExpenseRequest(string Category,decimal Amount,DateOnly Date,string Payee,string? Notes);
/** <summary>Update request.</summary> */ public sealed record UpdateExpenseRequest(string Category,decimal Amount,DateOnly Date,string Payee,string? Notes);
/** <summary>Response.</summary> */ public sealed record ExpenseResponse(Guid Id,string Category,decimal Amount,DateOnly Date,string Payee,string? Notes,DateTime CreatedAtUtc,DateTime UpdatedAtUtc);
/** <summary>Export request.</summary> */ public sealed record ExpenseExportRequest(GridQuery Grid,ExportFormat Format);
