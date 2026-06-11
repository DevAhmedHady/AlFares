using BuildingBlocks.Messaging;
using FluentValidation;
using Identity.Domain;
using SharedKernel;

namespace Identity.Features.AddTenantUser;

public sealed record AddTenantUserCommand(Guid TenantId, Guid UserId) : ICommand<bool>;

public sealed class AddTenantUserValidator : AbstractValidator<AddTenantUserCommand>
{
    public AddTenantUserValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class AddTenantUserHandler(
    ITenantRepository tenants,
    IUserRepository users,
    IMembershipRepository memberships
) : ICommandHandler<AddTenantUserCommand, bool>
{
    public async Task<Result<bool>> Handle(AddTenantUserCommand c, CancellationToken ct)
    {
        if (await tenants.GetByIdAsync(c.TenantId, ct) is null)
            return IdentityErrors.TenantNotFound(c.TenantId);

        if (await users.GetByIdAsync(c.UserId, ct) is null)
            return IdentityErrors.UserNotFound(c.UserId);

        if (await memberships.GetActiveMembershipAsync(c.TenantId, c.UserId, ct) is not null)
            return IdentityErrors.AlreadyMember;

        memberships.AddTenantUser(new TenantUser(c.TenantId, c.UserId));
        await memberships.SaveChangesAsync(ct);
        return true;
    }
}
