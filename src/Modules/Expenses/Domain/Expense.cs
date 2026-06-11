using BuildingBlocks.Ledger;
using SharedKernel;

namespace Expenses.Domain;

/// <summary>Expense type scope.</summary>
public enum ExpenseScope
{
    General = 0,
    Car = 1,
}

/// <summary>Managed expense type.</summary>
public sealed class ExpenseType : AggregateRoot
{
    /// <summary>Name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Scope.</summary>
    public ExpenseScope Scope { get; private set; }

    /// <summary>Whether selectable.</summary>
    public bool IsActive { get; private set; }

    private ExpenseType() { }

    private ExpenseType(string name, ExpenseScope scope)
        : base(Guid.NewGuid())
    {
        Name = name;
        Scope = scope;
        IsActive = true;
    }

    /// <summary>Creates an expense type.</summary>
    public static Result<ExpenseType> Create(string? name, ExpenseScope scope) =>
        string.IsNullOrWhiteSpace(name)
            ? ExpenseErrors.TypeNameRequired
            : new ExpenseType(name.Trim(), scope);

    /// <summary>Updates an expense type.</summary>
    public Result<ExpenseType> Update(string? name, ExpenseScope scope, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ExpenseErrors.TypeNameRequired;
        Name = name.Trim();
        Scope = scope;
        IsActive = isActive;
        return this;
    }
}

/// <summary>Expense aggregate.</summary>
public sealed class Expense : AggregateRoot
{
    /// <summary>Expense type id.</summary>
    public Guid ExpenseTypeId { get; private set; }

    /// <summary>Amount.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Expense date.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Payee.</summary>
    public string Payee { get; private set; } = string.Empty;

    /// <summary>Notes.</summary>
    public string? Notes { get; private set; }

    /// <summary>Owner kind.</summary>
    public OwnerType OwnerType { get; private set; }

    /// <summary>Owner id.</summary>
    public Guid? OwnerId { get; private set; }

    /// <summary>Created UTC.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Updated UTC.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private Expense() { }

    private Expense(
        Guid typeId,
        decimal amount,
        DateOnly date,
        string payee,
        string? notes,
        OwnerType ownerType,
        Guid? ownerId
    )
        : base(Guid.NewGuid())
    {
        ExpenseTypeId = typeId;
        Amount = amount;
        Date = date;
        Payee = payee;
        Notes = notes;
        OwnerType = ownerType;
        OwnerId = ownerId;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Creates expense.</summary>
    public static Result<Expense> Create(
        Guid typeId,
        decimal amount,
        DateOnly date,
        string? payee,
        string? notes,
        OwnerType ownerType = OwnerType.General,
        Guid? ownerId = null
    )
    {
        if (typeId == Guid.Empty)
            return ExpenseErrors.TypeRequired;
        if (amount <= 0)
            return ExpenseErrors.AmountInvalid;
        if (string.IsNullOrWhiteSpace(payee))
            return ExpenseErrors.PayeeRequired;
        if (ownerType != OwnerType.General && ownerId is null)
            return ExpenseErrors.OwnerRequired;
        return new Expense(
            typeId,
            amount,
            date,
            payee.Trim(),
            Normalize(notes),
            ownerType,
            ownerId
        );
    }

    /// <summary>Updates expense.</summary>
    public Result<Expense> Update(
        Guid typeId,
        decimal amount,
        DateOnly date,
        string? payee,
        string? notes,
        OwnerType ownerType,
        Guid? ownerId
    )
    {
        var valid = Create(typeId, amount, date, payee, notes, ownerType, ownerId);
        if (valid.IsFailure)
            return valid.Error;
        ExpenseTypeId = typeId;
        Amount = amount;
        Date = date;
        Payee = payee!.Trim();
        Notes = Normalize(notes);
        OwnerType = ownerType;
        OwnerId = ownerId;
        UpdatedAtUtc = DateTime.UtcNow;
        return this;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
