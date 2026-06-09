using Clients.Contracts;
using Clients.Domain;
using Clients.Features;
using Mapster;
namespace Clients.Mapping;
/// <summary>Configures Clients Mapster mappings.</summary>
public sealed class ClientsMappingConfig : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Client, ClientResponse>()
            .Map(d => d.ContactName, s => s.Contact.Name).Map(d => d.Phone, s => s.Contact.Phone).Map(d => d.Email, s => s.Contact.Email);
        config.NewConfig<CreateClientRequest, CreateClientCommand>();
    }
}
