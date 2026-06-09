using BuildingBlocks.Messaging;
using FluentValidation;
using Identity.Contracts;
using Identity.Domain;
using Identity.Persistence.Seed;
using MapsterMapper;
using SharedKernel;

namespace Identity.Features.ProvisionTenant;

public sealed record ProvisionTenantCommand(string Name, string Slug, Guid OwnerUserId)
    : ICommand<TenantResponse>;

public sealed class ProvisionTenantValidator : AbstractValidator<ProvisionTenantCommand>
{
    public ProvisionTenantValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.OwnerUserId).NotEmpty();
    }
}

public sealed class ProvisionTenantHandler(
    ITenantRepository tenants,
    IUserRepository users,
    IRoleTemplateRepository roleTemplates,
    IMembershipRepository memberships,
    IMapper mapper) : ICommandHandler<ProvisionTenantCommand, TenantResponse>
{
    public async Task<Result<TenantResponse>> Handle(ProvisionTenantCommand c, CancellationToken ct)
    {
        var slugResult = Slug.Create(c.Slug);
        if (slugResult.IsFailure)
            return slugResult.Error;

        if (await tenants.ExistsBySlugAsync(slugResult.Value.Value, ct))
            return IdentityErrors.SlugTaken;

        var owner = await users.GetByIdAsync(c.OwnerUserId, ct);
        if (owner is null)
            return IdentityErrors.UserNotFound(c.OwnerUserId);

        var tenantResult = Tenant.Create(c.Name, slugResult.Value);
        if (tenantResult.IsFailure)
            return tenantResult.Error;

        var tenant = tenantResult.Value;
        tenants.Add(tenant);

        // Clone global role templates into tenant-owned roles + permissions.
        var templates = await roleTemplates.GetTemplatesAsync(ct);
        TenantRole? ownerRole = null;
        foreach (var template in templates)
        {
            var tenantRole = new TenantRole(tenant.Id, template.Name, template.RoleId, isSystem: true);
            memberships.AddTenantRole(tenantRole);
            foreach (var permissionId in template.PermissionIds)
                memberships.AddTenantPermission(new TenantPermission(tenantRole.Id, permissionId));

            if (template.Name == IdentitySeeder.OwnerRoleName)
                ownerRole = tenantRole;
        }

        if (ownerRole is null)
            return IdentityErrors.OwnerRoleMissing;

        // Add the owner as a member with the Owner role.
        var membership = new TenantUser(tenant.Id, owner.Id);
        memberships.AddTenantUser(membership);
        memberships.AddTenantUserRole(new TenantUserRole(membership.Id, ownerRole.Id));

        await tenants.SaveChangesAsync(ct);

        return mapper.Map<TenantResponse>(tenant);
    }
}
