namespace Catalog.Contracts;

// Outbound DTO. Mapping from the Book aggregate (unwrapping value objects) is defined in CatalogMappingConfig.
public sealed record BookResponse(
    Guid Id,
    string Title,
    string Author,
    string Isbn,
    DateOnly PublishedOn,
    decimal Price,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
