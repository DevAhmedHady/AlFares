using BuildingBlocks.Charts;
using Clients.Charts;
using Clients.Domain;
using Clients.Persistence;
using DashboardCharts.Domain;
using DashboardCharts.Features;
using DashboardCharts.Persistence;
using DashboardCharts.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildingBlocks.Tests.Dashboard;

/// <summary>Verifies dashboard workflows without business-module references from DashboardCharts.</summary>
[TestClass]
public sealed class DashboardTests
{
    /// <summary>Verifies preview, persistence, saved data, and missing datasource behavior.</summary>
    [TestMethod]
    public async Task PreviewSaveAndLoadData_UsesRegisteredDatasource()
    {
        await using var clientsDb = CreateClientsDb();
        clientsDb.Set<Client>().Add(CreateClient("Active one", ClientStatus.Active));
        clientsDb.Set<Client>().Add(CreateClient("Active two", ClientStatus.Active));
        clientsDb.Set<Client>().Add(CreateClient("Inactive", ClientStatus.Inactive));
        await clientsDb.SaveChangesAsync();

        await using var dashboardDb = CreateDashboardDb();
        var repository = new ChartDefinitionRepository(dashboardDb);
        var registry = new ChartDataSourceRegistry([new ClientsChartDataSource(clientsDb)]);
        var service = new DashboardService(repository, registry);
        var request = new ChartRequest(
            "Client status",
            ChartType.Pie,
            "clients",
            "status",
            null,
            ChartAggregation.Count,
            "[]",
            null,
            0
        );

        var preview = await service.PreviewAsync(request, default);
        preview.IsSuccess.Should().BeTrue();
        preview.Value.Points.Should().Contain(x => x.Label == "Active" && x.Value == 2);
        preview.Value.Points.Should().Contain(x => x.Label == "Inactive" && x.Value == 1);

        var created = await service.CreateAsync(request, default);
        created.IsSuccess.Should().BeTrue();
        var listed = await service.ListAsync(default);
        listed.Value.Should().ContainSingle(x => x.Id == created.Value.Id);

        var savedData = await service.DataAsync(created.Value.Id, default);
        savedData.IsSuccess.Should().BeTrue();
        savedData.Value.Points.Should().BeEquivalentTo(preview.Value.Points);

        var serviceWithoutDatasource = new DashboardService(
            repository,
            new ChartDataSourceRegistry([])
        );
        var missing = await serviceWithoutDatasource.DataAsync(created.Value.Id, default);
        missing.IsFailure.Should().BeTrue();
        missing.Error.Code.Should().Be("dashboard.datasource_not_found");
    }

    private static global::Api.Persistence.MainDbContext CreateClientsDb() =>
        MainDbTestFactory.Create();

    private static global::Api.Persistence.MainDbContext CreateDashboardDb() =>
        MainDbTestFactory.Create();

    private static Client CreateClient(string name, ClientStatus status)
    {
        var client = Client
            .Create(
                name,
                Contact.Create("Contact", "0100", null).Value,
                10,
                ActivityLevel.Medium,
                null
            )
            .Value;
        client.SetStatus(status);
        return client;
    }
}
