using BuildingBlocks.Charts;
using BuildingBlocks.Endpoints;
using BuildingBlocks.Ledger;
using BuildingBlocks.Modules;
using BuildingBlocks.Persistence;
using Expenses.Charts;
using Expenses.Domain;
using Expenses.Ledger;
using Expenses.Persistence;
using Expenses.Persistence.Seed;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Expenses;

/// <summary>Expenses module.</summary>
public sealed class ExpensesModule : IModule
{
    /// <inheritdoc />
    public string Name => "Expenses";

    /// <inheritdoc />
    public void Register(IServiceCollection services, IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IChartDataSource, ExpensesChartDataSource>();
        services.AddScoped<ILedgerSource, ExpensesLedgerSource>();
        services.AddScoped<ILedgerWriter, ExpensesLedgerWriter>();
        services.AddHostedService<ExpensesSeedHostedService>();
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapEndpointsFromAssembly(typeof(ExpensesModule).Assembly);
}
