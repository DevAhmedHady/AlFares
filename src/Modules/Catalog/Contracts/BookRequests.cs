namespace Catalog.Contracts;

// Inbound transport DTOs. Endpoints bind these, then Mapster maps them to application commands.
public sealed record CreateBookRequest(
    string Title,
    string Author,
    string Isbn,
    DateOnly PublishedOn,
    decimal Price);

public sealed record UpdateBookRequest(
    string Title,
    string Author,
    string Isbn,
    DateOnly PublishedOn,
    decimal Price);

public sealed record CreateBookResponse(Guid Id);
