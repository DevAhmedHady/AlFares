using Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email)
            .HasConversion(e => e.Value, v => Email.FromPersisted(v))
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
        b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(150).IsRequired();
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("permissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(256).IsRequired();
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
        b.Property(x => x.IsSystem).HasColumnName("is_system");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("role_permissions");
        b.HasKey(x => new { x.RoleId, x.PermissionId });
        b.Property(x => x.RoleId).HasColumnName("role_id");
        b.Property(x => x.PermissionId).HasColumnName("permission_id");
    }
}

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("tenants");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        b.Property(x => x.Slug)
            .HasConversion(s => s.Value, v => Slug.FromPersisted(v))
            .HasColumnName("slug")
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
{
    public void Configure(EntityTypeBuilder<TenantUser> b)
    {
        b.ToTable("tenant_users");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.JoinedAtUtc).HasColumnName("joined_at_utc");
        b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
    }
}

public sealed class TenantRoleConfiguration : IEntityTypeConfiguration<TenantRole>
{
    public void Configure(EntityTypeBuilder<TenantRole> b)
    {
        b.ToTable("tenant_roles");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(x => x.BaseRoleId).HasColumnName("base_role_id");
        b.Property(x => x.IsSystem).HasColumnName("is_system");
        b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}

public sealed class TenantPermissionConfiguration : IEntityTypeConfiguration<TenantPermission>
{
    public void Configure(EntityTypeBuilder<TenantPermission> b)
    {
        b.ToTable("tenant_permissions");
        b.HasKey(x => new { x.TenantRoleId, x.PermissionId });
        b.Property(x => x.TenantRoleId).HasColumnName("tenant_role_id");
        b.Property(x => x.PermissionId).HasColumnName("permission_id");
    }
}

public sealed class TenantUserRoleConfiguration : IEntityTypeConfiguration<TenantUserRole>
{
    public void Configure(EntityTypeBuilder<TenantUserRole> b)
    {
        b.ToTable("tenant_user_roles");
        b.HasKey(x => new { x.TenantUserId, x.TenantRoleId });
        b.Property(x => x.TenantUserId).HasColumnName("tenant_user_id");
        b.Property(x => x.TenantRoleId).HasColumnName("tenant_role_id");
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc");
        b.Property(x => x.ReplacedByHash).HasColumnName("replaced_by_hash").HasMaxLength(128);
    }
}
