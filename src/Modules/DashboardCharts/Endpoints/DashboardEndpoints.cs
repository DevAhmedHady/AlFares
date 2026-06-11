using BuildingBlocks.Authentication;
using BuildingBlocks.Endpoints;
using BuildingBlocks.Http;
using DashboardCharts.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DashboardCharts.Endpoints;

/// <summary>Dashboard endpoints.</summary>
public sealed class DashboardEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/dashboard").WithTags("Dashboard");
        g.MapGet("/datasources", (DashboardService s) => Results.Ok(s.Datasources()))
            .RequirePermission("dashboard.charts.manage");
        g.MapGet(
                "/charts",
                async (DashboardService s, CancellationToken ct) =>
                    (await s.ListAsync(ct)).ToHttpResult()
            )
            .RequirePermission("dashboard.charts.read");
        g.MapPost(
                "/charts",
                async (ChartRequest r, DashboardService s, CancellationToken ct) =>
                    (await s.CreateAsync(r, ct)).ToHttpResult(v =>
                        Results.Created($"/api/dashboard/charts/{v.Id}", v)
                    )
            )
            .RequirePermission("dashboard.charts.manage");
        g.MapPut(
                "/charts/{id:guid}",
                async (Guid id, ChartRequest r, DashboardService s, CancellationToken ct) =>
                    (await s.UpdateAsync(id, r, ct)).ToHttpResult()
            )
            .RequirePermission("dashboard.charts.manage");
        g.MapDelete(
                "/charts/{id:guid}",
                async (Guid id, DashboardService s, CancellationToken ct) =>
                    (await s.DeleteAsync(id, ct)).ToHttpResult()
            )
            .RequirePermission("dashboard.charts.manage");
        g.MapGet(
                "/charts/{id:guid}/data",
                async (Guid id, DashboardService s, CancellationToken ct) =>
                    (await s.DataAsync(id, ct)).ToHttpResult()
            )
            .RequirePermission("dashboard.charts.read");
        g.MapPost(
                "/charts/preview",
                async (ChartRequest r, DashboardService s, CancellationToken ct) =>
                    (await s.PreviewAsync(r, ct)).ToHttpResult()
            )
            .RequirePermission("dashboard.charts.manage");
    }
}
