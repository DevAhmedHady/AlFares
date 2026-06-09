using BuildingBlocks.Grids;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildingBlocks.Tests.Grids;

/// <summary>Verifies safe grid query behavior.</summary>
[TestClass]
public sealed class GridQueryExtensionsTests
{
    private static readonly GridFieldMap<Row> FieldMap = new(new[]
    {
        (new GridField("name", "Name", GridFieldType.Text, true), (System.Linq.Expressions.Expression<Func<Row, object?>>)(x => x.Name)),
        (new GridField("city", "City", GridFieldType.Text, true), x => x.City),
        (new GridField("amount", "Amount", GridFieldType.Number, false), x => x.Amount),
        (new GridField("date", "Date", GridFieldType.Date, false), x => x.Date),
        (new GridField("active", "Active", GridFieldType.Boolean, false), x => x.Active),
        (new GridField("status", "Status", GridFieldType.Enum, false), x => x.Status)
    });

    /// <summary>Verifies each supported filter operation.</summary>
    [TestMethod]
    [DataRow(GridFilterOp.Eq, "10", null, 1)]
    [DataRow(GridFilterOp.Neq, "10", null, 3)]
    [DataRow(GridFilterOp.Gt, "10", null, 3)]
    [DataRow(GridFilterOp.Gte, "10", null, 4)]
    [DataRow(GridFilterOp.Lt, "20", null, 1)]
    [DataRow(GridFilterOp.Lte, "20", null, 3)]
    [DataRow(GridFilterOp.Between, "10", "20", 3)]
    [DataRow(GridFilterOp.In, "10,30", null, 2)]
    public void ApplyGridQuery_NumberOperations_ReturnExpectedRows(
        GridFilterOp operation, string value, string? value2, int expected)
    {
        var result = Seed().AsQueryable().ApplyGridQuery(
            new GridQuery { Filters = [new GridFilter("amount", operation, value, value2)] }, FieldMap);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(expected);
    }

    /// <summary>Verifies text filter operations.</summary>
    [TestMethod]
    [DataRow(GridFilterOp.Contains, "li", 3)]
    [DataRow(GridFilterOp.StartsWith, "A", 2)]
    public void ApplyGridQuery_TextOperations_ReturnExpectedRows(GridFilterOp operation, string value, int expected)
    {
        var result = Seed().AsQueryable().ApplyGridQuery(
            new GridQuery { Filters = [new GridFilter("name", operation, value)] }, FieldMap);

        result.Value.Should().HaveCount(expected);
    }

    /// <summary>Verifies boolean, date, and enum parsing.</summary>
    [TestMethod]
    public void ApplyGridQuery_TypedValues_AreParsed()
    {
        var result = Seed().AsQueryable().ApplyGridQuery(new GridQuery
        {
            Filters =
            [
                new GridFilter("active", GridFilterOp.Eq, "true"),
                new GridFilter("date", GridFilterOp.Gte, "2026-01-02"),
                new GridFilter("status", GridFilterOp.Eq, "Open")
            ]
        }, FieldMap);

        result.Value.Select(x => x.Name).Should().Equal("Charlie");
    }

    /// <summary>Verifies ordered multi-column sorting.</summary>
    [TestMethod]
    public void ApplyGridQuery_MultipleSorts_PreservePrecedence()
    {
        var result = Seed().AsQueryable().ApplyGridQuery(new GridQuery
        {
            Sort = [new GridSort("city"), new GridSort("amount", true)]
        }, FieldMap);

        result.Value.Select(x => x.Name).Should().Equal("Charlie", "Alice", "Alina", "Bob");
    }

    /// <summary>Verifies global search ORs searchable text fields.</summary>
    [TestMethod]
    public void ApplyGridQuery_GlobalSearch_UsesOrAcrossSearchableFields()
    {
        var result = Seed().AsQueryable().ApplyGridQuery(new GridQuery { Search = "Cairo" }, FieldMap);
        result.Value.Select(x => x.Name).Should().BeEquivalentTo("Alice", "Alina");
    }

    /// <summary>Verifies unknown fields fail without throwing.</summary>
    [TestMethod]
    public void ApplyGridQuery_UnknownField_ReturnsValidationFailure()
    {
        var result = Seed().AsQueryable().ApplyGridQuery(
            new GridQuery { Sort = [new GridSort("secret")] }, FieldMap);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("grid.unknown_field");
    }

    /// <summary>Verifies page-size and page-number clamping.</summary>
    [TestMethod]
    public async Task ToPagedResultAsync_OutOfRangePage_ClampsBounds()
    {
        await using var db = CreateDb();
        db.Rows.AddRange(Enumerable.Range(1, 250).Select(i => new Row { Id = i, Name = $"N{i}", City = "X" }));
        await db.SaveChangesAsync();

        var page = await db.Rows.OrderBy(x => x.Id).ToPagedResultAsync(
            new GridQuery { Page = 0, PageSize = 500 }, x => x.Id);

        page.Page.Should().Be(1);
        page.PageSize.Should().Be(200);
        page.TotalCount.Should().Be(250);
        page.Items.Should().HaveCount(200);
    }

    private static GridDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<GridDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new GridDbContext(options);
    }

    private static Row[] Seed() =>
    [
        new() { Id = 1, Name = "Alice", City = "Cairo", Amount = 20, Date = new DateOnly(2026, 1, 1), Active = true, Status = RowStatus.Open },
        new() { Id = 2, Name = "Bob", City = "Giza", Amount = 10, Date = new DateOnly(2026, 1, 2), Active = false, Status = RowStatus.Closed },
        new() { Id = 3, Name = "Charlie", City = "Alex", Amount = 30, Date = new DateOnly(2026, 1, 3), Active = true, Status = RowStatus.Open },
        new() { Id = 4, Name = "Alina", City = "Cairo", Amount = 20, Date = new DateOnly(2026, 1, 4), Active = true, Status = RowStatus.Closed }
    ];

    private sealed class GridDbContext(DbContextOptions<GridDbContext> options) : DbContext(options)
    {
        public DbSet<Row> Rows => Set<Row>();
    }

    private sealed class Row
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public DateOnly Date { get; init; }
        public bool Active { get; init; }
        public RowStatus Status { get; init; }
    }

    private enum RowStatus { Open, Closed }
}
