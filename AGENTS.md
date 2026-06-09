# AGENTS.md — operational guide for الفارس (Al-Faris)

Coding-agent guide for the Factory **الفارس** management system (.NET 10 modular monolith + Angular 22
RTL SPA). Read [`CLAUDE.md`](CLAUDE.md) for the architecture; this file is the operational checklist.

## Golden rules
1. **Module isolation.** A business module references **only** `Shared/BuildingBlocks`. **Never** add a
   `ProjectReference` between modules. Cross-module data flows only through the `IChartDataSource`
   DI registry (grid modules implement it; `DashboardCharts` consumes `IEnumerable<IChartDataSource>`).
2. **Grid engine is hand-rolled.** Do **not** add `System.Linq.Dynamic.Core`. Every sortable/filterable/
   searchable field must be in the module's `GridFieldMap` allow-list; unknown field ⇒
   `Error.Validation("grid.unknown_field")` ⇒ 400.
3. **Conventions.** XML docs on public types/methods; primary-ctor DI with null checks; async/await
   returning `Task<T>`; structured logging; strongly-typed config; `Result<T>` + `.ToHttpResult()`;
   enforce `.RequirePermission(...)` on every endpoint. Keep changes surgical.
4. **Enums cross the wire as numbers.** Backend enum order is the contract; keep `web/src/app/core`
   TS enums in sync (`GridFilterOp`, `ChartType`, `ChartAggregation`, statuses, priorities).
5. **Value objects** (`Email`, `Slug`) are EF value conversions — in LINQ compare the whole object
   (`u.Email == emailVo`), not `.Value`, so queries translate on Npgsql.

## Commands
```bash
dotnet build Factory.slnx                         # backend build (0 warnings expected)
dotnet test tests/BuildingBlocks.Tests/...         # backend tests (incl. boot e2e, self-skips w/o DB)
dotnet ef migrations add InitialCreate -p src/Modules/<X> -s src/Api -c <X>DbContext -o Persistence/Migrations
dotnet ef database update              -p src/Modules/<X> -s src/Api -c <X>DbContext
dotnet run --project src/Api                       # Scalar /scalar/v1 ; /health
cd web && npm install && npm run build && npm test # frontend build + vitest
```
Local Postgres not on 5432? Override per-shell:
`ConnectionStrings__Default=Host=localhost;Port=5433;Database=alfaris;Username=postgres;Password=postgres`
and `Seed__AdminPassword=<pwd>`. EF `database update` auto-creates the database.

## Definition of Done (per task)
- [ ] `dotnet build` clean, no new warnings; public APIs have XML docs.
- [ ] New behavior covered by a test where applicable; `dotnet test` green.
- [ ] No cross-module `ProjectReference`; modules reference only `Shared/BuildingBlocks`.
- [ ] Endpoints enforce the correct `.RequirePermission(...)`.
- [ ] For a new module: migration applied, seeder idempotent, grid + export + chart datasource wired.
- [ ] Frontend: `ng build` clean; TS enums match backend; RTL Arabic preserved.

## Adding a module (skeleton)
`Domain / Contracts / Features / Mapping / Persistence(own schema + IDesignTimeDbContextFactory) /
Endpoints / <Name>Module`. Add: a `<Name>Grid` (`GridFieldMap` + projection), a `Get<Name>GridQuery`,
`/grid` + `/export` endpoints, a `<Name>ChartDataSource : IChartDataSource`, an idempotent seeder +
hosted service. Register in `<Name>Module.Register`: `AddModuleDbContext`, repository,
`AddScoped<IChartDataSource, <Name>ChartDataSource>()`, `AddHostedService<<Name>SeedHostedService>()`.
Then add the `ProjectReference` + `AddModules(...)` entry in `src/Api`, create the migration, and apply it.

## Execution backlog
The ordered task list lives in
[`docs/ways-of-work/plan/factory-management/issues-checklist.md`](docs/ways-of-work/plan/factory-management/issues-checklist.md)
(T001→T101, with 🔶 review gates). `scripts/issue-map.json` maps task ids to GitHub issues. Execute
top-to-bottom; stop and request review at each 🔶 gate.
