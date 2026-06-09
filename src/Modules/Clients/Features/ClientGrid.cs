using System.Linq.Expressions;
using BuildingBlocks.Grids;
using BuildingBlocks.Messaging;
using Clients.Contracts;
using Clients.Domain;
using Clients.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
namespace Clients.Features;

/// <summary>Defines the Clients grid source of truth.</summary>
public static class ClientGrid
{
    /// <summary>Allow-listed grid fields.</summary>
    public static readonly GridFieldMap<Client> Fields = new(new[]
    {
        (new GridField("name", "اسم العميل", GridFieldType.Text, true), (Expression<Func<Client, object?>>)(x => x.Name)),
        (new GridField("contactName", "اسم جهة الاتصال", GridFieldType.Text, true), x => x.Contact.Name),
        (new GridField("phone", "الهاتف", GridFieldType.Text, true), x => x.Contact.Phone),
        (new GridField("email", "البريد الإلكتروني", GridFieldType.Text, true), x => x.Contact.Email),
        (new GridField("accountBalance", "رصيد الحساب", GridFieldType.Number, false, Chartable: true), x => x.AccountBalance),
        (new GridField("activityLevel", "مستوى النشاط", GridFieldType.Enum, false, Chartable: true), x => x.ActivityLevel),
        (new GridField("status", "الحالة", GridFieldType.Enum, false, Chartable: true), x => x.Status),
        (new GridField("createdAt", "تاريخ الإنشاء", GridFieldType.Date, false, Chartable: true), x => x.CreatedAtUtc)
    });
    /// <summary>Server-side response projection.</summary>
    public static readonly Expression<Func<Client, ClientResponse>> Projection = x => new ClientResponse(x.Id, x.Name, x.Contact.Name, x.Contact.Phone, x.Contact.Email, x.AccountBalance, x.ActivityLevel, x.Status, x.Notes, x.CreatedAtUtc, x.UpdatedAtUtc);
}

/// <summary>Gets a paged Clients grid.</summary>
public sealed record GetClientsGridQuery(GridQuery Grid) : IQuery<PagedResult<ClientResponse>>;

/// <summary>Handles Clients grid queries.</summary>
public sealed class GetClientsGridHandler(IMainDbContext dbContext) : IQueryHandler<GetClientsGridQuery, PagedResult<ClientResponse>>
{
    private readonly IMainDbContext dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    /// <inheritdoc />
    public async Task<Result<PagedResult<ClientResponse>>> Handle(GetClientsGridQuery query, CancellationToken ct)
    {
        var applied = dbContext.Set<Client>().AsNoTracking().ApplyGridQuery(query.Grid, ClientGrid.Fields);
        if (applied.IsFailure) return applied.Error;
        return await applied.Value.ToPagedResultAsync(query.Grid, ClientGrid.Projection, ct).ConfigureAwait(false);
    }
}
