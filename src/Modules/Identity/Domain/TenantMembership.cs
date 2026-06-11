using SharedKernel;

namespace Identity.Domain;

// A user's membership in a tenant.
public sealed class TenantUser : Entity
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }

    private TenantUser() { }

    public TenantUser(Guid tenantId, Guid userId)
        : base(Guid.NewGuid())
    {
        TenantId = tenantId;
        UserId = userId;
        IsActive = true;
        JoinedAtUtc = DateTime.UtcNow;
    }
}

// A role that belongs to a specific tenant (cloned from a global Role template at provisioning).
public sealed class TenantRole : Entity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public Guid? BaseRoleId { get; private set; }
    public bool IsSystem { get; private set; }

    private TenantRole() { }

    public TenantRole(Guid tenantId, string name, Guid? baseRoleId, bool isSystem)
        : base(Guid.NewGuid())
    {
        TenantId = tenantId;
        Name = name;
        BaseRoleId = baseRoleId;
        IsSystem = isSystem;
    }
}

// Permission granted to a tenant role.
public sealed class TenantPermission
{
    public Guid TenantRoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    private TenantPermission() { }

    public TenantPermission(Guid tenantRoleId, Guid permissionId)
    {
        TenantRoleId = tenantRoleId;
        PermissionId = permissionId;
    }
}

// Assignment of a tenant role to a member.
public sealed class TenantUserRole
{
    public Guid TenantUserId { get; private set; }
    public Guid TenantRoleId { get; private set; }

    private TenantUserRole() { }

    public TenantUserRole(Guid tenantUserId, Guid tenantRoleId)
    {
        TenantUserId = tenantUserId;
        TenantRoleId = tenantRoleId;
    }
}
