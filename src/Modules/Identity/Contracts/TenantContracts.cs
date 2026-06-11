namespace Identity.Contracts;

public sealed record ProvisionTenantRequest(string Name, string Slug, Guid OwnerUserId);

public sealed record TenantResponse(Guid Id, string Name, string Slug);

public sealed record AddTenantUserRequest(Guid TenantId, Guid UserId);

public sealed record AssignTenantRoleRequest(Guid TenantId, Guid UserId, string RoleName);
