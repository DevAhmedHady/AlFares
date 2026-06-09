using Microsoft.EntityFrameworkCore;
using Identity.Domain;

namespace Identity.Persistence.Seed;

// Idempotent seeding of the global permission catalog and system role templates.
// Runs at startup; provisioning a tenant later clones these templates per tenant.
public static class IdentitySeeder
{
    // Permission catalog (code, description). Extend as modules add capabilities.
    public static readonly (string Code, string Description)[] Permissions =
    [
        ("clients.read", "View clients"),
        ("clients.write", "Create or update clients"),
        ("clients.delete", "Delete clients"),
        ("clients.export", "Export clients"),
        ("expenses.read", "View expenses"),
        ("expenses.write", "Create or update expenses"),
        ("expenses.delete", "Delete expenses"),
        ("expenses.export", "Export expenses"),
        ("revenues.read", "View revenues"),
        ("revenues.write", "Create or update revenues"),
        ("revenues.delete", "Delete revenues"),
        ("revenues.export", "Export revenues"),
        ("cars.read", "View cars"),
        ("cars.write", "Create or update cars"),
        ("cars.delete", "Delete cars"),
        ("cars.export", "Export cars"),
        ("workers.read", "View workers"),
        ("workers.write", "Create or update workers"),
        ("workers.delete", "Delete workers"),
        ("workers.export", "Export workers"),
        ("reports.read", "View reports"),
        ("todos.read", "View to-dos"),
        ("todos.write", "Create or update to-dos"),
        ("todos.delete", "Delete to-dos"),
        ("todos.export", "Export to-dos"),
        ("dashboard.charts.read", "View dashboard charts"),
        ("dashboard.charts.manage", "Define and manage dashboard charts"),
        ("identity.users.read", "View tenant members"),
        ("identity.users.manage", "Manage tenant members and roles"),
        ("identity.tenants.manage", "Manage tenants")
    ];

    // All permission codes in catalog order — single source for the role templates below.
    private static readonly string[] AllCodes = Permissions.Select(p => p.Code).ToArray();

    // System role templates and the permission codes they grant (derived from the catalog,
    // so adding a permission above keeps the templates correct with no duplication):
    //   Owner  = everything · Admin = everything except tenant management · Member = read-only.
    public static readonly (string Name, string[] Permissions)[] Roles =
    [
        ("Owner", AllCodes),
        ("Admin", AllCodes.Where(c => c != "identity.tenants.manage").ToArray()),
        ("Member", AllCodes.Where(c => c.EndsWith(".read", StringComparison.Ordinal)).ToArray())
    ];

    public const string OwnerRoleName = "Owner";

    public static async Task SeedAsync(IMainDbContext db, CancellationToken ct = default)
    {
        // Permissions
        var existingCodes = await db.Set<Permission>().Select(p => p.Code).ToListAsync(ct);
        foreach (var (code, description) in Permissions)
            if (!existingCodes.Contains(code))
                db.Set<Permission>().Add(new Permission(Guid.NewGuid(), code, description));
        await db.SaveChangesAsync(ct);

        var permByCode = await db.Set<Permission>().ToDictionaryAsync(p => p.Code, p => p.Id, ct);

        // Roles + role_permissions
        foreach (var (name, perms) in Roles)
        {
            var role = await db.Set<Role>().FirstOrDefaultAsync(r => r.Name == name, ct);
            if (role is null)
            {
                role = new Role(Guid.NewGuid(), name, isSystem: true);
                db.Set<Role>().Add(role);
                await db.SaveChangesAsync(ct);
            }

            var existing = await db.Set<RolePermission>()
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync(ct);

            foreach (var code in perms)
                if (permByCode.TryGetValue(code, out var pid) && !existing.Contains(pid))
                    db.Set<RolePermission>().Add(new RolePermission(role.Id, pid));
        }

        await db.SaveChangesAsync(ct);

        // Reconcile already-provisioned tenant roles with their global templates.
        var rolePermissions = await db.Set<RolePermission>().ToListAsync(ct);
        var tenantRoles = await db.Set<TenantRole>().Where(x => x.BaseRoleId.HasValue).ToListAsync(ct);
        var existingTenantPermissions = await db.Set<TenantPermission>().ToListAsync(ct);
        foreach (var tenantRole in tenantRoles)
        {
            var desired = rolePermissions.Where(x => x.RoleId == tenantRole.BaseRoleId).Select(x => x.PermissionId);
            foreach (var permissionId in desired)
                if (!existingTenantPermissions.Any(x => x.TenantRoleId == tenantRole.Id && x.PermissionId == permissionId))
                    db.Set<TenantPermission>().Add(new TenantPermission(tenantRole.Id, permissionId));
        }

        await db.SaveChangesAsync(ct);
    }
}
