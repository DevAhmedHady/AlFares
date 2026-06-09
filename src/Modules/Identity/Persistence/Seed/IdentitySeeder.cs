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

    public static async Task SeedAsync(IdentityDbContext db, CancellationToken ct = default)
    {
        // Permissions
        var existingCodes = await db.Permissions.Select(p => p.Code).ToListAsync(ct);
        foreach (var (code, description) in Permissions)
            if (!existingCodes.Contains(code))
                db.Permissions.Add(new Permission(Guid.NewGuid(), code, description));
        await db.SaveChangesAsync(ct);

        var permByCode = await db.Permissions.ToDictionaryAsync(p => p.Code, p => p.Id, ct);

        // Roles + role_permissions
        foreach (var (name, perms) in Roles)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);
            if (role is null)
            {
                role = new Role(Guid.NewGuid(), name, isSystem: true);
                db.Roles.Add(role);
                await db.SaveChangesAsync(ct);
            }

            var existing = await db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync(ct);

            foreach (var code in perms)
                if (permByCode.TryGetValue(code, out var pid) && !existing.Contains(pid))
                    db.RolePermissions.Add(new RolePermission(role.Id, pid));
        }

        await db.SaveChangesAsync(ct);
    }
}
