using BuildingBlocks.Persistence;
using Clients.Persistence;
using Clients.Persistence.Seed;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildingBlocks.Tests.Persistence;

/// <summary>Verifies startup database initialization and first-run-only seeding.</summary>
[TestClass]
public sealed class DatabaseInitializationTests
{
    /// <summary>Seeds a newly created database but does not reseed on later starts.</summary>
    [TestMethod]
    public async Task InitializeAndSeed_RunsSeederOnlyOnFirstRun()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContext<ClientsDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        await using var provider = services.BuildServiceProvider();
        var state = new DatabaseInitializationState<ClientsDbContext>();
        var initializer = new ModuleDatabaseInitializer<ClientsDbContext>(provider, state,
            NullLogger<ModuleDatabaseInitializer<ClientsDbContext>>.Instance);
        var seeder = new ClientsSeedHostedService(provider, state,
            NullLogger<ClientsSeedHostedService>.Instance);

        await initializer.StartAsync(default);
        state.IsFirstRun.Should().BeTrue();
        await seeder.StartAsync(default);

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClientsDbContext>();
            (await db.Clients.CountAsync()).Should().BeGreaterThan(0);
            db.Clients.RemoveRange(db.Clients);
            await db.SaveChangesAsync();
        }

        await initializer.StartAsync(default);
        state.IsFirstRun.Should().BeFalse();
        await seeder.StartAsync(default);

        await using var verificationScope = provider.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ClientsDbContext>();
        (await verificationDb.Clients.CountAsync()).Should().Be(0);
    }
}
