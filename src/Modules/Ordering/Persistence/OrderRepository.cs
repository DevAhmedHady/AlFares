using Microsoft.EntityFrameworkCore;
using Ordering.Domain;

namespace Ordering.Persistence;

public sealed class OrderRepository(OrderingDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public void Add(Order order) => db.Orders.Add(order);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
