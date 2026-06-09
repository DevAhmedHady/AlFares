using Expenses.Domain; using Microsoft.EntityFrameworkCore; using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Expenses.Persistence;
/// <summary>Expense mapping.</summary>
public sealed class ExpenseConfiguration:IEntityTypeConfiguration<Expense>{/** <inheritdoc/> */ public void Configure(EntityTypeBuilder<Expense> b){b.ToTable("expenses");b.HasKey(x=>x.Id);b.Property(x=>x.Amount).HasPrecision(18,2);b.Property(x=>x.Payee).HasMaxLength(200).IsRequired();b.Property(x=>x.Notes).HasMaxLength(2000);b.Property(x=>x.OwnerType).HasConversion<int>();b.HasIndex(x=>x.Date);b.HasIndex(x=>x.ExpenseTypeId);b.HasIndex(x=>new{x.OwnerType,x.OwnerId});b.HasOne<ExpenseType>().WithMany().HasForeignKey(x=>x.ExpenseTypeId).OnDelete(DeleteBehavior.Restrict);}}
/// <summary>Expense type mapping.</summary>
public sealed class ExpenseTypeConfiguration:IEntityTypeConfiguration<ExpenseType>{/** <inheritdoc/> */ public void Configure(EntityTypeBuilder<ExpenseType>b){b.ToTable("expense_types");b.HasKey(x=>x.Id);b.Property(x=>x.Name).HasMaxLength(100).IsRequired();b.Property(x=>x.Scope).HasConversion<int>();b.HasIndex(x=>x.Name).IsUnique();}}
