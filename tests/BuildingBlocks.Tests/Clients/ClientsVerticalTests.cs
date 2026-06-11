using BuildingBlocks.Charts;
using BuildingBlocks.Export;
using BuildingBlocks.Grids;
using Clients.Charts;
using Clients.Domain;
using Clients.Features;
using Clients.Mapping;
using Clients.Persistence;
using ClosedXML.Excel;
using FluentAssertions;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildingBlocks.Tests.Clients;

/// <summary>Verifies Clients reference vertical slice.</summary>
[TestClass]
public sealed class ClientsVerticalTests
{
    /// <summary>Verifies create, filtered grid, and Excel export flow.</summary>
    [TestMethod]
    public async Task CreateGridExport_ReturnsCreatedFilteredClient()
    {
        await using var db = CreateDb();
        var mapper = CreateMapper();
        var create = new CreateClientHandler(new ClientRepository(db), mapper);
        var created = await create.Handle(
            new CreateClientCommand(
                "شركة الاختبار",
                "أحمد",
                "0100",
                "a@test.local",
                500m,
                ActivityLevel.High,
                null
            ),
            default
        );
        created.IsSuccess.Should().BeTrue();

        var grid = await new GetClientsGridHandler(db).Handle(
            new GetClientsGridQuery(
                new GridQuery
                {
                    Filters = [new GridFilter("status", GridFilterOp.Eq, "Active")],
                    Sort = [new GridSort("accountBalance", true)],
                }
            ),
            default
        );
        grid.Value.Items.Should().ContainSingle().Which.Name.Should().Be("شركة الاختبار");

        var bytes = new ExcelGridExporter().Export(
            grid.Value.Items,
            [
                new ExportColumn(
                    nameof(global::Clients.Contracts.ClientResponse.Name),
                    "اسم العميل",
                    GridFieldType.Text
                ),
            ],
            "العملاء"
        );
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        workbook.Worksheet("Data").Cell(3, 1).GetString().Should().Be("شركة الاختبار");
    }

    /// <summary>Verifies chart status buckets.</summary>
    [TestMethod]
    public async Task Chart_StatusCount_ReturnsBuckets()
    {
        await using var db = CreateDb();
        db.Set<Client>().Add(Create("نشط", ClientStatus.Active));
        db.Set<Client>().Add(Create("غير نشط", ClientStatus.Inactive));
        db.Set<Client>().Add(Create("نشط 2", ClientStatus.Active));
        await db.SaveChangesAsync();
        var series = await new ClientsChartDataSource(db).ComputeAsync(
            new ChartComputeRequest("status", null, ChartAggregation.Count, []),
            default
        );
        series.Points.Should().Contain(x => x.Label == "Active" && x.Value == 2);
        series.Points.Should().Contain(x => x.Label == "Inactive" && x.Value == 1);
    }

    /// <summary>Verifies invalid balance fails.</summary>
    [TestMethod]
    public void Create_NegativeBalance_Fails() =>
        Client
            .Create("x", Contact.Create("n", "p", null).Value, -1, ActivityLevel.Low, null)
            .IsFailure.Should()
            .BeTrue();

    private static global::Api.Persistence.MainDbContext CreateDb() => MainDbTestFactory.Create();

    private static IMapper CreateMapper()
    {
        var config = new TypeAdapterConfig();
        new ClientsMappingConfig().Register(config);
        return new Mapper(config);
    }

    private static Client Create(string name, ClientStatus status)
    {
        var client = Client
            .Create(
                name,
                Contact.Create("contact", "phone", null).Value,
                10,
                ActivityLevel.Medium,
                null
            )
            .Value;
        client.SetStatus(status);
        return client;
    }
}
