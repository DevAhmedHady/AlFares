using Microsoft.EntityFrameworkCore;
using Identity.Domain;

namespace Identity.Persistence;

public sealed class UserRepository(IMainDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Set<User>().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        db.Set<User>().FirstOrDefaultAsync(u => u.Email == Email.FromPersisted(normalizedEmail), ct);

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        db.Set<User>().AnyAsync(u => u.Email == Email.FromPersisted(normalizedEmail), ct);

    public void Add(User user) => db.Set<User>().Add(user);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class TenantRepository(IMainDbContext db) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct) =>
        db.Set<Tenant>().AnyAsync(t => t.Slug == Slug.FromPersisted(slug), ct);

    public void Add(Tenant tenant) => db.Set<Tenant>().Add(tenant);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class RoleTemplateRepository(IMainDbContext db) : IRoleTemplateRepository
{
    public async Task<IReadOnlyList<RoleTemplate>> GetTemplatesAsync(CancellationToken ct)
    {
        var roles = await db.Set<Role>().AsNoTracking().ToListAsync(ct);
        var rolePermissions = await db.Set<RolePermission>().AsNoTracking().ToListAsync(ct);

        return roles
            .Select(r => new RoleTemplate(
                r.Id, r.Name, r.IsSystem,
                rolePermissions.Where(rp => rp.RoleId == r.Id).Select(rp => rp.PermissionId).ToList()))
            .ToList();
    }
}

public sealed class MembershipRepository(IMainDbContext db) : IMembershipRepository
{
    public Task<TenantUser?> GetActiveMembershipAsync(Guid tenantId, Guid userId, CancellationToken ct) =>
        db.Set<TenantUser>().FirstOrDefaultAsync(tu => tu.TenantId == tenantId && tu.UserId == userId && tu.IsActive, ct);

    public async Task<EffectiveAccess> GetEffectiveAccessAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var membership = await db.Set<TenantUser>().AsNoTracking()
            .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && tu.UserId == userId && tu.IsActive, ct);
        if (membership is null)
            return new EffectiveAccess([], []);

        var roleIds = await db.Set<TenantUserRole>().AsNoTracking()
            .Where(tur => tur.TenantUserId == membership.Id)
            .Select(tur => tur.TenantRoleId)
            .ToListAsync(ct);

        if (roleIds.Count == 0)
            return new EffectiveAccess([], []);

        var roles = await db.Set<TenantRole>().AsNoTracking()
            .Where(tr => roleIds.Contains(tr.Id))
            .Select(tr => tr.Name)
            .ToListAsync(ct);

        var permissionIds = await db.Set<TenantPermission>().AsNoTracking()
            .Where(tp => roleIds.Contains(tp.TenantRoleId))
            .Select(tp => tp.PermissionId)
            .Distinct()
            .ToListAsync(ct);

        var permissions = await db.Set<Permission>().AsNoTracking()
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Code)
            .ToListAsync(ct);

        return new EffectiveAccess(roles, permissions);
    }

    public Task<TenantRole?> GetTenantRoleByNameAsync(Guid tenantId, string name, CancellationToken ct) =>
        db.Set<TenantRole>().FirstOrDefaultAsync(tr => tr.TenantId == tenantId && tr.Name == name, ct);

    public Task<bool> HasTenantUserRoleAsync(Guid tenantUserId, Guid tenantRoleId, CancellationToken ct) =>
        db.Set<TenantUserRole>().AnyAsync(x => x.TenantUserId == tenantUserId && x.TenantRoleId == tenantRoleId, ct);

    public void AddTenantUser(TenantUser tenantUser) => db.Set<TenantUser>().Add(tenantUser);
    public void AddTenantRole(TenantRole tenantRole) => db.Set<TenantRole>().Add(tenantRole);
    public void AddTenantPermission(TenantPermission tenantPermission) => db.Set<TenantPermission>().Add(tenantPermission);
    public void AddTenantUserRole(TenantUserRole tenantUserRole) => db.Set<TenantUserRole>().Add(tenantUserRole);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class RefreshTokenRepository(IMainDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
        db.Set<RefreshToken>().FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

    public void Add(RefreshToken token) => db.Set<RefreshToken>().Add(token);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
