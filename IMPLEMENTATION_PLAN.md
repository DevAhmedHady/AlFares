# Factory "الفارس" — High-Level Management System — Implementation Plan

## Context
Build a new solution for factory **الفارس** (Al-Faris, "the knight") for high-level management:
clients/accounts, expenses, a configurable dashboard, time-restricted to-dos, and user management.
Base = `C:\Users\AhmedHady\source\repos\knights-templates` — a **backend-only** .NET 10 modular-monolith
(Minimal APIs, EF Core 10 + Npgsql/PostgreSQL, Mapster, FluentValidation, JWT RBAC, per-module schema +
seeders, `Result<T>` + `.ToHttpResult()`, modules reference **only** `Shared/BuildingBlocks`, never each other).

The template has **no UI, grids, charts, or export** — all of that is new. Decisions locked with the user:
- **Frontend:** Angular + TypeScript (separate SPA consuming the API).
- **Tenancy:** single-tenant (seed one tenant الفارس; hide tenant switching).
- **Charts:** metadata-registry datasources (grids expose chartable fields; dashboard aggregates server-side).
- **Localization:** Arabic-first **RTL**, English secondary.

Execution: this plan is handed to **Codex-CLI** for implementation; user reviews after. Also produce
`CLAUDE.md` and a Codex agent file (`AGENTS.md`).

---

## High-level shape
```
Factory/                                  (copy of knights-templates, Factory.slnx)
  src/
    Shared/SharedKernel/                   unchanged
    Shared/BuildingBlocks/                 + Grids/  + Charts/  + Export/   (new cross-cutting infra)
    Modules/Identity/                      extend: permissions, roles, single-tenant + admin seed
    Modules/Clients/                       new
    Modules/Expenses/                      new
    Modules/Todos/                         new
    Modules/DashboardCharts/               new
    Api/                                   register 4 modules + RTL font
    (Catalog/Ordering — remove, sample-only)
  web/                                     new Angular SPA (AG Grid + ng-apexcharts, RTL)
  CLAUDE.md  AGENTS.md
```

---

## Phase 0 — Scaffold the solution
1. Copy `knights-templates` → `C:\Users\AhmedHady\source\repos\Factory`. Rename `knights.slnx`→`Factory.slnx`.
2. Remove `Catalog` and `Ordering` modules (sample-only): delete projects, their `ProjectReference` in
   `src/Api/Api.csproj`, and their `typeof(...).Assembly` lines in `src/Api/Program.cs`.
3. `appsettings.json`: connection `Database=alfaris`; set `Jwt:Issuer/Audience` = `al-faris`; add
   `Seed:AdminEmail` (`admin@alfaris.local`), `Seed:AdminPassword`, `Seed:TenantName` (الفارس), `Seed:TenantSlug` (`al-faris`).

---

## Phase 1 — BuildingBlocks: shared grid/chart/export infra (the linchpin — do first)

### 1a. Grids — `src/Shared/BuildingBlocks/Grids/`
- `GridQuery.cs`: `record GridQuery { int Page=1; int PageSize=25; string? Search; IReadOnlyList<GridSort> Sort; IReadOnlyList<GridFilter> Filters; }`
  - `record GridSort(string Field, bool Desc)`
  - `enum GridFilterOp { Eq, Neq, Contains, StartsWith, Gt, Gte, Lt, Lte, Between, In }`
  - `record GridFilter(string Field, GridFilterOp Op, string? Value, string? Value2=null)`
- `PagedResult.cs`: `record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount){ TotalPages... }`
- `GridField.cs`: `enum GridFieldType { Text, Number, Date, Boolean, Enum }`,
  `record GridField(string Key, string DisplayName, GridFieldType Type, bool Searchable, bool Sortable=true, bool Filterable=true, bool Chartable=false)`,
  `class GridFieldMap<T>` holding `IReadOnlyList<GridField> Fields` + `Expression<Func<T,object?>> Selector(string key)`.
- `GridQueryExtensions.cs`:
  - `Result<IQueryable<T>> ApplyGridQuery<T>(this IQueryable<T>, GridQuery, GridFieldMap<T>)` — **hand-rolled
    expression building** (NO System.Linq.Dynamic.Core): validate every field key against the map (unknown ⇒
    `Error.Validation("grid.unknown_field", key)`), fold `Sort` into `OrderBy/ThenBy`, AND per-column filters
    (build `BinaryExpression`/`string.Contains` per op, parse `Value` to CLR type from `GridFieldType`), OR
    global `Search` across `Searchable` text fields.
  - `Task<PagedResult<TOut>> ToPagedResultAsync<T,TOut>(this IQueryable<T> filtered, GridQuery, Expression<Func<T,TOut>> projection, CancellationToken)` — COUNT then Skip/Take. Clamp PageSize to [1..200].
- **Field allow-list is the single source of truth** per grid (drives sort + filter + search + chart fields + export headers).

### 1b. Charts — `src/Shared/BuildingBlocks/Charts/`
- `ChartContracts.cs`: `enum ChartAggregation { Count, Sum, Avg, Min, Max }`,
  `record ChartPoint(string Label, decimal Value)`, `record ChartSeries(string Name, IReadOnlyList<ChartPoint> Points)`,
  `record ChartFieldDescriptor(string Key, string DisplayName, GridFieldType Type, bool CanGroupBy, bool CanAggregate)`,
  `record ChartDataSourceMetadata(string Key, string DisplayName, IReadOnlyList<ChartFieldDescriptor> Fields)`,
  `record ChartComputeRequest(string XField, string? YField, ChartAggregation Aggregation, IReadOnlyList<GridFilter> Filters)`.
- `IChartDataSource.cs`: `interface IChartDataSource { string Key; string DisplayName; ChartDataSourceMetadata Describe(); Task<ChartSeries> ComputeAsync(ChartComputeRequest, CancellationToken); }`
- **Inversion:** grid modules *implement* `IChartDataSource`; DashboardCharts *consumes* `IEnumerable<IChartDataSource>` via DI — so Dashboard aggregates other modules' data **without referencing them**.

### 1c. Export — `src/Shared/BuildingBlocks/Export/`
- Packages → `Directory.Packages.props`: `ClosedXML` (xlsx), `QuestPDF` (PDF, RTL/Arabic). Reference from BuildingBlocks.
- `enum ExportFormat { Xlsx, Pdf }`, `record ExportColumn(string Key, string Header, GridFieldType Type)`,
  `interface IGridExporter { ExportFormat Format; byte[] Export<T>(IReadOnlyList<T> rows, IReadOnlyList<ExportColumn> columns, string title); }`
- `ExcelGridExporter` (ClosedXML) + `PdfGridExporter` (QuestPDF, document RTL, embed Arabic font Cairo/Amiri).
  Register both (keyed DI by format). Clamp export to ~50k rows.

---

## Phase 2 — Business modules (Clients, Expenses, Todos)
Each is a standard vertical-slice module (copy old `Catalog` layout: Domain / Contracts / Features / Mapping /
Persistence(own schema + `IDesignTimeDbContextFactory`) / Endpoints / `<Name>Module`). Each adds: a `*Grid`
static class (the `GridFieldMap` + projection), a `Get<X>Grid` query (`POST /api/<x>/grid`), an export endpoint
(`POST /api/<x>/export`, paging stripped, full filtered set), a `*ChartDataSource : IChartDataSource`, a seeder +
hosted service. Reuse `Money`/`Email`-style value objects from the template.

| Module | Schema | Aggregate (key fields) | Features | Permissions |
|---|---|---|---|---|
| **Clients** | `clients` | `Client`(Name, Contact{Name,Phone,Email}, AccountBalance, ActivityLevel{Low,Med,High}, Status{Active,Inactive}, Notes, timestamps) | Create/Update/SetStatus/Delete/GetById/**GetGrid** | `clients.read/write/delete/export` |
| **Expenses** | `expenses` | `Expense`(Category, Amount, Date(DateOnly), Payee, Notes, timestamps) | Create/Update/Delete/GetById/**GetGrid** | `expenses.read/write/delete/export` |
| **Todos** | `todos` | `TodoItem`(Title, DueDate, Status{Open,InProgress,Done}, Priority{Low,Normal,High,Urgent}, Notes, timestamps); factory enforces DueDate ≥ today (time-restricted) | Create/Update/ChangeStatus/Delete/GetById/**GetGrid** | `todos.read/write/delete/export` |

Reference impl for charts (copy template): `Modules/Clients/Charts/ClientsChartDataSource.cs` — e.g. x=`status`,
Count → `GroupBy(c=>c.Status).Select(g=>new{g.Key, Count})`; Sum/Avg switch over allow-listed numeric Y selector.
Expenses datasource supports **month-bucketing** for the Line chart.

Register in each `*Module.Register`: `AddModuleDbContext`, repository, `AddScoped<IChartDataSource, *ChartDataSource>()`,
`AddHostedService<*SeedHostedService>()`.

---

## Phase 3 — DashboardCharts module — schema `dashboard`
- `Domain/ChartDefinition.cs` (AggregateRoot): Title(AR), Type{Bar,Pie,Line}, DatasourceKey, XField, YField?,
  Aggregation, ColorsJson(jsonb), FiltersJson?(jsonb optional scope), LayoutOrder, IsEnabled, timestamps; `Create/Update → Result<>`.
- `ChartDataSourceRegistry(IEnumerable<IChartDataSource> sources)`: `All()` → metadata list; `Get(key)`.
- Endpoints (`/api/dashboard/...`):
  - `GET /datasources` → `ChartDataSourceMetadata[]` (drives the "define chart" form) — `dashboard.charts.manage`
  - `GET /charts`, `POST /charts`, `PUT /charts/{id}`, `DELETE /charts/{id}`
  - `GET /charts/{id}/data` → resolve `registry.Get(def.DatasourceKey)` → `ComputeAsync` → `ChartSeries` (`dashboard.charts.read`; missing key ⇒ `Error.NotFound`)
  - `POST /charts/preview` → compute unsaved definition (editor live preview)
- Permissions: `dashboard.charts.read`, `dashboard.charts.manage`.
- Default palette `DashboardPalette.cs`: `["#2563EB","#16A34A","#DC2626","#D97706","#7C3AED","#0891B2","#DB2777","#65A30D"]`.
- Seeded charts (idempotent by Title): Pie clients by status (Count); Bar expenses by category (Sum amount);
  Bar todos by priority (Count); Line expenses over time (Sum amount, month-bucketed).

---

## Phase 4 — Identity extension (single-tenant + admin + user management)
- Extend `Identity/Persistence/Seed/IdentitySeeder.cs`: add all new permission codes; roles **Owner**=all,
  **Admin**=all except `identity.tenants.manage`, **Member**=`*.read`.
- New seed step `IdentityTenantSeeder` (invoked from existing `IdentitySeedHostedService` after perms/roles):
  idempotent by slug `al-faris` — create tenant الفارس + admin `User` (from `Seed:*` config) + Owner membership.
- User management: reuse existing admin/tenant endpoints; add `GET /api/admin/users/grid` (server-side grid over
  `IdentityDbContext.Users` using the same `GridQuery` infra) for the Angular Users page.

---

## Phase 5 — `src/Api` wiring
- `Program.cs`: `AddModules(... typeof(IdentityModule).Assembly, ClientsModule, ExpensesModule, TodosModule, DashboardChartsModule)`.
- Register QuestPDF Arabic font once at startup. Add permissive **CORS** for the Angular dev origin.
- Migrations (per module): `dotnet ef migrations add InitialCreate -p Modules/<X> -s Api -c <X>DbContext -o Persistence/Migrations`
  then `dotnet ef database update -p Modules/<X> -s Api -c <X>DbContext` for Identity, Clients, Expenses, Todos, DashboardCharts.

---

## Phase 6 — Angular SPA — `web/` (Arabic-first RTL)
- Angular + TS, `dir="rtl"`, Arabic locale default + EN secondary (i18n; `@angular/localize` or ngx-translate).
- **Grids:** AG Grid Angular — server-side row model posting `GridQuery` to `/api/<x>/grid`; enables sort, search
  (global + per-column filter), **column reorder via drag & drop**, column show/hide. Toolbar **Export PDF / Excel**
  buttons POST current `GridQuery` to `/api/<x>/export` and download the returned file (full filtered set).
- **Charts:** `ng-apexcharts` (ApexCharts) — bar/pie/line. Dashboard page lists saved charts (`GET /charts` +
  per-chart `GET /charts/{id}/data`). **Chart builder** dialog (admin): pick type → datasource (`GET /datasources`)
  → X field / Y field + aggregation → color scheme (default palette + custom) → live preview (`POST /charts/preview`)
  → save. Drag to reorder (LayoutOrder).
- **Auth:** login (single tenant auto-selected), JWT stored, route guards by permission, refresh-token flow.
- **Pages:** Login, Dashboard, Clients, Expenses, Todos, Users — all grids share one reusable grid component.
- API client services generated/typed from the OpenAPI spec (`/openapi`).

---

## Phase 7 — Docs / agent files
- `CLAUDE.md` (repo root): stack, module conventions (link old `docs/adding-a-module.md`), how to add a grid +
  chart datasource, build/run/migrate commands, Angular dev commands, RTL/Arabic notes.
- `AGENTS.md` (Codex agent file): same operational guidance phrased for Codex-CLI — build/test/migrate/run commands,
  conventions to follow, "never reference another module — use the `IChartDataSource` registry", definition-of-done
  per module (migration applied, seeder idempotent, grid+export+chart wired, permissions enforced).

---

## Build / dependency order
SharedKernel → **BuildingBlocks (Grids+Charts+Export first)** → Identity (seed) → Clients/Expenses/Todos (parallel) →
DashboardCharts → Api → Angular `web/`.

## Verification (end-to-end)
1. `dotnet build` clean; run all `dotnet ef database update` per module → schemas `identity/clients/expenses/todos/dashboard` created.
2. `dotnet run --project src/Api` → seeders log; Postgres has tenant الفارس, admin user, sample clients/expenses/todos, 4 default charts.
3. Scalar UI `/scalar/v1`: login as admin → token. Hit `POST /api/clients/grid` with sort+search+filter → correct `PagedResult`. Unknown field → 400 `grid.unknown_field`.
4. `POST /api/clients/export {format:"xlsx", grid:{...filters}}` and `pdf` → files download, contain full filtered set, Arabic headers render RTL.
5. `GET /api/dashboard/datasources` lists clients/expenses/todos fields; `POST /charts/preview {type:"pie", datasourceKey:"clients", xField:"status", aggregation:"Count"}` → series; save → appears in `GET /charts`; `GET /charts/{id}/data` returns series.
6. Permissions: a `Member` token (read-only) is 403 on write/manage endpoints.
7. Angular: `npm install && npm start` → login → each grid sorts/searches/reorders columns + exports; Dashboard renders charts; admin builds a new chart and it persists; UI is RTL Arabic.

## Notes
- "breakdown-plan" skill is **not installed** in this environment; plan produced with built-in planning + a Plan agent.
- Frontend export rejected in favor of backend endpoints because grids are server-paged — only backend can export the full filtered dataset with one source of truth for Arabic column formatting.
