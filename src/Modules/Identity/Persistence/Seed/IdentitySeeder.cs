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
        ("catalog.books.read", "View books"),
        ("catalog.books.write", "Create or update books"),
        ("catalog.books.delete", "Delete books"),
        ("ordering.orders.read", "View orders"),
        ("ordering.orders.write", "Place orders"),
        ("identity.tenants.manage", "Manage tenants"),
        ("identity.users.manage", "Manage tenant members and roles")
    ];

    // System role templates and the permission codes they grant.
    public static readonly (string Name, string[] Permissions)[] Roles =
    [
        ("Owner", ["catalog.books.read", "catalog.books.write", "catalog.books.delete",
                   "ordering.orders.read", "ordering.orders.write",
                   "identity.tenants.manage", "identity.users.manage"]),
        ("Admin", ["catalog.books.read", "catalog.books.write", "catalog.books.delete",
                   "ordering.orders.read", "ordering.orders.write", "identity.users.manage"]),
        ("Member", ["catalog.books.read", "ordering.orders.read"])
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
