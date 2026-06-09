
using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore.Design;using Microsoft.EntityFrameworkCore.Metadata.Builders;using Microsoft.Extensions.Configuration;using Todos.Domain;
namespace Todos.Persistence;
/** <summary>Todo mapping.</summary> */ public sealed class TodoConfiguration:IEntityTypeConfiguration<TodoItem>{/** <inheritdoc/> */ public void Configure(EntityTypeBuilder<TodoItem>b){b.ToTable("todo_items");b.HasKey(x=>x.Id);b.Property(x=>x.Title).HasMaxLength(300).IsRequired();b.Property(x=>x.Status).HasConversion<string>();b.Property(x=>x.Priority).HasConversion<string>();b.Property(x=>x.Notes).HasMaxLength(2000);b.HasIndex(x=>x.DueDate);b.HasIndex(x=>x.Status);}}
/** <summary>Repository.</summary> */ public sealed class TodoRepository(IMainDbContext db):ITodoRepository{/** <inheritdoc/> */ public Task<TodoItem?> GetByIdAsync(Guid id,CancellationToken ct)=>db.Set<TodoItem>().SingleOrDefaultAsync(x=>x.Id==id,ct);/** <inheritdoc/> */ public void Add(TodoItem i)=>db.Set<TodoItem>().Add(i);/** <inheritdoc/> */ public void Remove(TodoItem i)=>db.Set<TodoItem>().Remove(i);/** <inheritdoc/> */ public async Task SaveChangesAsync(CancellationToken ct)=>await db.SaveChangesAsync(ct);}


