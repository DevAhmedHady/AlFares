using Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence.Seed;

/// <summary>
/// Seeds the single Al-Faris tenant, its bootstrap owner account, and the owner membership.
/// Runs after <see cref="IdentitySeeder"/> (which seeds the global permission catalog and role
/// templates) and is idempotent by tenant slug — re-running is a no-op once the tenant exists.
/// </summary>
public static class IdentityTenantSeeder
{
    /// <summary>
    /// Creates the configured tenant + owner if the tenant slug is not already present.
    /// </summary>
    /// <param name="db">The Identity database context.</param>
    /// <param name="hasher">Password hasher used to hash the bootstrap admin password.</param>
    /// <param name="options">Bootstrap tenant/admin settings.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task SeedAsync(
        IdentityDbContext db,
        IPasswordHasher<User> hasher,
        SeedOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(options);

        var slugResult = Slug.Create(options.TenantSlug);
        if (slugResult.IsFailure)
            throw new InvalidOperationException($"Invalid Seed:TenantSlug '{options.TenantSlug}': {slugResult.Error.Code}.");
        var slug = slugResult.Value;

        // Idempotency guard: the tenant already exists, nothing to do. Compare the whole value
        // object (not .Value) so EF applies the configured value converter and the query translates.
        if (await db.Tenants.AnyAsync(t => t.Slug == slug, ct))
            return;

        var emailResult = Email.Create(options.AdminEmail);
        if (emailResult.IsFailure)
            throw new InvalidOperationException($"Invalid Seed:AdminEmail '{options.AdminEmail}': {emailResult.Error.Code}.");
        var email = emailResult.Value;

        var tenantResult = Tenant.Create(options.TenantName, slugResult.Value);
        if (tenantResult.IsFailure)
            throw new InvalidOperationException($"Invalid Seed:TenantName: {tenantResult.Error.Code}.");
        var tenant = tenantResult.Value;
        db.Tenants.Add(tenant);

        // Reuse an existing admin user (e.g. previously registered) or create one from config.
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (admin is null)
        {
            var userResult = User.Create(email, options.TenantName);
            if (userResult.IsFailure)
                throw new InvalidOperationException($"Cannot create admin user: {userResult.Error.Code}.");
            admin = userResult.Value;
            admin.SetPasswordHash(hasher.HashPassword(admin, options.AdminPassword));
            db.Users.Add(admin);
        }

        // Clone the global role templates (Owner/Admin/Member) into tenant-owned roles + permissions.
        var templates = await db.Roles
            .Where(r => r.IsSystem)
            .Select(r => new
            {
                r.Id,
                r.Name,
                PermissionIds = db.RolePermissions.Where(rp => rp.RoleId == r.Id).Select(rp => rp.PermissionId).ToList()
            })
            .ToListAsync(ct);

        TenantRole? ownerRole = null;
        foreach (var template in templates)
        {
            var tenantRole = new TenantRole(tenant.Id, template.Name, template.Id, isSystem: true);
            db.TenantRoles.Add(tenantRole);
            foreach (var permissionId in template.PermissionIds)
                db.TenantPermissions.Add(new TenantPermission(tenantRole.Id, permissionId));

            if (template.Name == IdentitySeeder.OwnerRoleName)
                ownerRole = tenantRole;
        }

        if (ownerRole is null)
            throw new InvalidOperationException("Owner role template missing; run IdentitySeeder first.");

        var membership = new TenantUser(tenant.Id, admin.Id);
        db.TenantUsers.Add(membership);
        db.TenantUserRoles.Add(new TenantUserRole(membership.Id, ownerRole.Id));

        await db.SaveChangesAsync(ct);
    }
}
