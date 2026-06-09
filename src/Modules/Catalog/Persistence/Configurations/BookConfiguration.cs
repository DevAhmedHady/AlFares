using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Catalog.Domain;

namespace Catalog.Persistence.Configurations;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> b)
    {
        b.ToTable("books");
        b.HasKey(x => x.Id);

        // Value objects mapped to single columns via converters.
        b.Property(x => x.Title)
            .HasConversion(t => t.Value, v => Title.FromPersisted(v))
            .HasColumnName("title")
            .HasMaxLength(Title.MaxLength)
            .IsRequired();

        b.Property(x => x.Author)
            .HasColumnName("author")
            .HasMaxLength(150)
            .IsRequired();

        b.Property(x => x.Isbn)
            .HasConversion(i => i.Value, v => Isbn.FromPersisted(v))
            .HasColumnName("isbn")
            .HasMaxLength(20)
            .IsRequired();
        b.HasIndex(x => x.Isbn).IsUnique();

        b.Property(x => x.PublishedOn).HasColumnName("published_on");

        b.Property(x => x.Price)
            .HasConversion(m => m.Amount, v => Money.FromPersisted(v))
            .HasColumnName("price")
            .HasPrecision(10, 2);

        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        // Domain events are not persisted.
        b.Ignore(x => x.DomainEvents);
    }
}
