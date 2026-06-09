# Codex Execution Backlog — Factory "الفارس"

> Companion to [`project-plan.md`](./project-plan.md). Source: [`IMPLEMENTATION_PLAN.md`](../../../../IMPLEMENTATION_PLAN.md).
> **Execute top-to-bottom with Codex CLI (GPT-5.5).** Each task is INVEST-sized: it lists the files to touch, acceptance criteria, and a Definition of Done. Respect module isolation — **never add a `ProjectReference` between modules**; cross-module data flows only through the `IChartDataSource` DI registry.
> Apply the `dotnet-best-practices` skill to every C# task (XML docs on public APIs, primary-ctor DI with null checks, async/await + `Task<T>`, structured logging, strongly-typed config, SOLID, no duplication).
> 🔶 = **REVIEW GATE** (stop for human review before continuing).

---

## Definition of Done (global — every task)
- [ ] Code compiles (`dotnet build`) with no new warnings.
- [ ] Public types/methods have XML doc comments.
- [ ] New behavior covered by at least one test (unit or integration) where applicable.
- [ ] No cross-module `ProjectReference`; modules reference only `Shared/BuildingBlocks`.
- [ ] Endpoints enforce the correct `.RequirePermission(...)`.
- [ ] The task's stated verification command passes.

---

## Milestone M0 — Scaffold & Config  (Feature F1, P0)

- [x] **T001 — Copy template → Factory solution.** Copy `knights-templates` to `C:\Users\AhmedHady\source\repos\Factory`. Rename `knights.slnx` → `Factory.slnx`. Update solution-name references.
  - *Files:* whole tree; `Factory.slnx`.
  - *Accept:* `dotnet build Factory.slnx` succeeds on the unmodified copy.
- [x] **T002 — Remove sample modules.** Delete `src/Modules/Catalog` and `src/Modules/Ordering`; remove their `ProjectReference` from `src/Api/Api.csproj` and their `typeof(...).Assembly` lines + `using` from `src/Api/Program.cs`.
  - *Accept:* `dotnet build` succeeds; no Catalog/Ordering symbols remain.
- [x] **T003 — App config for الفارس.** In `src/Api/appsettings.json`: `ConnectionStrings:Default` → `Database=alfaris`; `Jwt:Issuer`/`Audience` → `al-faris`; add `Seed` section: `AdminEmail=admin@alfaris.local`, `AdminPassword`, `TenantName=الفارس`, `TenantSlug=al-faris`. Add a strongly-typed `SeedOptions` bound via `IConfiguration`.
  - *Files:* `appsettings.json`, `appsettings.Development.json`, new `Api/Configuration/SeedOptions.cs`.
  - *Accept:* app boots reading `SeedOptions`; secrets not hard-coded.

---

## Milestone M1 — Shared Infra (Feature F2, P0) 🔶 linchpin

### Enabler EN1 — Grid query engine — `src/Shared/BuildingBlocks/Grids/`
- [x] **T010 — Grid DTOs.** `GridQuery`, `GridSort`, `GridFilter`, `GridFilterOp` enum, `PagedResult<T>` (with `TotalPages`).
  - *Accept:* records compile; `PageSize` clamped helper present.
- [x] **T011 — Grid field metadata.** `GridFieldType` enum; `GridField` record (Key, DisplayName, Type, Searchable, Sortable, Filterable, Chartable); `GridFieldMap<T>` (Fields list + `Expression<Func<T,object?>> Selector(string key)`).
  - *Accept:* a map can be built statically per entity; lookups by key O(1).
- [x] **T012 — `ApplyGridQuery` engine.** `GridQueryExtensions.ApplyGridQuery<T>(IQueryable<T>, GridQuery, GridFieldMap<T>) → Result<IQueryable<T>>`: validate every Sort/Filter/Search field against the map (unknown → `Error.Validation("grid.unknown_field", key)`); fold Sort into `OrderBy/ThenBy`; AND per-column filters (build `BinaryExpression`/`string.Contains`/comparisons per op, parse value to CLR type from `GridFieldType`); OR global `Search` across `Searchable` text fields. **Hand-rolled expressions — do NOT add System.Linq.Dynamic.Core.**
  - *Accept:* unknown field returns failure (no exception); valid query returns filtered `IQueryable`.
- [x] **T013 — `ToPagedResultAsync`.** `ToPagedResultAsync<T,TOut>(IQueryable<T>, GridQuery, Expression<Func<T,TOut>> projection, CancellationToken) → Task<PagedResult<TOut>>`: COUNT then Skip/Take; clamp `PageSize` to [1..200].
  - *Accept:* returns correct `TotalCount` + page slice.
- [x] **T014 — TEST: grid engine.** Unit tests over an in-memory/SQLite `IQueryable`: each `GridFilterOp`, multi-column sort precedence, global search OR, unknown-field rejection, page clamping.
  - *Accept:* all pass.

### Enabler EN2 — Chart registry contracts — `src/Shared/BuildingBlocks/Charts/`
- [x] **T015 — Chart contracts.** `ChartAggregation` enum; `ChartPoint`, `ChartSeries`, `ChartFieldDescriptor`, `ChartDataSourceMetadata`, `ChartComputeRequest` (reuse `GridFilter`/`GridFieldType`).
- [x] **T016 — `IChartDataSource`.** Interface: `Key`, `DisplayName`, `Describe()`, `ComputeAsync(ChartComputeRequest, CancellationToken)`. XML-doc the inversion contract (implemented by grid modules, consumed by Dashboard via DI).
  - *Accept:* interface compiles; no dependency on any module.

### Enabler EN3 — Export — `src/Shared/BuildingBlocks/Export/`
- [x] **T017 — Export packages.** Add `ClosedXML` + `QuestPDF` to `Directory.Packages.props`; reference from `BuildingBlocks.csproj`.
  - *Accept:* restore succeeds; versions pinned.
- [x] **T018 — Export contracts + Excel.** `ExportFormat` enum, `ExportColumn` record, `IGridExporter` interface; `ExcelGridExporter` (ClosedXML) — headers from `ExportColumn`, typed cells.
  - *Accept:* produces a valid `.xlsx` byte array from sample rows.
- [x] **T019 — PDF exporter (RTL).** `PdfGridExporter` (QuestPDF) — document `DirectionFromRightToLeft`, embed Arabic font (Cairo/Amiri under `BuildingBlocks/Assets/`), table of columns/rows, title.
  - *Accept:* Arabic header text renders RTL in the produced PDF.
- [x] **T020 — Export DI registration.** Register both exporters keyed by `ExportFormat`; a small factory/resolver `IGridExporterFactory.For(ExportFormat)`. Clamp exports to ~50k rows.
  - *Accept:* resolving by format returns the right exporter.
- [x] **T021 — TEST: exporters.** Round-trip xlsx (open + read header/first row); PDF non-empty + contains Arabic glyphs.

> 🔶 **REVIEW GATE M1** — freeze `GridQuery`/`PagedResult`/`IChartDataSource`/`IGridExporter` contracts before building modules.

---

## Milestone M2 — Business Modules (Features F3/F4/F5, P1) — parallelizable after M1

### Feature F3 — Clients  `src/Modules/Clients/` (schema `clients`)
- [x] **T030 — Project + module shell.** Create `Clients.csproj` (copy old Catalog csproj; ref BuildingBlocks + EFCore.Design). `ClientsModule : IModule`.
- [x] **T031 — Domain.** `Client` aggregate (Name, Contact{Name,Phone,Email}, AccountBalance, `ActivityLevel{Low,Medium,High}`, `Status{Active,Inactive}`, Notes, timestamps); value objects reused (`Email`, `Money`-style); `Client.Create/Update/SetStatus → Result<Client>`; `ClientErrors`; `IClientRepository`.
- [x] **T032 — Persistence.** `ClientsDbContext` (`HasDefaultSchema("clients")`), `ClientConfiguration`, `ClientRepository`, `ClientsDbContextFactory : IDesignTimeDbContextFactory`.
- [x] **T033 — Contracts + Mapping.** Request/Response records; `ClientsMappingConfig : IRegister` (entity→response unwrapping VOs, request→command).
- [x] **T034 — CRUD features.** Create/Update/SetStatus/Delete/GetById commands+queries+handlers (+ FluentValidation validators). All return `Result<T>`.
- [x] **T035 — Grid query + field map.** `ClientGrid` static (GridFieldMap + projection, Arabic DisplayNames, Chartable flags on status/activityLevel/accountBalance/createdAt); `GetClientsGridQuery` + handler using `ApplyGridQuery`/`ToPagedResultAsync`.
- [x] **T036 — Endpoints.** `POST /api/clients` (write), `PUT /{id}` (write), `DELETE /{id}` (delete), `GET /{id}` (read), `POST /api/clients/grid` (read), `POST /api/clients/export` (export, strip paging → full filtered set via `IGridExporter`). Permissions `clients.read/write/delete/export`.
- [x] **T037 — ClientsChartDataSource.** Implement `IChartDataSource` (Key `clients`): `Describe()` lists chartable fields; `ComputeAsync` groups by X, aggregates Y (Count/Sum/Avg switch over allow-listed selector). Register `AddScoped<IChartDataSource, ClientsChartDataSource>()`.
- [x] **T038 — Seeder.** `ClientsSeeder` + `ClientsSeedHostedService` (~15 rows across statuses/levels; idempotent if `Any()`).
- [x] **T039 — TEST: Clients vertical.** Integration: create→grid(filter+sort)→export(xlsx); chart compute x=status,Count returns correct buckets.

> 🔶 **REVIEW GATE M2a** — Clients is the reference vertical slice. Review before cloning to Expenses/Todos.

### Feature F4 — Expenses  `src/Modules/Expenses/` (schema `expenses`)
- [x] **T040 — Module + Domain.** `Expense`(Category, Amount(Money), Date(DateOnly), Payee, Notes, timestamps); factory; repo; errors. (Mirror Clients structure.)
- [x] **T041 — Persistence + Contracts + Mapping.** `ExpensesDbContext` schema `expenses`; config; factory; DTOs; mapping.
- [x] **T042 — CRUD + Grid.** Create/Update/Delete/GetById; `ExpenseGrid` map; `GetExpensesGridQuery`.
- [x] **T043 — Endpoints + permissions.** `/api/expenses` CRUD + `/grid` + `/export`; `expenses.read/write/delete/export`.
- [ ] **T044 — ExpensesChartDataSource.** Implement `IChartDataSource` (Key `expenses`); support **month-bucketing** of `Date` for the Line chart; Sum on `amount`.
- [ ] **T045 — Seeder.** ~30 rows over several months + categories (idempotent).
- [ ] **T046 — TEST: Expenses vertical** (grid + export + chart month-bucket sum).

### Feature F5 — Todos  `src/Modules/Todos/` (schema `todos`)
- [ ] **T050 — Module + Domain.** `TodoItem`(Title, DueDate, `Status{Open,InProgress,Done}`, `Priority{Low,Normal,High,Urgent}`, Notes, timestamps); **factory enforces DueDate ≥ today** (time-restricted invariant); repo; errors.
- [ ] **T051 — Persistence + Contracts + Mapping.** Schema `todos`; config; factory; DTOs; mapping.
- [ ] **T052 — Features + Grid.** Create/Update/ChangeStatus/Delete/GetById; `TodoGrid` map; `GetTodosGridQuery`.
- [ ] **T053 — Endpoints + permissions.** `/api/todos` CRUD + `/grid` + `/export`; `todos.read/write/delete/export`.
- [ ] **T054 — TodosChartDataSource.** Key `todos`; x=priority/status Count.
- [ ] **T055 — Seeder.** ~10 rows across priorities/statuses, future due dates (idempotent).
- [ ] **T056 — TEST: Todos vertical** (DueDate invariant, grid, chart).

---

## Milestone M3 — Configurable Dashboard (Feature F6, P1)  `src/Modules/DashboardCharts/` (schema `dashboard`)

- [ ] **T060 — Module + Domain.** `ChartDefinition` aggregate (Title-AR, `ChartType{Bar,Pie,Line}`, DatasourceKey, XField, YField?, Aggregation, ColorsJson(jsonb), FiltersJson?(jsonb), LayoutOrder, IsEnabled, timestamps; `Create/Update → Result<>`); repo; errors.
- [ ] **T061 — Persistence.** `DashboardChartsDbContext` schema `dashboard`; `chart_definitions` table with `colors_json`/`filters_json` as `jsonb`; factory.
- [ ] **T062 — Registry consumer.** `ChartDataSourceRegistry(IEnumerable<IChartDataSource>)` — `All()` → metadata list, `Get(key)`; register scoped.
- [ ] **T063 — Features.** CreateChart/UpdateChart/DeleteChart/GetCharts; GetDatasources (from registry); GetChartData (load def → `registry.Get(DatasourceKey)` → build `ChartComputeRequest` → `ComputeAsync`; missing key → `Error.NotFound`); PreviewChartData (unsaved def).
- [ ] **T064 — Endpoints.** `GET /api/dashboard/datasources` (manage), `GET/POST /charts` , `PUT/DELETE /charts/{id}`, `GET /charts/{id}/data` (read), `POST /charts/preview` (manage). Permissions `dashboard.charts.read/manage`.
- [ ] **T065 — Palette + default charts seeder.** `DashboardPalette` constant; `DashboardChartsSeeder` (idempotent by Title): Pie clients-by-status(Count); Bar expenses-by-category(Sum); Bar todos-by-priority(Count); Line expenses-over-time(Sum, month-bucket).
- [ ] **T066 — TEST: Dashboard.** Preview pie(clients,status,Count) → series; save → appears in `GET /charts`; `GET /charts/{id}/data` returns series; removed datasource key → NotFound.

> 🔶 **REVIEW GATE M3** — verify inversion holds (no module refs DashboardCharts; Dashboard refs no module).

---

## Milestone M4 — Identity, Auth & User Management (Feature F7, P0)

- [ ] **T070 — Extend permission catalog.** In `Identity/Persistence/Seed/IdentitySeeder.cs` add all `clients.*`, `expenses.*`, `todos.*`, `dashboard.charts.*` codes.
- [ ] **T071 — Role templates.** Owner = all; Admin = all except `identity.tenants.manage`; Member = `*.read` only. Idempotent upsert.
- [ ] **T072 — Single-tenant + admin seed.** New `IdentityTenantSeeder` invoked from existing `IdentitySeedHostedService` after perms/roles: idempotent by slug `al-faris` — create tenant الفارس + admin `User` (from `SeedOptions`) + Owner membership.
- [ ] **T073 — Users grid endpoint.** `GET /api/admin/users/grid` (or POST) over `IdentityDbContext.Users` using shared `GridQuery` infra + a `UserGrid` field map. Permission `identity.users.read`.
- [ ] **T074 — TEST: auth + RBAC.** Login as seeded admin → JWT; Member token → 403 on a write/manage endpoint; users grid returns paged users.

---

## Milestone M5 — API Wiring & Migrations (Feature F8, P0)

- [ ] **T080 — Register modules.** `src/Api/Program.cs`: `AddModules(... IdentityModule, ClientsModule, ExpensesModule, TodosModule, DashboardChartsModule)`; add `ProjectReference`s in `Api.csproj`.
- [ ] **T081 — RTL font + CORS.** Register QuestPDF Arabic font once at startup; add CORS policy for Angular dev origin (`http://localhost:4200`).
- [ ] **T082 — Migrations.** For Identity, Clients, Expenses, Todos, DashboardCharts run `dotnet ef migrations add InitialCreate -p Modules/<X> -s Api -c <X>DbContext -o Persistence/Migrations` then `database update`.
  - *Accept:* schemas `identity/clients/expenses/todos/dashboard` exist with `__ef_migrations_history` each.
- [ ] **T083 — TEST: boot + seed e2e.** `dotnet run` → seeders log; DB has tenant الفارس, admin, sample clients/expenses/todos, 4 default charts; `/health` green; `POST /api/clients/grid` returns `PagedResult`; unknown field → 400 `grid.unknown_field`.

> 🔶 **REVIEW GATE M5** — backend complete + verifiable via Scalar before frontend.

---

## Milestone M6 — Angular RTL SPA (Feature F9, P1)  `web/`

- [ ] **T090 — Scaffold Angular app.** `web/` Angular + TS; `index.html` `dir="rtl"` `lang="ar"`; i18n (Arabic default, EN secondary); base theme + RTL styles.
- [ ] **T091 — OpenAPI TS client.** Generate typed API client/services from `/openapi`; env config for API base URL.
- [ ] **T092 — Auth.** Login page (single tenant auto-selected), JWT storage, HTTP interceptor (bearer + refresh-token flow), permission route guards.
- [ ] **T093 — Reusable RTL grid component.** Wrap AG Grid Angular: server-side row model → `POST /api/<x>/grid` with `GridQuery`; sort, global + per-column filter, **column reorder drag&drop**, show/hide; toolbar **Export PDF/Excel** → `POST /api/<x>/export` → download blob (full filtered set).
- [ ] **T094 — Grid pages.** Clients, Expenses, Todos, Users pages using the shared grid component + CRUD dialogs.
- [ ] **T095 — Dashboard view.** `ng-apexcharts`; list saved charts (`GET /charts` + per-chart `GET /charts/{id}/data`); render bar/pie/line with stored colors; drag to reorder (`LayoutOrder`).
- [ ] **T096 — Chart builder dialog.** Admin: type → datasource (`GET /datasources`) → X / Y + aggregation → color scheme (default palette + custom) → live preview (`POST /charts/preview`) → save.
- [ ] **T097 — TEST: frontend e2e.** `npm install && npm start` → login → each grid sorts/searches/reorders/exports; dashboard renders; admin builds + persists a chart; UI is RTL Arabic.

> 🔶 **REVIEW GATE M6** — full app demo.

---

## Milestone M7 — Docs & Agent Files (Feature F10, P2)

- [ ] **T100 — CLAUDE.md** (repo root): stack, module conventions (link `docs/adding-a-module.md`), how to add a grid + chart datasource, build/run/migrate commands, Angular dev commands, RTL/Arabic notes.
- [ ] **T101 — AGENTS.md** (Codex agent file): operational guidance for Codex-CLI — build/test/migrate/run commands, conventions, the rule "never reference another module — use the `IChartDataSource` registry", per-module Definition of Done (migration applied, seeder idempotent, grid+export+chart wired, permissions enforced).

---

## Filled Issue Templates (reference)

### Epic
```markdown
# Epic: Factory الفارس High-Level Management
Business Value: one management surface for clients, expenses, dashboard, todos (single-tenant, Arabic RTL).
Acceptance: 6 grid pages (sort/search/reorder/export) + configurable charts dashboard + seeded admin/demo data.
Features: F1 Scaffold · F2 Infra · F3 Clients · F4 Expenses · F5 Todos · F6 Dashboard · F7 Identity · F8 API · F9 SPA · F10 Docs.
Labels: epic, priority-critical, value-high. Estimate: XL (~82 pts).
```

### Enabler example (F2/EN1)
```markdown
# Technical Enabler: Grid query engine (GridQuery + ApplyGridQuery)
Requirements: safe hand-rolled sort/filter/search over IQueryable gated by GridFieldMap allow-list; PagedResult.
Tasks: T010–T014. Enables: every grid story (F3/F4/F5/F7) + export + chart registry.
Acceptance: unknown field → Error.Validation; all ops translate to SQL; page clamp [1..200].
DoD: unit tests green, XML docs, no Dynamic.Core dependency. Labels: enabler, priority-critical, backend.
```

### Story example (F6)
```markdown
# User Story: Admin defines a custom dashboard chart
As an admin, I want to define a chart (type, datasource grid, X, Y, aggregation, colors) so that I can tailor the dashboard.
Acceptance: pick from registered datasources; live preview; save persists; renders with chosen colors; reorder by drag.
Tasks: T063, T064, T096. Tests: T066, T097. Blocked by: F2, F3/F4/F5. Labels: user-story, priority-high, fullstack.
```

---

## Progress Summary
- M0 Scaffold: T001–T003
- M1 Infra 🔶: T010–T021
- M2 Modules 🔶: T030–T056
- M3 Dashboard 🔶: T060–T066
- M4 Identity: T070–T074
- M5 API 🔶: T080–T083
- M6 SPA 🔶: T090–T097
- M7 Docs: T100–T101

**Total: ~60 tasks / 8 milestones / 6 review gates.**
