using Clients.Domain;
using Microsoft.EntityFrameworkCore;
namespace Clients.Persistence;
/// <summary>EF Core Client repository.</summary>
public sealed class ClientRepository(ClientsDbContext dbContext) : IClientRepository
{
    private readonly ClientsDbContext dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    /// <inheritdoc />
    public Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => dbContext.Clients.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    /// <inheritdoc />
    public void Add(Client client) => dbContext.Clients.Add(client);
    /// <inheritdoc />
    public void Remove(Client client) => dbContext.Clients.Remove(client);
    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
}
