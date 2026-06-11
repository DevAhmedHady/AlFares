using FluentValidation;

namespace Clients.Features;

/// <summary>Validates create requests.</summary>
public sealed class CreateClientValidator : AbstractValidator<CreateClientCommand>
{
    /// <summary>Initializes rules.</summary>
    public CreateClientValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AccountBalance).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Validates update requests.</summary>
public sealed class UpdateClientValidator : AbstractValidator<UpdateClientCommand>
{
    /// <summary>Initializes rules.</summary>
    public UpdateClientValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactName).NotEmpty();
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.AccountBalance).GreaterThanOrEqualTo(0);
    }
}
