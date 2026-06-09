
using Expenses.Domain; using Microsoft.EntityFrameworkCore;
namespace Expenses.Persistence; /** <summary>Expense repository.</summary> */ public sealed class ExpenseRepository(IMainDbContext db):IExpenseRepository{private readonly IMainDbContext db=db??throw new ArgumentNullException(nameof(db));/** <inheritdoc/> */ public Task<Expense?> GetByIdAsync(Guid id,CancellationToken ct)=>db.Set<Expense>().SingleOrDefaultAsync(x=>x.Id==id,ct);/** <inheritdoc/> */ public void Add(Expense e)=>db.Set<Expense>().Add(e);/** <inheritdoc/> */ public void Remove(Expense e)=>db.Set<Expense>().Remove(e);/** <inheritdoc/> */ public async Task SaveChangesAsync(CancellationToken ct)=>await db.SaveChangesAsync(ct).ConfigureAwait(false);}


