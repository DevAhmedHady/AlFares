using Expenses.Domain; using Microsoft.EntityFrameworkCore;
namespace Expenses.Persistence;
/** <summary>Expenses context.</summary> */
public sealed class ExpensesDbContext(DbContextOptions<ExpensesDbContext> options):DbContext(options){/** <summary>Schema.</summary> */ public const string Schema="expenses"; /** <summary>Expenses.</summary> */ public DbSet<Expense> Expenses=>Set<Expense>(); /** <inheritdoc/> */ protected override void OnModelCreating(ModelBuilder modelBuilder){modelBuilder.HasDefaultSchema(Schema);modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpensesDbContext).Assembly);}}

