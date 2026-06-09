using BuildingBlocks.Messaging;
using Catalog.Contracts;

namespace Catalog.Features.GetBookById;

public sealed record GetBookByIdQuery(Guid Id) : IQuery<BookResponse>;
