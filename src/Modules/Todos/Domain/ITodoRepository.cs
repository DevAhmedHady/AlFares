namespace Todos.Domain;

/// <summary>Todo repository.</summary>
public interface ITodoRepository
{
    /** <summary>Gets.</summary> */Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken ct);

    /** <summary>Adds.</summary> */void Add(TodoItem item);

    /** <summary>Removes.</summary> */void Remove(TodoItem item);

    /** <summary>Saves.</summary> */Task SaveChangesAsync(CancellationToken ct);
}
