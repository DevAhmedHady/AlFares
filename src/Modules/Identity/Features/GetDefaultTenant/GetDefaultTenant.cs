using BuildingBlocks.Messaging;
using Identity.Contracts;
using Identity.Domain;
using Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Identity.Features.GetDefaultTenant;

/// <summary>
/// Resolves the single active tenant so the single-tenant SPA can auto-select it at login,
/// before any token exists. Returns a NotFound error when no tenant has been provisioned.
/// </summary>
public sealed record GetDefaultTenantQuery : IQuery<TenantResponse>;

/// <summary>Handles <see cref="GetDefaultTenantQuery"/> over the active tenants.</summary>
public sealed class GetDefaultTenantHandler(IMainDbContext dbContext)
    : IQueryHandler<GetDefaultTenantQuery, TenantResponse>
{
    private readonly IMainDbContext dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public async Task<Result<TenantResponse>> Handle(GetDefaultTenantQuery query, CancellationToken ct)
    {
        var tenant = await dbContext.Set<Tenant>().AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.CreatedAtUtc)
            .Select(t => new TenantResponse(t.Id, t.Name, t.Slug.Value))
            .FirstOrDefaultAsync(ct);

        return tenant is not null
            ? Result.Success(tenant)
            : Result.Failure<TenantResponse>(Error.NotFound("tenant.none", "No tenant has been provisioned."));
    }
}
