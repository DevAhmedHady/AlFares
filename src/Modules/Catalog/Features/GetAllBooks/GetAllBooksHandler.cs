using BuildingBlocks.Messaging;
using Catalog.Contracts;
using Catalog.Domain;
using MapsterMapper;
using SharedKernel;

namespace Catalog.Features.GetAllBooks;

public sealed class GetAllBooksHandler(IBookRepository repo, IMapper mapper)
    : IQueryHandler<GetAllBooksQuery, IReadOnlyList<BookResponse>>
{
    public async Task<Result<IReadOnlyList<BookResponse>>> Handle(GetAllBooksQuery q, CancellationToken ct)
    {
        var books = await repo.GetAllAsync(ct);
        return mapper.Map<List<BookResponse>>(books);
    }
}
