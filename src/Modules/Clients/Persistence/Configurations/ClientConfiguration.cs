using Clients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Clients.Persistence.Configurations;
/// <summary>Configures Client persistence.</summary>
public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AccountBalance).HasPrecision(18, 2);
        builder.Property(x => x.ActivityLevel).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.OwnsOne(x => x.Contact, contact =>
        {
            contact.Property(x => x.Name).HasColumnName("contact_name").HasMaxLength(150).IsRequired();
            contact.Property(x => x.Phone).HasColumnName("contact_phone").HasMaxLength(50).IsRequired();
            contact.Property(x => x.Email).HasColumnName("contact_email").HasMaxLength(320);
        });
        builder.HasIndex(x => x.Name); builder.HasIndex(x => x.Status); builder.HasIndex(x => x.CreatedAtUtc);
    }
}
