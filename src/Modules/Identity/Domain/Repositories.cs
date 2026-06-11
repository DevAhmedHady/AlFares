namespace Identity.Domain;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken ct);
    void Add(User user);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct);
    void Add(Tenant tenant);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IRoleTemplateRepository
{
    // Global role templates with the permission ids they grant.
    Task<IReadOnlyList<RoleTemplate>> GetTemplatesAsync(CancellationToken ct);
}

// Effective access for a member within one tenant.
public sealed record EffectiveAccess(
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
);

public interface IMembershipRepository
{
    Task<TenantUser?> GetActiveMembershipAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task<EffectiveAccess> GetEffectiveAccessAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task<TenantRole?> GetTenantRoleByNameAsync(Guid tenantId, string name, CancellationToken ct);
    Task<bool> HasTenantUserRoleAsync(Guid tenantUserId, Guid tenantRoleId, CancellationToken ct);

    void AddTenantUser(TenantUser tenantUser);
    void AddTenantRole(TenantRole tenantRole);
    void AddTenantPermission(TenantPermission tenantPermission);
    void AddTenantUserRole(TenantUserRole tenantUserRole);

    Task SaveChangesAsync(CancellationToken ct);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
    void Add(RefreshToken token);
    Task SaveChangesAsync(CancellationToken ct);
}
