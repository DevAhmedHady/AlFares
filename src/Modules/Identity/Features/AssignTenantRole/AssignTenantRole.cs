using BuildingBlocks.Messaging;
using FluentValidation;
using Identity.Domain;
using SharedKernel;

namespace Identity.Features.AssignTenantRole;

public sealed record AssignTenantRoleCommand(Guid TenantId, Guid UserId, string RoleName)
    : ICommand<bool>;

public sealed class AssignTenantRoleValidator : AbstractValidator<AssignTenantRoleCommand>
{
    public AssignTenantRoleValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleName).NotEmpty();
    }
}

public sealed class AssignTenantRoleHandler(IMembershipRepository memberships)
    : ICommandHandler<AssignTenantRoleCommand, bool>
{
    public async Task<Result<bool>> Handle(AssignTenantRoleCommand c, CancellationToken ct)
    {
        var membership = await memberships.GetActiveMembershipAsync(c.TenantId, c.UserId, ct);
        if (membership is null)
            return IdentityErrors.MembershipNotFound;

        var role = await memberships.GetTenantRoleByNameAsync(c.TenantId, c.RoleName, ct);
        if (role is null)
            return IdentityErrors.TenantRoleNotFound;

        if (await memberships.HasTenantUserRoleAsync(membership.Id, role.Id, ct))
            return true; // already assigned, idempotent

        memberships.AddTenantUserRole(new TenantUserRole(membership.Id, role.Id));
        await memberships.SaveChangesAsync(ct);
        return true;
    }
}
