using BuildingBlocks.Charts;
using BuildingBlocks.Export;
using BuildingBlocks.Grids;
using Expenses.Charts;
using Expenses.Domain;
using Expenses.Features;
using Expenses.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildingBlocks.Tests.Expenses;

[TestClass]
public sealed class ExpensesVerticalTests
{
    [TestMethod]
    public async Task GridExportChart_Works()
    {
        await using var db = CreateDb();
        var materials = ExpenseType.Create("مواد", ExpenseScope.General).Value;
        var transport = ExpenseType.Create("نقل", ExpenseScope.General).Value;
        db.Set<ExpenseType>().AddRange(materials, transport);
        db.Set<Expense>()
            .Add(Expense.Create(materials.Id, 100, new DateOnly(2026, 1, 5), "أ", null).Value);
        db.Set<Expense>()
            .Add(Expense.Create(materials.Id, 250, new DateOnly(2026, 1, 20), "ب", null).Value);
        db.Set<Expense>()
            .Add(Expense.Create(transport.Id, 50, new DateOnly(2026, 2, 1), "ج", null).Value);
        await db.SaveChangesAsync();
        var grid = await new GetExpensesGridHandler(db).Handle(
            new(
                new GridQuery
                {
                    Filters = [new("expenseTypeName", GridFilterOp.Eq, "مواد")],
                    Sort = [new("amount", true)],
                }
            ),
            default
        );
        grid.Value.Items.Select(x => x.Amount).Should().Equal(250, 100);
        grid.Value.Aggregates.Should().ContainKey("amount").WhoseValue.Should().Be(350);
        new ExcelGridExporter()
            .Export(
                grid.Value.Items,
                [new("ExpenseTypeName", "الفئة", GridFieldType.Text)],
                "المصروفات"
            )
            .Should()
            .NotBeEmpty();
        var chart = await new ExpensesChartDataSource(db).ComputeAsync(
            new("date", "amount", ChartAggregation.Sum, []),
            default
        );
        chart.Points.Should().Contain(x => x.Label == "2026-01" && x.Value == 350);
    }

    [TestMethod]
    public async Task BulkDelete_DeletesMatchingIds_AndRejectsEmpty()
    {
        await using var db = CreateDb();
        var type = ExpenseType.Create("مواد", ExpenseScope.General).Value;
        db.Set<ExpenseType>().Add(type);
        var keep = Expense.Create(type.Id, 100, new DateOnly(2026, 1, 5), "أ", null).Value;
        var removeA = Expense.Create(type.Id, 200, new DateOnly(2026, 1, 6), "ب", null).Value;
        var removeB = Expense.Create(type.Id, 300, new DateOnly(2026, 1, 7), "ج", null).Value;
        db.Set<Expense>().AddRange(keep, removeA, removeB);
        await db.SaveChangesAsync();

        var empty = await new BulkDeleteExpensesHandler(db).Handle(
            new BulkDeleteExpensesCommand([]),
            default
        );
        empty.IsFailure.Should().BeTrue();
        empty.Error.Code.Should().Be("expenses.bulk_ids_required");

        var result = await new BulkDeleteExpensesHandler(db).Handle(
            new BulkDeleteExpensesCommand([removeA.Id, removeB.Id, Guid.NewGuid()]),
            default
        );
        result.IsSuccess.Should().BeTrue();
        result.Value.Deleted.Should().Be(2);
        (await db.Set<Expense>().CountAsync()).Should().Be(1);
        (await db.Set<Expense>().SingleAsync()).Id.Should().Be(keep.Id);
    }

    private static global::Api.Persistence.MainDbContext CreateDb() => MainDbTestFactory.Create();
}
