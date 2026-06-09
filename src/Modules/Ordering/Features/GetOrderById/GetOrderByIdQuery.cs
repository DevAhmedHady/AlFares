using BuildingBlocks.Messaging;
using Ordering.Contracts;

namespace Ordering.Features.GetOrderById;

public sealed record GetOrderByIdQuery(Guid Id) : IQuery<OrderResponse>;
