using SharedKernel;

namespace Identity.Domain;

public sealed class Tenant : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public Slug Slug { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Tenant() { }

    private Tenant(Guid id, string name, Slug slug, DateTime createdAtUtc) : base(id)
    {
        Name = name;
        Slug = slug;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<Tenant> Create(string? name, Slug slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            return IdentityErrors.TenantNameEmpty;

        return new Tenant(Guid.NewGuid(), name.Trim(), slug, DateTime.UtcNow);
    }
}
