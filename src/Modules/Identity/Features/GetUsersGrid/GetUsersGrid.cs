using System.Linq.Expressions;
using BuildingBlocks.Grids;
using BuildingBlocks.Messaging;
using Identity.Contracts;
using Identity.Domain;
using Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Identity.Features.GetUsersGrid;

/// <summary>Defines the admin users grid source of truth (field allow-list + projection).</summary>
public static class UserGrid
{
    /// <summary>Allow-listed grid fields.</summary>
    public static readonly GridFieldMap<User> Fields = new(new[]
    {
        (new GridField("email", "البريد الإلكتروني", GridFieldType.Text, true),
            (Expression<Func<User, object?>>)(x => x.Email.Value)),
        (new GridField("displayName", "الاسم المعروض", GridFieldType.Text, true), x => x.DisplayName),
        (new GridField("isActive", "نشط", GridFieldType.Boolean, false), x => x.IsActive),
        (new GridField("createdAt", "تاريخ الإنشاء", GridFieldType.Date, false), x => x.CreatedAtUtc)
    });

    /// <summary>Server-side response projection.</summary>
    public static readonly Expression<Func<User, UserResponse>> Projection =
        x => new UserResponse(x.Id, x.Email.Value, x.DisplayName, x.IsActive, x.CreatedAtUtc);
}

/// <summary>Gets a paged admin users grid.</summary>
/// <param name="Grid">Grid query (paging, sort, filter, search).</param>
public sealed record GetUsersGridQuery(GridQuery Grid) : IQuery<PagedResult<UserResponse>>;

/// <summary>Handles admin users grid queries.</summary>
public sealed class GetUsersGridHandler(IMainDbContext dbContext)
    : IQueryHandler<GetUsersGridQuery, PagedResult<UserResponse>>
{
    private readonly IMainDbContext dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public async Task<Result<PagedResult<UserResponse>>> Handle(GetUsersGridQuery query, CancellationToken ct)
    {
        var applied = dbContext.Set<User>().AsNoTracking().ApplyGridQuery(query.Grid, UserGrid.Fields);
        if (applied.IsFailure) return applied.Error;
        return await applied.Value.ToPagedResultAsync(query.Grid, UserGrid.Projection, ct).ConfigureAwait(false);
    }
}
