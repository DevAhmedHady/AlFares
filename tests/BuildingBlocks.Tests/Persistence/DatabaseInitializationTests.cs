using Api.Persistence;
using Cars;
using Clients.Domain;
using DashboardCharts.Domain;
using Expenses.Domain;
using FluentAssertions;
using Identity.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Revenues.Domain;
using Todos.Domain;
using Workers;

namespace BuildingBlocks.Tests.Persistence;

/// <summary>Verifies the unified database model.</summary>
[TestClass]
public sealed class DatabaseInitializationTests
{
    /// <summary>All module entities are mapped by the main context.</summary>
    [TestMethod]
    public void MainContext_MapsEveryModule()
    {
        using MainDbContext db = MainDbTestFactory.Create();
        Type[] entities =
        [
            typeof(User),
            typeof(Client),
            typeof(Expense),
            typeof(TodoItem),
            typeof(ChartDefinition),
            typeof(Revenue),
            typeof(Car),
            typeof(Worker),
        ];

        entities.All(type => db.Model.FindEntityType(type) != null).Should().BeTrue();
    }
}
