using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Clients.Persistence;
using DashboardCharts.Persistence;
using Expenses.Persistence;
using FluentAssertions;
using Identity.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using Todos.Persistence;

namespace BuildingBlocks.Tests.Api;

/// <summary>
/// Boots the composed API against a real PostgreSQL instance and verifies migrations, seeding,
/// health, authentication, and the shared grid contract end-to-end. Skips (inconclusive) when no
/// database is reachable so the suite still runs in environments without PostgreSQL.
/// </summary>
[TestClass]
public sealed class BootAndSeedTests
{
    private const string AdminEmail = "admin@alfaris.local";
    private const string AdminPassword = "Admin#12345";

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ALFARIS_TEST_CONN")
        ?? "Host=localhost;Port=5433;Database=alfaris;Username=postgres;Password=postgres";

    [TestMethod]
    public async Task App_Boots_Seeds_AndServesGridContract()
    {
        if (!await DatabaseReachableAsync())
            Assert.Inconclusive($"PostgreSQL not reachable at '{ConnectionString}'; skipping boot e2e.");

        // Apply every module's migration up-front (idempotent) so the host's seeders have schema.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", ConnectionString);
        await MigrateAllAsync();

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(Environments.Production); // skip launchSettings URLs/env
                builder.UseSetting("ConnectionStrings:Default", ConnectionString);
                builder.UseSetting("Seed:AdminPassword", AdminPassword);
            });

        using var client = factory.CreateClient(); // starts host → runs idempotent seeders

        // Health is green.
        (await client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Seeded admin can authenticate against the seeded tenant.
        var tenantId = await SeededTenantIdAsync();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new { email = AdminEmail, password = AdminPassword, tenantId });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = (await loginResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("accessToken").GetString();
        token.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // The shared grid contract returns a PagedResult over the seeded clients.
        var grid = await client.PostAsJsonAsync("/api/clients/grid",
            new { page = 1, pageSize = 5, sort = new[] { new { field = "name", desc = false } } });
        grid.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await grid.Content.ReadFromJsonAsync<JsonElement>();
        paged.GetProperty("totalCount").GetInt64().Should().BeGreaterThan(0);
        paged.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);

        // Unknown field is rejected with a validation error, not an exception.
        var bad = await client.PostAsJsonAsync("/api/clients/grid",
            new { page = 1, pageSize = 5, filters = new[] { new { field = "bogus", op = 0, value = "x" } } });
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await bad.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("code").GetString().Should().Be("grid.unknown_field");
    }

    private static async Task<bool> DatabaseReachableAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (NpgsqlException)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static async Task MigrateAllAsync()
    {
        await using (var db = new IdentityDbContextFactory().CreateDbContext([])) await db.Database.MigrateAsync();
        await using (var db = new ClientsDbContextFactory().CreateDbContext([])) await db.Database.MigrateAsync();
        await using (var db = new ExpensesDbContextFactory().CreateDbContext([])) await db.Database.MigrateAsync();
        await using (var db = new TodosDbContextFactory().CreateDbContext([])) await db.Database.MigrateAsync();
        await using (var db = new DashboardChartsDbContextFactory().CreateDbContext([])) await db.Database.MigrateAsync();
    }

    private static async Task<Guid> SeededTenantIdAsync()
    {
        await using var db = new IdentityDbContextFactory().CreateDbContext([]);
        var tenant = await db.Tenants.FirstAsync();
        return tenant.Id;
    }
}
