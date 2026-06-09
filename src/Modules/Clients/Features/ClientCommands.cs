using BuildingBlocks.Messaging;
using Clients.Contracts;
using Clients.Domain;
namespace Clients.Features;
/// <summary>Create command.</summary>
public sealed record CreateClientCommand(string Name, string ContactName, string Phone, string? Email, decimal AccountBalance, ActivityLevel ActivityLevel, string? Notes) : ICommand<ClientResponse>;
/// <summary>Update command.</summary>
public sealed record UpdateClientCommand(Guid Id, string Name, string ContactName, string Phone, string? Email, decimal AccountBalance, ActivityLevel ActivityLevel, string? Notes) : ICommand<ClientResponse>;
/// <summary>Status command.</summary>
public sealed record SetClientStatusCommand(Guid Id, ClientStatus Status) : ICommand<ClientResponse>;
/// <summary>Delete command.</summary>
public sealed record DeleteClientCommand(Guid Id) : ICommand<bool>;
/// <summary>Get query.</summary>
public sealed record GetClientByIdQuery(Guid Id) : IQuery<ClientResponse>;
