using Clients.Domain;

namespace Clients.Contracts;

/// <summary>Create client request.</summary>
public sealed record CreateClientRequest(
    string Name,
    string ContactName,
    string Phone,
    string? Email,
    decimal AccountBalance,
    ActivityLevel ActivityLevel,
    string? Notes
);

/// <summary>Update client request.</summary>
public sealed record UpdateClientRequest(
    string Name,
    string ContactName,
    string Phone,
    string? Email,
    decimal AccountBalance,
    ActivityLevel ActivityLevel,
    string? Notes
);

/// <summary>Change client status request.</summary>
public sealed record SetClientStatusRequest(ClientStatus Status);

/// <summary>Client response.</summary>
public sealed record ClientResponse(
    Guid Id,
    string Name,
    string ContactName,
    string Phone,
    string? Email,
    decimal AccountBalance,
    ActivityLevel ActivityLevel,
    ClientStatus Status,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

/// <summary>Grid/export request.</summary>
public sealed record ClientExportRequest(
    BuildingBlocks.Grids.GridQuery Grid,
    BuildingBlocks.Export.ExportFormat Format
);
