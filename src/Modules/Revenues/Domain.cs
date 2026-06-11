using BuildingBlocks.Ledger;
using SharedKernel;

namespace Revenues.Domain;

/// <summary>Managed revenue type.</summary>
public sealed class RevenueType : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private RevenueType() { }

    private RevenueType(string name)
        : base(Guid.NewGuid())
    {
        Name = name;
        IsActive = true;
    }

    public static Result<RevenueType> Create(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? Error.Validation("revenues.type_name_required", "Name is required.")
            : new RevenueType(name.Trim());

    public Result<RevenueType> Update(string? name, bool active)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("revenues.type_name_required", "Name is required.");
        Name = name.Trim();
        IsActive = active;
        return this;
    }
}

/// <summary>Revenue aggregate.</summary>
public sealed class Revenue : AggregateRoot
{
    public Guid RevenueTypeId { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public OwnerType OwnerType { get; private set; }
    public Guid? OwnerId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private Revenue() { }

    private Revenue(
        Guid typeId,
        decimal amount,
        DateOnly date,
        string source,
        string? notes,
        OwnerType ownerType,
        Guid? ownerId
    )
        : base(Guid.NewGuid())
    {
        RevenueTypeId = typeId;
        Amount = amount;
        Date = date;
        Source = source;
        Notes = notes;
        OwnerType = ownerType;
        OwnerId = ownerId;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
    }

    public static Result<Revenue> Create(
        Guid typeId,
        decimal amount,
        DateOnly date,
        string? source,
        string? notes,
        OwnerType ownerType = OwnerType.General,
        Guid? ownerId = null
    )
    {
        if (
            typeId == Guid.Empty
            || amount <= 0
            || string.IsNullOrWhiteSpace(source)
            || (ownerType != OwnerType.General && ownerId is null)
        )
            return Error.Validation("revenues.invalid", "Invalid revenue.");
        return new Revenue(
            typeId,
            amount,
            date,
            source.Trim(),
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            ownerType,
            ownerId
        );
    }

    public Result<Revenue> Update(
        Guid typeId,
        decimal amount,
        DateOnly date,
        string? source,
        string? notes,
        OwnerType ownerType,
        Guid? ownerId
    )
    {
        var r = Create(typeId, amount, date, source, notes, ownerType, ownerId);
        if (r.IsFailure)
            return r.Error;
        RevenueTypeId = typeId;
        Amount = amount;
        Date = date;
        Source = source!.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        OwnerType = ownerType;
        OwnerId = ownerId;
        UpdatedAtUtc = DateTime.UtcNow;
        return this;
    }
}
