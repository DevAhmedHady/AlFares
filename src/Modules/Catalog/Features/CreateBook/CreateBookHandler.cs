using BuildingBlocks.Messaging;
using Catalog.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Features.CreateBook;

public sealed class CreateBookHandler(IBookRepository repo)
    : ICommandHandler<CreateBookCommand, CreateBookResponse>
{
    public async Task<Result<CreateBookResponse>> Handle(CreateBookCommand c, CancellationToken ct)
    {
        var bookResult = Book.Create(c.Title, c.Author, c.Isbn, c.PublishedOn, c.Price);
        if (bookResult.IsFailure)
            return bookResult.Error;

        var book = bookResult.Value;

        if (await repo.ExistsByIsbnAsync(book.Isbn.Value, ct))
            return BookErrors.IsbnTaken;

        repo.Add(book);
        await repo.SaveChangesAsync(ct);

        return new CreateBookResponse(book.Id);
    }
}
