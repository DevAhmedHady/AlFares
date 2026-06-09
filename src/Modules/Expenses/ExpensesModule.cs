using BuildingBlocks.Endpoints;
using BuildingBlocks.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Persistence;
using Expenses.Domain;
using Expenses.Persistence;
using BuildingBlocks.Charts;
using Expenses.Charts;
using Expenses.Persistence.Seed;
namespace Expenses;
/** <summary>Expenses module.</summary> */
public sealed class ExpensesModule:IModule
{
    /** <inheritdoc/> */
    public string Name=>"Expenses";
    /** <inheritdoc/> */
    public void Register(IServiceCollection services,IConfiguration config){ArgumentNullException.ThrowIfNull(services);ArgumentNullException.ThrowIfNull(config);services.AddModuleDbContext<ExpensesDbContext>(config,Name,ExpensesDbContext.Schema);services.AddScoped<IExpenseRepository,ExpenseRepository>();services.AddScoped<IChartDataSource,ExpensesChartDataSource>();services.AddHostedService<ExpensesSeedHostedService>();}
    /** <inheritdoc/> */
    public void MapEndpoints(IEndpointRouteBuilder endpoints)=>endpoints.MapEndpointsFromAssembly(typeof(ExpensesModule).Assembly);
}

