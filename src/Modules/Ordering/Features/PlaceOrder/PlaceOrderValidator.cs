using FluentValidation;

namespace Ordering.Features.PlaceOrder;

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
    }
}
