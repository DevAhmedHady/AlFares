using Identity.Contracts;
using Identity.Domain;
using Identity.Features.AddTenantUser;
using Identity.Features.AssignTenantRole;
using Identity.Features.Login;
using Identity.Features.Logout;
using Identity.Features.ProvisionTenant;
using Identity.Features.Refresh;
using Identity.Features.Register;
using Mapster;

namespace Identity.Mapping;

public sealed class IdentityMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Outbound
        config.NewConfig<Tenant, TenantResponse>().Map(d => d.Slug, s => s.Slug.Value);

        // Inbound: request -> command
        config.NewConfig<RegisterRequest, RegisterCommand>();
        config.NewConfig<LoginRequest, LoginCommand>();
        config.NewConfig<RefreshRequest, RefreshCommand>();
        config.NewConfig<LogoutRequest, LogoutCommand>();
        config.NewConfig<ProvisionTenantRequest, ProvisionTenantCommand>();
        config.NewConfig<AddTenantUserRequest, AddTenantUserCommand>();
        config.NewConfig<AssignTenantRoleRequest, AssignTenantRoleCommand>();
    }
}
