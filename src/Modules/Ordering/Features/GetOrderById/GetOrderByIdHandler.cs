using BuildingBlocks.Messaging;
using MapsterMapper;
using Ordering.Contracts;
using Ordering.Domain;
using SharedKernel;

namespace Ordering.Features.GetOrderById;

public sealed class GetOrderByIdHandler(IOrderRepository repo, IMapper mapper)
    : IQueryHandler<GetOrderByIdQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery q, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(q.Id, ct);
        return order is null ? OrderErrors.NotFound(q.Id) : mapper.Map<OrderResponse>(order);
    }
}
