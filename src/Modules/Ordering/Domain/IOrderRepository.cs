namespace Ordering.Domain;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    void Add(Order order);
    Task SaveChangesAsync(CancellationToken ct);
}
