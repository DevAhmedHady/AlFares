using BuildingBlocks.Messaging;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Features.UpdateBook;

public sealed class UpdateBookHandler(IBookRepository repo)
    : ICommandHandler<UpdateBookCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateBookCommand c, CancellationToken ct)
    {
        var book = await repo.GetByIdAsync(c.Id, ct);
        if (book is null)
            return BookErrors.NotFound(c.Id);

        // Normalize incoming ISBN to detect a real change before touching the DB.
        var isbnResult = Isbn.Create(c.Isbn);
        if (isbnResult.IsFailure)
            return isbnResult.Error;

        if (isbnResult.Value.Value != book.Isbn.Value && await repo.ExistsByIsbnAsync(isbnResult.Value.Value, ct))
            return BookErrors.IsbnTaken;

        var updateResult = book.UpdateDetails(c.Title, c.Author, c.Isbn, c.PublishedOn, c.Price);
        if (updateResult.IsFailure)
            return updateResult.Error;

        await repo.SaveChangesAsync(ct);
        return true;
    }
}
