using SharedKernel;

namespace Catalog.Domain;

public sealed class Book : AggregateRoot
{
    public Title Title { get; private set; } = default!;
    public string Author { get; private set; } = default!;
    public Isbn Isbn { get; private set; } = default!;
    public DateOnly PublishedOn { get; private set; }
    public Money Price { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private Book() { } // EF materialization

    private Book(Guid id, Title title, string author, Isbn isbn, DateOnly publishedOn, Money price, DateTime createdAtUtc)
        : base(id)
    {
        Title = title;
        Author = author;
        Isbn = isbn;
        PublishedOn = publishedOn;
        Price = price;
        CreatedAtUtc = createdAtUtc;
        Raise(new BookCreatedDomainEvent(id));
    }

    public static Result<Book> Create(string? title, string? author, string? isbn, DateOnly publishedOn, decimal price)
    {
        var titleResult = Title.Create(title);
        if (titleResult.IsFailure) return titleResult.Error;

        if (string.IsNullOrWhiteSpace(author)) return BookErrors.AuthorEmpty;

        var isbnResult = Isbn.Create(isbn);
        if (isbnResult.IsFailure) return isbnResult.Error;

        var priceResult = Money.Create(price);
        if (priceResult.IsFailure) return priceResult.Error;

        return new Book(Guid.NewGuid(), titleResult.Value, author.Trim(), isbnResult.Value, publishedOn, priceResult.Value, DateTime.UtcNow);
    }

    public Result UpdateDetails(string? title, string? author, string? isbn, DateOnly publishedOn, decimal price)
    {
        var titleResult = Title.Create(title);
        if (titleResult.IsFailure) return Result.Failure(titleResult.Error);

        if (string.IsNullOrWhiteSpace(author)) return Result.Failure(BookErrors.AuthorEmpty);

        var isbnResult = Isbn.Create(isbn);
        if (isbnResult.IsFailure) return Result.Failure(isbnResult.Error);

        var priceResult = Money.Create(price);
        if (priceResult.IsFailure) return Result.Failure(priceResult.Error);

        Title = titleResult.Value;
        Author = author.Trim();
        Isbn = isbnResult.Value;
        PublishedOn = publishedOn;
        Price = priceResult.Value;
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }
}

public sealed record BookCreatedDomainEvent(Guid BookId) : IDomainEvent;
