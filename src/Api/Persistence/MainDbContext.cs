using BuildingBlocks.Persistence;
using Cars;
using Clients;
using DashboardCharts;
using Expenses;
using Identity;
using Microsoft.EntityFrameworkCore;
using Revenues;
using Todos;
using Workers;

namespace Api.Persistence;

/// <summary>Single EF Core context for the complete application database.</summary>
public sealed class MainDbContext(DbContextOptions<MainDbContext> options)
    : DbContext(options), IMainDbContext
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ApplyModule(modelBuilder, typeof(IdentityModule).Assembly, "identity");
        ApplyModule(modelBuilder, typeof(ClientsModule).Assembly, "clients");
        ApplyModule(modelBuilder, typeof(ExpensesModule).Assembly, "expenses");
        ApplyModule(modelBuilder, typeof(TodosModule).Assembly, "todos");
        ApplyModule(modelBuilder, typeof(DashboardChartsModule).Assembly, "dashboard");
        ApplyModule(modelBuilder, typeof(RevenuesModule).Assembly, "revenues");
        ApplyModule(modelBuilder, typeof(CarsModule).Assembly, "cars");
        ApplyModule(modelBuilder, typeof(WorkersModule).Assembly, "workers");
    }

    private static void ApplyModule(ModelBuilder modelBuilder, System.Reflection.Assembly assembly, string schema)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(x => x.ClrType.Assembly == assembly))
            entityType.SetSchema(schema);
    }
}
