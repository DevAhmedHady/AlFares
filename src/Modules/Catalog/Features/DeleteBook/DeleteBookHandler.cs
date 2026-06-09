using BuildingBlocks.Messaging;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Features.DeleteBook;

public sealed class DeleteBookHandler(IBookRepository repo)
    : ICommandHandler<DeleteBookCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteBookCommand c, CancellationToken ct)
    {
        var book = await repo.GetByIdAsync(c.Id, ct);
        if (book is null)
            return BookErrors.NotFound(c.Id);

        repo.Remove(book);
        await repo.SaveChangesAsync(ct);
        return true;
    }
}
