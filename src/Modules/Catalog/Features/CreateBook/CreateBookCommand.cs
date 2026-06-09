using BuildingBlocks.Messaging;
using Catalog.Contracts;

namespace Catalog.Features.CreateBook;

public sealed record CreateBookCommand(
    string Title,
    string Author,
    string Isbn,
    DateOnly PublishedOn,
    decimal Price) : ICommand<CreateBookResponse>;
