using BuildingBlocks.Messaging;

namespace Catalog.Features.UpdateBook;

public sealed record UpdateBookCommand(
    Guid Id,
    string Title,
    string Author,
    string Isbn,
    DateOnly PublishedOn,
    decimal Price) : ICommand<bool>;
