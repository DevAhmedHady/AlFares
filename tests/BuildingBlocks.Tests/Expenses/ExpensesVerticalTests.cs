using BuildingBlocks.Charts;using BuildingBlocks.Export;using BuildingBlocks.Grids;using Expenses.Charts;using Expenses.Domain;using Expenses.Features;using Expenses.Persistence;using FluentAssertions;using Microsoft.EntityFrameworkCore;using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace BuildingBlocks.Tests.Expenses;
[TestClass]public sealed class ExpensesVerticalTests
{
 [TestMethod]public async Task GridExportChart_Works()
 {
  await using var db=CreateDb();
  var materials=ExpenseType.Create("مواد",ExpenseScope.General).Value;
  var transport=ExpenseType.Create("نقل",ExpenseScope.General).Value;
  db.Set<ExpenseType>().AddRange(materials,transport);
  db.Set<Expense>().Add(Expense.Create(materials.Id,100,new DateOnly(2026,1,5),"أ",null).Value);
  db.Set<Expense>().Add(Expense.Create(materials.Id,250,new DateOnly(2026,1,20),"ب",null).Value);
  db.Set<Expense>().Add(Expense.Create(transport.Id,50,new DateOnly(2026,2,1),"ج",null).Value);
  await db.SaveChangesAsync();
  var grid=await new GetExpensesGridHandler(db).Handle(new(new GridQuery{Filters=[new("expenseTypeName",GridFilterOp.Eq,"مواد")],Sort=[new("amount",true)]}),default);
  grid.Value.Items.Select(x=>x.Amount).Should().Equal(250,100);
  new ExcelGridExporter().Export(grid.Value.Items,[new("ExpenseTypeName","الفئة",GridFieldType.Text)],"المصروفات").Should().NotBeEmpty();
  var chart=await new ExpensesChartDataSource(db).ComputeAsync(new("date","amount",ChartAggregation.Sum,[]),default);
  chart.Points.Should().Contain(x=>x.Label=="2026-01"&&x.Value==350);
 }
 private static global::Api.Persistence.MainDbContext CreateDb()=>MainDbTestFactory.Create();
}
