using SharedKernel;

namespace Identity.Domain;

// Global catalog entry, e.g. code "catalog.books.write".
public sealed class Permission : Entity
{
    public string Code { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    private Permission() { }

    public Permission(Guid id, string code, string description) : base(id)
    {
        Code = code;
        Description = description;
    }
}
