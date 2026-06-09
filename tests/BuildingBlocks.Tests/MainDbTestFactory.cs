using Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Tests;

internal static class MainDbTestFactory
{
    public static MainDbContext Create(string? databaseName = null) => new(
        new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options);
}
