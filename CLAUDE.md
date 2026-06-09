# الفارس (Al-Faris) — Factory Management System

High-level management for factory **الفارس**: clients/accounts, expenses, time-restricted to-dos, a
configurable charts dashboard, and user management. Single-tenant, **Arabic-first RTL**.

## Stack
- **Backend:** .NET 10 modular monolith — Minimal APIs, EF Core 10 + Npgsql/PostgreSQL, Mapster,
  FluentValidation, JWT RBAC. `Result<T>` + `.ToHttpResult()`. Per-module schema + idempotent seeders.
- **Frontend:** Angular 22 (standalone, signals, zoneless) SPA in `web/`, RTL Arabic, ApexCharts.
- **Auth:** JWT access + refresh; permissions are claims; `.RequirePermission(code)` on endpoints.

## Layout
```
src/
  Shared/SharedKernel/        Entity, AggregateRoot, ValueObject, Result, Error (no framework deps)
  Shared/BuildingBlocks/      Grids/ Charts/ Export/ + messaging, auth, modules, mapping infra
  Modules/Identity/           tenants, users, roles, permissions, JWT, seeding
  Modules/Clients/            schema `clients`
  Modules/Expenses/           schema `expenses`
  Modules/Todos/              schema `todos`   (DueDate ≥ today invariant)
  Modules/DashboardCharts/    schema `dashboard` (consumes IChartDataSource registry)
  Api/                        host: AddModules(...) + migrations + CORS + RTL font
web/                          Angular RTL SPA
tests/BuildingBlocks.Tests/   xUnit/MSTest — module verticals + boot e2e
```

## Module isolation (hard rule)
A module references **only** `Shared/BuildingBlocks` — **never** another module. Cross-module data flows
through the `IChartDataSource` DI registry only: grid modules *implement* `IChartDataSource`;
`DashboardCharts` *consumes* `IEnumerable<IChartDataSource>` and aggregates without referencing them.

## The three pillars in BuildingBlocks
- **Grids** (`Grids/`): `GridQuery` (page/sort/filter/search) → `ApplyGridQuery` (hand-rolled expression
  building gated by a `GridFieldMap` allow-list — **no System.Linq.Dynamic.Core**) → `ToPagedResultAsync`.
  Unknown field ⇒ `Error.Validation("grid.unknown_field")` ⇒ HTTP 400.
- **Charts** (`Charts/`): `IChartDataSource` + `ChartComputeRequest`/`ChartSeries` contracts.
- **Export** (`Export/`): `IGridExporter` for Xlsx (ClosedXML) + PDF (QuestPDF, RTL, embedded Cairo font),
  keyed by `ExportFormat`. Exports the full filtered set (paging stripped), clamped to ~50k rows.

## Add a grid + chart to a module
1. `<Name>Grid` static: a `GridFieldMap<T>` (Arabic `DisplayName`, `Chartable` flags) + a `Projection`.
2. `Get<Name>GridQuery` + handler: `db.Set.AsNoTracking().ApplyGridQuery(q, Map).ToPagedResultAsync(...)`.
3. Endpoints: `POST /api/<x>/grid` (read), `POST /api/<x>/export` (export). `.RequirePermission(...)`.
4. `<Name>ChartDataSource : IChartDataSource` — `Describe()` lists chartable fields; `ComputeAsync` groups
   by X, aggregates Y. Register `AddScoped<IChartDataSource, <Name>ChartDataSource>()` in `<Name>Module`.

See [`docs/adding-a-module.md`](docs/adding-a-module.md) for the full module skeleton.

## Permissions
Catalog seeded by `IdentitySeeder`: `clients.*`, `expenses.*`, `todos.*` (`read/write/delete/export`),
`dashboard.charts.read|manage`, `identity.users.read|manage`, `identity.tenants.manage`.
Roles: **Owner**=all · **Admin**=all except `identity.tenants.manage` · **Member**=`*.read` only.

## Build / run / migrate
```bash
dotnet build Factory.slnx
dotnet test tests/BuildingBlocks.Tests/BuildingBlocks.Tests.csproj
# Postgres on :5432 (alfaris db). Override with env: ConnectionStrings__Default + Seed__AdminPassword
dotnet ef migrations add InitialCreate -p src/Modules/<X> -s src/Api -c <X>DbContext -o Persistence/Migrations
dotnet ef database update           -p src/Modules/<X> -s src/Api -c <X>DbContext
dotnet run --project src/Api        # Scalar UI at /scalar/v1 ; health at /health
```
Seeders create tenant الفارس, an admin (`Seed:AdminEmail` / `Seed:AdminPassword`), demo
clients/expenses/todos, and 4 default dashboard charts (all idempotent).

## Frontend (`web/`)
```bash
cd web && npm install
npm start          # http://localhost:4200 (API expected at http://localhost:5113, CORS-allowed)
npm run build      # production bundle
npm test           # vitest unit specs
```
- Login auto-resolves the tenant via anonymous `GET /api/tenants/default`.
- One reusable server-side **grid component** (`shared/grid`) drives Clients/Expenses/Todos/Users:
  sort, global + per-column filter, column drag-reorder, show/hide, Excel/PDF export.
- **Dashboard** renders saved charts (ApexCharts); the **chart builder** dialog previews live via
  `POST /api/dashboard/charts/preview` and persists definitions.
- RTL/Arabic: `index.html` `dir="rtl" lang="ar"`, Cairo font, Arabic UI strings + server-provided
  Arabic field display names. Enums cross the wire as **numbers** — keep the TS enums in sync.
- **Note:** uses a custom grid + ApexCharts core (not AG Grid / ng-apexcharts) — those wrappers lack
  Angular 22 peer support; the same behaviors are delivered.

## Conventions
XML docs on public APIs · primary-ctor DI with null checks · async `Task<T>` · structured logging ·
strongly-typed config · enforce the correct permission · keep changes surgical.
