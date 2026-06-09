using Microsoft.EntityFrameworkCore;
using Identity.Domain;

namespace Identity.Persistence;

public sealed class UserRepository(IdentityDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == Email.FromPersisted(normalizedEmail), ct);

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.Email == Email.FromPersisted(normalizedEmail), ct);

    public void Add(User user) => db.Users.Add(user);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class TenantRepository(IdentityDbContext db) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct) =>
        db.Tenants.AnyAsync(t => t.Slug == Slug.FromPersisted(slug), ct);

    public void Add(Tenant tenant) => db.Tenants.Add(tenant);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class RoleTemplateRepository(IdentityDbContext db) : IRoleTemplateRepository
{
    public async Task<IReadOnlyList<RoleTemplate>> GetTemplatesAsync(CancellationToken ct)
    {
        var roles = await db.Roles.AsNoTracking().ToListAsync(ct);
        var rolePermissions = await db.RolePermissions.AsNoTracking().ToListAsync(ct);

        return roles
            .Select(r => new RoleTemplate(
                r.Id, r.Name, r.IsSystem,
                rolePermissions.Where(rp => rp.RoleId == r.Id).Select(rp => rp.PermissionId).ToList()))
            .ToList();
    }
}

public sealed class MembershipRepository(IdentityDbContext db) : IMembershipRepository
{
    public Task<TenantUser?> GetActiveMembershipAsync(Guid tenantId, Guid userId, CancellationToken ct) =>
        db.TenantUsers.FirstOrDefaultAsync(tu => tu.TenantId == tenantId && tu.UserId == userId && tu.IsActive, ct);

    public async Task<EffectiveAccess> GetEffectiveAccessAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var membership = await db.TenantUsers.AsNoTracking()
            .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && tu.UserId == userId && tu.IsActive, ct);
        if (membership is null)
            return new EffectiveAccess([], []);

        var roleIds = await db.TenantUserRoles.AsNoTracking()
            .Where(tur => tur.TenantUserId == membership.Id)
            .Select(tur => tur.TenantRoleId)
            .ToListAsync(ct);

        if (roleIds.Count == 0)
            return new EffectiveAccess([], []);

        var roles = await db.TenantRoles.AsNoTracking()
            .Where(tr => roleIds.Contains(tr.Id))
            .Select(tr => tr.Name)
            .ToListAsync(ct);

        var permissionIds = await db.TenantPermissions.AsNoTracking()
            .Where(tp => roleIds.Contains(tp.TenantRoleId))
            .Select(tp => tp.PermissionId)
            .Distinct()
            .ToListAsync(ct);

        var permissions = await db.Permissions.AsNoTracking()
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Code)
            .ToListAsync(ct);

        return new EffectiveAccess(roles, permissions);
    }

    public Task<TenantRole?> GetTenantRoleByNameAsync(Guid tenantId, string name, CancellationToken ct) =>
        db.TenantRoles.FirstOrDefaultAsync(tr => tr.TenantId == tenantId && tr.Name == name, ct);

    public Task<bool> HasTenantUserRoleAsync(Guid tenantUserId, Guid tenantRoleId, CancellationToken ct) =>
        db.TenantUserRoles.AnyAsync(x => x.TenantUserId == tenantUserId && x.TenantRoleId == tenantRoleId, ct);

    public void AddTenantUser(TenantUser tenantUser) => db.TenantUsers.Add(tenantUser);
    public void AddTenantRole(TenantRole tenantRole) => db.TenantRoles.Add(tenantRole);
    public void AddTenantPermission(TenantPermission tenantPermission) => db.TenantPermissions.Add(tenantPermission);
    public void AddTenantUserRole(TenantUserRole tenantUserRole) => db.TenantUserRoles.Add(tenantUserRole);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class RefreshTokenRepository(IdentityDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
        db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

    public void Add(RefreshToken token) => db.RefreshTokens.Add(token);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
