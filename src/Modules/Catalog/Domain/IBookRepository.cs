namespace Catalog.Domain;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken ct);
    Task<bool> ExistsByIsbnAsync(string isbn, CancellationToken ct);
    void Add(Book book);
    void Remove(Book book);
    Task SaveChangesAsync(CancellationToken ct);
}
