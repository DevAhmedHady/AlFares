namespace BuildingBlocks.Ledger;

/// <summary>Identifies the business owner of a ledger entry.</summary>
public enum OwnerType
{
    General = 0,
    Client = 1,
    OwnedCar = 2,
    RentedCar = 3,
    Worker = 4,
}

/// <summary>Identifies the financial direction of a ledger entry.</summary>
public enum LedgerKind
{
    Expense = 0,
    Revenue = 1,
}

/// <summary>Cross-module ledger read model.</summary>
public sealed record LedgerEntry(
    Guid Id,
    LedgerKind Kind,
    OwnerType OwnerType,
    Guid? OwnerId,
    string Description,
    decimal Amount,
    DateOnly Date
);

/// <summary>Exposes module-owned ledger rows to read-only consumers.</summary>
public interface ILedgerSource
{
    /// <summary>Gets the source ledger kind.</summary>
    LedgerKind Kind { get; }

    /// <summary>Gets entries for one owner and optional period.</summary>
    Task<IReadOnlyList<LedgerEntry>> GetEntriesAsync(
        OwnerType ownerType,
        Guid ownerId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct
    );

    /// <summary>Gets totals grouped by owner.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetTotalsAsync(
        OwnerType ownerType,
        IReadOnlyCollection<Guid> ownerIds,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct
    );
}

/// <summary>Allows modules to create expense ledger rows without referencing Expenses.</summary>
public interface ILedgerWriter
{
    /// <summary>Creates an expense owned by another module entity.</summary>
    Task<Guid> CreateExpenseAsync(
        OwnerType ownerType,
        Guid ownerId,
        Guid expenseTypeId,
        decimal amount,
        DateOnly date,
        string payee,
        string? notes,
        CancellationToken ct
    );
}
