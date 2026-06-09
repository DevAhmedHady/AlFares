using BuildingBlocks.Messaging;
using FluentValidation;
using Identity.Contracts;
using Identity.Domain;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Identity.Features.Register;

public sealed record RegisterCommand(string Email, string Password, string DisplayName)
    : ICommand<RegisterResponse>;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(150);
    }
}

public sealed class RegisterHandler(IUserRepository users, IPasswordHasher<User> hasher)
    : ICommandHandler<RegisterCommand, RegisterResponse>
{
    public async Task<Result<RegisterResponse>> Handle(RegisterCommand c, CancellationToken ct)
    {
        var emailResult = Email.Create(c.Email);
        if (emailResult.IsFailure)
            return emailResult.Error;

        if (await users.ExistsByEmailAsync(emailResult.Value.Value, ct))
            return IdentityErrors.EmailTaken;

        var userResult = User.Create(emailResult.Value, c.DisplayName);
        if (userResult.IsFailure)
            return userResult.Error;

        var user = userResult.Value;
        user.SetPasswordHash(hasher.HashPassword(user, c.Password));

        users.Add(user);
        await users.SaveChangesAsync(ct);

        return new RegisterResponse(user.Id);
    }
}
