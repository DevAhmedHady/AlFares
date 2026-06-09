using Mapster;
using Catalog.Contracts;
using Catalog.Domain;
using Catalog.Features.CreateBook;
using Catalog.Features.UpdateBook;

namespace Catalog.Mapping;

// Mapster profile for the Catalog module. Discovered automatically via TypeAdapterConfig.Scan.
public sealed class CatalogMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Outbound: aggregate -> response DTO, unwrapping value objects.
        config.NewConfig<Book, BookResponse>()
            .Map(d => d.Title, s => s.Title.Value)
            .Map(d => d.Isbn, s => s.Isbn.Value)
            .Map(d => d.Price, s => s.Price.Amount);

        // Inbound: transport request -> application command (primitive -> primitive).
        // UpdateBookCommand.Id has no source member, so Mapster leaves it as default(Guid);
        // the endpoint then supplies the route id via `with { Id = id }`.
        config.NewConfig<CreateBookRequest, CreateBookCommand>();
        config.NewConfig<UpdateBookRequest, UpdateBookCommand>();
    }
}
