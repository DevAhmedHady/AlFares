using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BuildingBlocks.Persistence;

/// <summary>Shared unit of work backed by the application's single database context.</summary>
public interface IMainDbContext
{
    /// <summary>Gets the set for an entity type owned by any module.</summary>
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    /// <summary>Adds an entity to the context.</summary>
    EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>Removes an entity from the context.</summary>
    EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>Persists all tracked changes in one transaction.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
