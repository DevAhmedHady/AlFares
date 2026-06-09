using BuildingBlocks.Messaging;
using Catalog.Contracts;

namespace Catalog.Features.GetAllBooks;

public sealed record GetAllBooksQuery : IQuery<IReadOnlyList<BookResponse>>;
