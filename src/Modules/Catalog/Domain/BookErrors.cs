using SharedKernel;

namespace Catalog.Domain;

public static class BookErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Book.NotFound", $"Book with id '{id}' was not found.");

    public static readonly Error IsbnTaken =
        Error.Conflict("Book.IsbnTaken", "A book with this ISBN already exists.");

    public static readonly Error TitleEmpty =
        Error.Validation("Book.Title.Empty", "Title is required.");

    public static readonly Error TitleTooLong =
        Error.Validation("Book.Title.TooLong", $"Title must be at most {Title.MaxLength} characters.");

    public static readonly Error AuthorEmpty =
        Error.Validation("Book.Author.Empty", "Author is required.");

    public static readonly Error IsbnEmpty =
        Error.Validation("Book.Isbn.Empty", "ISBN is required.");

    public static readonly Error IsbnInvalid =
        Error.Validation("Book.Isbn.Invalid", "ISBN must be a valid ISBN-10 or ISBN-13.");

    public static readonly Error PriceNegative =
        Error.Validation("Book.Price.Negative", "Price cannot be negative.");
}
