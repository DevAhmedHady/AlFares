using Microsoft.EntityFrameworkCore;

namespace Api.Persistence;

/// <summary>Applies the single main database migration set before module seeders run.</summary>
public sealed class MainDatabaseInitializer(IServiceProvider services, ILogger<MainDatabaseInitializer> logger)
    : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        await MigrateAsync(db, cancellationToken);
        logger.LogInformation("Main database migrations applied.");
    }

    /// <summary>Baselines a legacy per-module schema and applies the central migration set.</summary>
    /// <remarks>
    /// The single <c>InitialMainDatabase</c> migration describes the whole schema. A database created
    /// by the former per-module contexts already holds the legacy module tables, so re-running the
    /// migration would fail with "relation already exists". We therefore mark the migration as applied
    /// whenever the legacy <c>identity.users</c> table is present, then create any module tables added
    /// after that baseline (revenues, cars, workers) idempotently. A fresh database has no legacy table,
    /// so the baseline is skipped and <see cref="RelationalDatabaseFacadeExtensions.MigrateAsync"/>
    /// creates the complete schema.
    /// </remarks>
    public static async Task MigrateAsync(
        MainDbContext db,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            );

            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT '20260609140539_InitialMainDatabase', '10.0.4'
            WHERE to_regclass('identity.users') IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1 FROM "__EFMigrationsHistory"
                  WHERE "MigrationId" = '20260609140539_InitialMainDatabase'
              );
            """,
            cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync(NewModuleSchemaSql, cancellationToken);
    }

    /// <summary>
    /// Idempotently provisions the Revenues, Cars, and Workers tables for databases baselined before
    /// those modules existed. Every statement is <c>IF NOT EXISTS</c>, so it is a no-op on an
    /// already-current schema.
    /// </summary>
    private const string NewModuleSchemaSql =
        """
        CREATE SCHEMA IF NOT EXISTS revenues;
        CREATE SCHEMA IF NOT EXISTS cars;
        CREATE SCHEMA IF NOT EXISTS workers;

        CREATE TABLE IF NOT EXISTS revenues.revenue_types (
            "Id" uuid NOT NULL,
            "Name" character varying(100) NOT NULL,
            "IsActive" boolean NOT NULL,
            CONSTRAINT "PK_revenue_types" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS revenues.revenues (
            "Id" uuid NOT NULL,
            "RevenueTypeId" uuid NOT NULL,
            "Amount" numeric(18,2) NOT NULL,
            "Date" date NOT NULL,
            "Source" character varying(200) NOT NULL,
            "Notes" character varying(2000),
            "OwnerType" integer NOT NULL,
            "OwnerId" uuid,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "UpdatedAtUtc" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_revenues" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_revenues_revenue_types_RevenueTypeId" FOREIGN KEY ("RevenueTypeId")
                REFERENCES revenues.revenue_types ("Id") ON DELETE RESTRICT
        );

        CREATE TABLE IF NOT EXISTS cars.cars (
            "Id" uuid NOT NULL,
            "Name" character varying(150) NOT NULL,
            "PlateNumber" character varying(50),
            "DriverName" character varying(150),
            "Type" integer NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "UpdatedAtUtc" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_cars" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS workers.workers (
            "Id" uuid NOT NULL,
            "Name" character varying(150) NOT NULL,
            "JobTitle" character varying(150),
            "IsActive" boolean NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "UpdatedAtUtc" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_workers" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS workers.advances (
            "Id" uuid NOT NULL,
            "WorkerId" uuid NOT NULL,
            "Amount" numeric(18,2) NOT NULL,
            "Date" date NOT NULL,
            "Notes" text,
            "LinkedExpenseId" uuid NOT NULL,
            CONSTRAINT "PK_advances" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS workers.settlements (
            "Id" uuid NOT NULL,
            "WorkerId" uuid NOT NULL,
            "Amount" numeric(18,2) NOT NULL,
            "Date" date NOT NULL,
            "Notes" text,
            CONSTRAINT "PK_settlements" PRIMARY KEY ("Id")
        );

        CREATE INDEX IF NOT EXISTS "IX_advances_WorkerId" ON workers.advances ("WorkerId");
        CREATE INDEX IF NOT EXISTS "IX_settlements_WorkerId" ON workers.settlements ("WorkerId");
        CREATE INDEX IF NOT EXISTS "IX_cars_Name" ON cars.cars ("Name");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_revenue_types_Name" ON revenues.revenue_types ("Name");
        CREATE INDEX IF NOT EXISTS "IX_revenues_Date" ON revenues.revenues ("Date");
        CREATE INDEX IF NOT EXISTS "IX_revenues_OwnerType_OwnerId" ON revenues.revenues ("OwnerType", "OwnerId");
        CREATE INDEX IF NOT EXISTS "IX_revenues_RevenueTypeId" ON revenues.revenues ("RevenueTypeId");
        """;

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
