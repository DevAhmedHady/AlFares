using SharedKernel;

namespace Identity.Domain;

// Global role template (e.g. Owner / Admin / Member). Cloned into tenant_roles on provisioning.
public sealed class Role : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public bool IsSystem { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Role() { }

    public Role(Guid id, string name, bool isSystem)
        : base(id)
    {
        Name = name;
        IsSystem = isSystem;
        CreatedAtUtc = DateTime.UtcNow;
    }
}

// Default permissions for a global role template.
public sealed class RolePermission
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    private RolePermission() { }

    public RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}

// Read model: a role template with the permission ids it grants (used by provisioning).
public sealed record RoleTemplate(
    Guid RoleId,
    string Name,
    bool IsSystem,
    IReadOnlyList<Guid> PermissionIds
);
