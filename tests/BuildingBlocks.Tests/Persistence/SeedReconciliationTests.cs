using System.Text;
using BuildingBlocks.Charts;
using DashboardCharts.Domain;
using DashboardCharts.Persistence.Seed;
using Expenses.Domain;
using Expenses.Persistence.Seed;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildingBlocks.Tests.Persistence;

/// <summary>Verifies repair of legacy seeded data.</summary>
[TestClass]
public sealed class SeedReconciliationTests
{
    /// <summary>Repairs chart titles and obsolete expense grouping fields without changing IDs.</summary>
    [TestMethod]
    public async Task DashboardSeeder_RepairsLegacyChart()
    {
        await using var db = MainDbTestFactory.Create();
        var chart = ChartDefinition.Create(
            Mojibake("المصروفات حسب الفئة"), ChartType.Bar, "expenses", "category", "amount",
            ChartAggregation.Sum, "[]", null, 1).Value;
        db.Set<ChartDefinition>().Add(chart);
        await db.SaveChangesAsync();

        await DashboardChartsSeeder.SeedAsync(db, default);

        var repaired = await db.Set<ChartDefinition>().SingleAsync(x => x.Id == chart.Id);
        repaired.Title.Should().Be("المصروفات حسب الفئة");
        repaired.XField.Should().Be("expenseTypeName");
        (await db.Set<ChartDefinition>().CountAsync(x => x.Title == "المصروفات حسب الفئة"))
            .Should().Be(1);
    }

    /// <summary>Merges duplicate types and repairs seeded expense Arabic text.</summary>
    [TestMethod]
    public async Task ExpensesSeeder_RepairsLegacyArabicAndDuplicates()
    {
        await using var db = MainDbTestFactory.Create();
        var canonical = ExpenseType.Create("مواد خام", ExpenseScope.General).Value;
        var legacy = ExpenseType.Create(Mojibake("مواد خام"), ExpenseScope.General).Value;
        var expense = Expense.Create(
            legacy.Id, 100, new DateOnly(2026, 1, 1),
            Mojibake("مستفيد 1"), Mojibake("مصروف تجريبي 1")).Value;
        db.Set<ExpenseType>().AddRange(canonical, legacy);
        db.Set<Expense>().Add(expense);
        await db.SaveChangesAsync();

        await ExpensesSeeder.SeedAsync(db, default);

        db.ChangeTracker.Clear();
        (await db.Set<ExpenseType>().CountAsync(x => x.Name == "مواد خام")).Should().Be(1);
        var repaired = await db.Set<Expense>().SingleAsync(x => x.Id == expense.Id);
        var remainingType = await db.Set<ExpenseType>().SingleAsync(x => x.Name == "مواد خام");
        repaired.ExpenseTypeId.Should().Be(remainingType.Id);
        repaired.Payee.Should().Be("مستفيد 1");
        repaired.Notes.Should().Be("مصروف تجريبي 1");
    }

    private static string Mojibake(string value) =>
        Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(value));
}
