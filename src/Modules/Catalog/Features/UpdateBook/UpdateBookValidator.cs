using FluentValidation;

namespace Catalog.Features.UpdateBook;

public sealed class UpdateBookValidator : AbstractValidator<UpdateBookCommand>
{
    public UpdateBookValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Author).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Isbn).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}
