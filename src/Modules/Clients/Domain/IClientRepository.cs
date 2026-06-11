namespace Clients.Domain;

/// <summary>Persists Client aggregates.</summary>
public interface IClientRepository
{
    /// <summary>Finds client by id.</summary>
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Adds client.</summary>
    void Add(Client client);

    /// <summary>Removes client.</summary>
    void Remove(Client client);

    /// <summary>Saves pending changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
