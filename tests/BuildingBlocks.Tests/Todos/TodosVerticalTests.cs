using BuildingBlocks.Charts;
using BuildingBlocks.Grids;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Todos.Charts;
using Todos.Domain;
using Todos.Features;

namespace BuildingBlocks.Tests.Todos;

/// <summary>Todo tests.</summary>
[TestClass]
public sealed class TodosVerticalTests
{
    /// <summary>Past date fails.</summary>
    [TestMethod]
    public void PastDueDate_Fails()
    {
        var today = new DateOnly(2026, 6, 9);
        TodoItem
            .Create("x", today.AddDays(-1), TodoPriority.Normal, null, today)
            .IsFailure.Should()
            .BeTrue();
    }

    /// <summary>Grid and chart work.</summary>
    [TestMethod]
    public async Task GridChart_Work()
    {
        await using var db = MainDbTestFactory.Create();
        var today = new DateOnly(2026, 6, 9);
        var first = TodoItem.Create("مهم", today.AddDays(1), TodoPriority.High, null, today).Value;
        var second = TodoItem
            .Create("عاجل", today.AddDays(2), TodoPriority.High, null, today)
            .Value;
        second.ChangeStatus(TodoStatus.InProgress);
        db.Set<TodoItem>().AddRange(first, second);
        await db.SaveChangesAsync();

        var grid = await new GetTodosGridHandler(db).Handle(
            new(new GridQuery { Filters = [new("priority", GridFilterOp.Eq, "High")] }),
            default
        );
        grid.Value.Items.Should().HaveCount(2);

        var chart = await new TodosChartDataSource(db).ComputeAsync(
            new("priority", null, ChartAggregation.Count, []),
            default
        );
        chart.Points.Should().Contain(x => x.Label == "High" && x.Value == 2);
    }
}
