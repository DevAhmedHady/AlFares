using FluentValidation;

namespace Catalog.Features.CreateBook;

// Cheap shape checks; deep domain invariants live in the Book aggregate.
public sealed class CreateBookValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Author).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Isbn).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}
