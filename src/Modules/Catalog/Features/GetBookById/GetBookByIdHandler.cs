using BuildingBlocks.Messaging;
using Catalog.Contracts;
using Catalog.Domain;
using MapsterMapper;
using SharedKernel;

namespace Catalog.Features.GetBookById;

public sealed class GetBookByIdHandler(IBookRepository repo, IMapper mapper)
    : IQueryHandler<GetBookByIdQuery, BookResponse>
{
    public async Task<Result<BookResponse>> Handle(GetBookByIdQuery q, CancellationToken ct)
    {
        var book = await repo.GetByIdAsync(q.Id, ct);
        return book is null ? BookErrors.NotFound(q.Id) : mapper.Map<BookResponse>(book);
    }
}
