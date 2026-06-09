using Microsoft.EntityFrameworkCore;
using Catalog.Domain;

namespace Catalog.Persistence;

public sealed class BookRepository(CatalogDbContext db) : IBookRepository
{
    public Task<Book?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Books.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken ct) =>
        await db.Books.AsNoTracking().OrderBy(b => b.Title).ToListAsync(ct);

    public Task<bool> ExistsByIsbnAsync(string isbn, CancellationToken ct) =>
        db.Books.AnyAsync(b => b.Isbn == Isbn.FromPersisted(isbn), ct);

    public void Add(Book book) => db.Books.Add(book);

    public void Remove(Book book) => db.Books.Remove(book);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
