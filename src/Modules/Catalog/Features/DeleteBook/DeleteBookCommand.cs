using BuildingBlocks.Messaging;

namespace Catalog.Features.DeleteBook;

public sealed record DeleteBookCommand(Guid Id) : ICommand<bool>;
