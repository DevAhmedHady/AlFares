using BuildingBlocks.Messaging;
using Clients.Contracts;
using Clients.Domain;
using MapsterMapper;
using SharedKernel;

namespace Clients.Features;

/// <summary>Creates clients.</summary>
public sealed class CreateClientHandler(IClientRepository repository, IMapper mapper)
    : ICommandHandler<CreateClientCommand, ClientResponse>
{
    private readonly IClientRepository repository =
        repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IMapper mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    /// <inheritdoc />
    public async Task<Result<ClientResponse>> Handle(
        CreateClientCommand command,
        CancellationToken ct
    )
    {
        var contact = Contact.Create(command.ContactName, command.Phone, command.Email);
        if (contact.IsFailure)
            return contact.Error;
        var client = Client.Create(
            command.Name,
            contact.Value,
            command.AccountBalance,
            command.ActivityLevel,
            command.Notes
        );
        if (client.IsFailure)
            return client.Error;
        repository.Add(client.Value);
        await repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return mapper.Map<ClientResponse>(client.Value);
    }
}

/// <summary>Updates clients.</summary>
public sealed class UpdateClientHandler(IClientRepository repository, IMapper mapper)
    : ICommandHandler<UpdateClientCommand, ClientResponse>
{
    /// <inheritdoc />
    public async Task<Result<ClientResponse>> Handle(
        UpdateClientCommand command,
        CancellationToken ct
    )
    {
        var client = await repository.GetByIdAsync(command.Id, ct).ConfigureAwait(false);
        if (client is null)
            return ClientErrors.NotFound(command.Id);
        var contact = Contact.Create(command.ContactName, command.Phone, command.Email);
        if (contact.IsFailure)
            return contact.Error;
        var updated = client.Update(
            command.Name,
            contact.Value,
            command.AccountBalance,
            command.ActivityLevel,
            command.Notes
        );
        if (updated.IsFailure)
            return updated.Error;
        await repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return mapper.Map<ClientResponse>(client);
    }
}

/// <summary>Changes client status.</summary>
public sealed class SetClientStatusHandler(IClientRepository repository, IMapper mapper)
    : ICommandHandler<SetClientStatusCommand, ClientResponse>
{
    /// <inheritdoc />
    public async Task<Result<ClientResponse>> Handle(
        SetClientStatusCommand command,
        CancellationToken ct
    )
    {
        var client = await repository.GetByIdAsync(command.Id, ct).ConfigureAwait(false);
        if (client is null)
            return ClientErrors.NotFound(command.Id);
        client.SetStatus(command.Status);
        await repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return mapper.Map<ClientResponse>(client);
    }
}

/// <summary>Deletes clients.</summary>
public sealed class DeleteClientHandler(IClientRepository repository)
    : ICommandHandler<DeleteClientCommand, bool>
{
    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteClientCommand command, CancellationToken ct)
    {
        var client = await repository.GetByIdAsync(command.Id, ct).ConfigureAwait(false);
        if (client is null)
            return ClientErrors.NotFound(command.Id);
        repository.Remove(client);
        await repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}

/// <summary>Gets clients by id.</summary>
public sealed class GetClientByIdHandler(IClientRepository repository, IMapper mapper)
    : IQueryHandler<GetClientByIdQuery, ClientResponse>
{
    /// <inheritdoc />
    public async Task<Result<ClientResponse>> Handle(GetClientByIdQuery query, CancellationToken ct)
    {
        var client = await repository.GetByIdAsync(query.Id, ct).ConfigureAwait(false);
        return client is null
            ? ClientErrors.NotFound(query.Id)
            : mapper.Map<ClientResponse>(client);
    }
}
