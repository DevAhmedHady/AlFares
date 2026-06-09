# Plan: Revenues, Cars, Workers, Reports & related changes (الفارس)

## Context

The factory system today has four grid modules (Clients, Expenses, Todos, DashboardCharts) plus
Identity. The owner wants to grow it into a fuller financial/operations tool:

- Track **revenues** (item 0) with a managed type catalog, seeded with `مبيعات طوب`.
- Manage **cars** owned or rented by the factory, each with its own expenses (and revenues for owned cars),
  using a **car-scoped** subset of expense types for rented cars (item 1).
- Attach expenses & revenues to **clients**, auto-compute each client's **balance** (gain/loss), and add a
  printable **client report** over a date range (items 2, 3).
- Improve **tasks**: a "today's tasks" card with priority/due-time and **due-time alerts** (items 4, 5).
- Increase **session** to 2h (item 6).
- Add a **date-range + type filter** to the expenses page (item 7).
- Add **workers** (by name / job title), record **advances (سُلفة)** and **settlements (تسوية)**, and a
  **worker transactions report** over a date range (items 8, 9, 10).

The overriding constraint is **module isolation** (CLAUDE.md hard rule): a module references only
`Shared/BuildingBlocks`, never another module. Cross-module data flows through DI-registry contracts
(today only `IChartDataSource`). All four architectural decisions were confirmed with the owner:
1. **Owner reference on records** — Expense/Revenue carry a loose `OwnerType` + `OwnerId` (no cross-module FK).
2. **Lookup tables + scope flag** — `ExpenseType`/`RevenueType` entities with management screens; `ExpenseType`
   has a `Scope` (General / Car); existing free-text categories migrate into seeded types.
3. **Client-side polling + p-toast** for task alerts.
4. **Advances post to Expenses too** — recording an advance also creates an Expense (owner = Worker).

Because decisions 1, 2 and 4 require reads/writes across modules, we extend the sanctioned registry pattern
with two new BuildingBlocks contracts (mirroring `IChartDataSource`): **`ILedgerSource`** (read) and
**`ILedgerWriter`** (write).

---

## New BuildingBlocks contracts (the cross-module backbone)

Add under `src/Shared/BuildingBlocks/Ledger/`:

- **`OwnerType` enum** (`Ledger/OwnerRef.cs`): `General=0, Client=1, OwnedCar=2, RentedCar=3, Worker=4`.
  Plain enum, no module deps. Stored as `int` on Expense/Revenue.
- **`LedgerKind` enum**: `Expense=0, Revenue=1`.
- **`LedgerEntry` record**: `(Guid Id, LedgerKind Kind, OwnerType OwnerType, Guid? OwnerId, string Description,
  decimal Amount, DateOnly Date)`.
- **`ILedgerSource`** (read registry, like `IChartDataSource`):
  - `LedgerKind Kind { get; }`
  - `Task<IReadOnlyList<LedgerEntry>> GetEntriesAsync(OwnerType ownerType, Guid ownerId, DateOnly? from, DateOnly? to, CancellationToken ct);`
  - `Task<IReadOnlyDictionary<Guid, decimal>> GetTotalsAsync(OwnerType ownerType, IReadOnlyCollection<Guid> ownerIds, DateOnly? from, DateOnly? to, CancellationToken ct);`
- **`ILedgerWriter`** (write registry):
  - `Task<Guid> CreateExpenseAsync(OwnerType ownerType, Guid ownerId, Guid expenseTypeId, decimal amount, DateOnly date, string payee, string? notes, CancellationToken ct);`

`Expenses` implements `ILedgerSource` (Kind=Expense) **and** `ILedgerWriter`; `Revenues` implements
`ILedgerSource` (Kind=Revenue). Each registers via `services.AddScoped<ILedgerSource, ...>()` in its module.
Consumers (`Reports`, `Workers`) inject `IEnumerable<ILedgerSource>` / `ILedgerWriter`. This keeps every
module pointing only at BuildingBlocks.

---

## Backend changes

### A. Expenses module — types + owner ref + ledger contracts
Files: `src/Modules/Expenses/...` (mirror existing structure).

1. **`ExpenseType` aggregate** (`Domain/ExpenseType.cs`): `Name` (required), `Scope` enum
   `ExpenseScope { General=0, Car=1 }`, `IsActive`. EF config in `Persistence/`, loaded by `MainDbContext`.
2. **`Expense` changes** (`Domain/Expense.cs`): replace free-text `Category` with `ExpenseTypeId` (Guid) +
   add `OwnerType` (default General) + `OwnerId` (Guid?). Keep `Payee`, `Amount`, `Date`, `Notes`.
   Update `Create`/`Update` signatures and `ExpenseErrors`.
3. **Grid** (`Features/ExpenseFeatures.cs` → `ExpenseGrid`): add fields `expenseTypeId`/`expenseTypeName`
   (join/projection), `ownerType`, `ownerId` (all `Filterable`), keep `amount`/`date`/`payee` chartable.
   Projection returns the type name (denormalized via join in the grid handler's query).
4. **Type CRUD**: commands/queries/handlers/endpoints for `ExpenseType`
   (`GET/POST/PUT/DELETE /api/expenses/types`, grid `POST /api/expenses/types/grid`). A `?scope=Car` filter
   on the list endpoint serves rented-car forms.
5. **`ExpensesLedgerSource : ILedgerSource`** + **`ExpensesLedgerWriter : ILedgerWriter`**
   (`Ledger/ExpensesLedger.cs`): query `db.Expenses` by owner; writer builds an `Expense` via the domain
   factory. Register both in `ExpensesModule`.
6. **Seeder** (`ExpensesSeeder`): seed a handful of `ExpenseType` rows (General: مواد خام، رواتب، صيانة…;
   Car: وقود، صيانة سيارة، تأمين، رخصة) **before** seeding demo expenses; map demo expenses to type ids.
   **Migration note**: for existing dev DBs, add an EF migration that (a) creates `expense_types`,
   (b) inserts distinct existing `Category` values as General types, (c) adds `ExpenseTypeId`/`OwnerType`/
   `OwnerId` columns and back-fills `ExpenseTypeId` from the matched category, (d) drops `Category`.
   The chart data source (`ExpensesChartDataSource`) must group by type name instead of `Category`.

### B. Revenues module (new) — `src/Modules/Revenues/`, schema `revenues`
Clone the Expenses vertical (`RevenuesModule`, entity configuration, repository, features, endpoints, seeder, routes) using `IMainDbContext`.
- **`RevenueType`** (`Name` required, `IsActive`) + management endpoints (`/api/revenues/types`). Seed
  `مبيعات طوب`.
- **`Revenue`**: `RevenueTypeId`, `Amount`, `Date`, `Source`/`Payer` (string), `Notes`, `OwnerType`, `OwnerId`.
- Grid (`POST /api/revenues/grid`, `/export`), chart data source (`RevenuesChartDataSource : IChartDataSource`,
  Key `"revenues"`), and **`RevenuesLedgerSource : ILedgerSource`** (Kind=Revenue). Register in
  `RevenuesModule`; add to `AddModules(...)` in `src/Api/Program.cs`.

### C. Cars module (new) — `src/Modules/Cars/`, schema `cars`
- **`Car` aggregate**: `Name` (required), `PlateNumber` (optional), `DriverName` (optional),
  `CarType` enum `CarType { Owned=0, Rented=1 }`, timestamps. Factory + `Update`.
- Grid + CRUD endpoints (`/api/cars`, `/api/cars/grid`, `/export`), seeder with a couple demo cars,
  `CarsChartDataSource` optional (count by type) — nice-to-have, can skip.
- **Cars have no expense/revenue tables of their own.** A car's expenses/revenues are ordinary Expense/
  Revenue rows with `OwnerType=OwnedCar|RentedCar` and `OwnerId=carId`. The car detail pages reuse the
  main grids filtered by owner (see frontend). Rented cars only expose expenses and restrict the type
  picker to `Scope=Car`.

### D. Workers module (new) — `src/Modules/Workers/`, schema `workers`
- **`Worker`**: `Name` (required), `JobTitle` (optional), `IsActive`, timestamps. Grid + CRUD + seeder.
- **`Advance`** (سُلفة): `WorkerId`, `Amount`, `Date`, `Notes`, `LinkedExpenseId` (Guid). On create, the
  handler injects **`ILedgerWriter`** and calls `CreateExpenseAsync(OwnerType.Worker, workerId, <salaries/
  advances ExpenseTypeId>, amount, date, payee=worker name, notes)`, storing the returned id in
  `LinkedExpenseId` (satisfies decision 4). Resolve the expense type id by name via a small config/lookup,
  or pass a chosen type from the dialog.
- **`Settlement`** (تسوية السُلفة): `WorkerId`, `Amount`, `Date`, `Notes`. Reduces outstanding advance
  balance; tracked internally (does not post to Expenses).
- **Worker balance** = Σ advances − Σ settlements, exposed on the worker grid projection and worker detail.
- Endpoints: `/api/workers` CRUD + grid; `POST /api/workers/{id}/advances`, `POST /api/workers/{id}/settlements`,
  and `POST /api/workers/report` (`{workerId, from, to}` → advances + settlements merged, sorted by date,
  with running balance) + `/api/workers/report/export` (reuse `IGridExporterFactory`). Report lives **inside**
  Workers (self-contained data — no consumer needed).

### E. Reports module (new) — `src/Modules/Reports/`, no schema (read-only consumer)
Mirrors `DashboardCharts`: references only BuildingBlocks, injects `IEnumerable<ILedgerSource>`.
- **`POST /api/reports/owner-ledger`** `{ownerType, ownerId, from?, to?}` → `{ totalExpenses, totalRevenues,
  net, entries[] }` (entries merged from all ledger sources, sorted by date). Serves the **client report**,
  the **car expense/revenue summaries**, and the per-owner detail pages.
- **`POST /api/reports/owner-balances`** `{ownerType, ids[]}` → `id → {expenses, revenues, net}`. Serves the
  **client list balance column** (and could serve car lists). Client displayed balance =
  `Client.AccountBalance` (repurposed as **opening balance / رصيد افتتاحي**) + revenues − expenses.
- **`POST /api/reports/owner-ledger/export`** `{ownerType, ownerId, from?, to?, format, ownerName}` →
  PDF/Xlsx via `IGridExporterFactory.For(format)`; `ownerName` + period + totals passed in the export title.
- Gate with new `reports.read` permission. Register `ReportsModule` in `AddModules(...)`.

### F. Identity — session + permissions
- `src/Api/appsettings.json`: `Jwt.AccessMinutes: 15 → 120` (item 6). (Refresh flow already transparent; no
  frontend change needed.)
- `IdentitySeeder.Permissions`: append `revenues.{read,write,delete,export}`,
  `cars.{read,write,delete,export}`, `workers.{read,write,delete,export}`, `reports.read`. Roles auto-derive
  (Owner=all, Admin=all-except-tenants, Member=`*.read`) — no role edits needed. Seeder is idempotent and
  re-runs add missing permissions, but **note**: existing tenant roles are cloned at provision time, so on an
  existing DB the new permissions must also be attached to existing tenant Owner/Admin roles (extend the
  seeder to reconcile tenant roles, or document a one-off).

### G. Todos — due time
- `TodoItem` (`Domain/TodoItem.cs`): add optional `DueTime` (`TimeOnly?`). Keep `DueDate ≥ today` invariant.
  Update create/update commands, grid projection, EF config, and `TodoResponse`. Migration for the new column.

---

## Frontend changes (`web/`)

Reuse the established page pattern (signals + `GridComponent` + `p-dialog`, services extending
`GridClient<T>` in `core/api/resources.ts`, enums in `core/models.ts`, Arabic labels/formatters in
`core/labels.ts`, routes with `data.permission` in `app.routes.ts`, nav entries in the shell).

### Shared: forced-filter grid source wrapper
Add a tiny helper (e.g. `core/api/scoped-source.ts`) that wraps a `GridSource` and injects fixed filters
into every `grid()`/`export()` query (owner filters, page-level date/type filters). **No change to
`GridComponent`** — the page passes the wrapped source. Used by car detail pages and the expenses filter bar.

### New pages
- **Revenues** (`features/revenues/`) + **Revenue Types** management — clone `features/expenses/`. Service
  `RevenuesService`. Routes `revenues` (`revenues.read`) and `revenues/types`.
- **Expense Types** management page (`features/expense-types/`) with a `Scope` select. Route under expenses.
- **Cars** (`features/cars/`): list with type (`CarType` labels), plate, driver. Row action → car detail.
  - **Car detail** (`features/cars/car-detail/`): tabs/sections. Owned → Expenses grid + Revenues grid +
    financial summary (from `/api/reports/owner-ledger`). Rented → Expenses grid only, with the create
    dialog's type picker restricted to `Scope=Car` (call `/api/expenses/types?scope=Car`). Each grid uses
    the scoped-source wrapper forcing `ownerType`+`ownerId`; the create dialog pre-sets owner fields.
- **Workers** (`features/workers/`): list by name/job title with balance column. Row actions open
  **Advance dialog** (سُلفة) and **Settlement dialog** (تسوية). Route `workers` (`workers.read`).
- **Worker Report** (`features/worker-report/`): worker select + from/to + transactions table + PDF/Xlsx
  export (calls `/api/workers/report` + `/report/export`).
- **Client Report** (`features/client-report/`): client select + from/to → summary cards (opening, revenues,
  expenses, net, closing) + transactions table + PDF/Xlsx export (calls `/api/reports/owner-ledger[/export]`).

### Changed pages
- **Clients** (`features/clients/`): add a **balance** column fed by `/api/reports/owner-balances` (fetch
  balances for the page's client ids after each grid load, merge into rows via `format`). Add Expenses &
  Revenues sub-views per client (reuse the owner-scoped grids, same wrapper as cars) and a "تقرير العميل"
  action linking to the client report. Repurpose the existing balance field label to **رصيد افتتاحي**.
- **Expenses** (`features/expenses/`): add a filter toolbar above the grid — **from/to date range** +
  **expense type select** — that drives `baseFilters` on a scoped-source wrapper (item 7). Replace the
  free-text category input with a type `p-select` (loaded from `/api/expenses/types`).
- **Todos / Tasks** (`features/todos/`):
  - Add a **"مهام اليوم"** `p-card` at the top listing tasks where `dueDate == today`, showing priority
    (`todoPriorityLabels`) and `dueTime`.
  - Add a `dueTime` field (`p-inputmask`/time input) to the form and a column.
  - **Due-time alerts** (item 5): a root-level `TaskNotificationService` (provided in root, started by the
    shell) that polls `/api/todos/grid` (filtered to open tasks due today) on an interval (e.g. 2 min) and
    raises **PrimeNG `MessageService` toasts** for tasks due-soon/overdue, deduping by id in a `Set`.
    Add `<p-toast>` to the shell template and `MessageService` to app providers.

### Models / labels / enums (`core/models.ts`, `core/labels.ts`)
Add `RevenueResponse`, `ExpenseTypeResponse`, `RevenueTypeResponse`, `CarResponse`, `WorkerResponse`,
`AdvanceResponse`, `SettlementResponse`, owner-ledger/balance DTOs; enums `CarType {Owned=0,Rented=1}`,
`ExpenseScope {General=0,Car=1}`, `OwnerType {General=0,Client=1,OwnedCar=2,RentedCar=3,Worker=4}`, with
Arabic label maps. Update `ExpenseResponse` (typeId/typeName, ownerType/ownerId) and `TodoResponse` (dueTime).
Keep numeric enum values in exact sync with the C# enums.

---

## Suggested implementation order (for the implementing agent)
1. BuildingBlocks `Ledger/` contracts (`OwnerType`, `LedgerKind`, `LedgerEntry`, `ILedgerSource`, `ILedgerWriter`).
2. Identity: `AccessMinutes=120` + permission catalog additions (small, unblocks auth for new pages).
3. Expenses: `ExpenseType` + owner ref + ledger source/writer + migration + chart fix + seeder.
4. Revenues module (new) end-to-end.
5. Reports module (new) — owner-ledger / owner-balances / export.
6. Cars module (new) + frontend car pages.
7. Workers module (new, incl. advance→expense via `ILedgerWriter`, settlements, worker report) + frontend.
8. Clients page (balance column + sub-views + report link) + Client Report page.
9. Expenses page filter toolbar + type select.
10. Todos: `DueTime`, today-card, notification service + toasts.
11. Frontend models/labels/routes/nav wiring throughout.

## Verification
- **Build/test**: `dotnet build Factory.slnx`; `dotnet test tests/BuildingBlocks.Tests/BuildingBlocks.Tests.csproj`.
  Add vertical tests for new modules mirroring existing ones (Revenues, Cars, Workers, Reports), and a
  boot e2e asserting all modules register and migrations apply.
- **Migrations** (per new/changed module, dev DB on **:5433** — see memory, `ConnectionStrings__Default`):
  `dotnet ef migrations add <Name> -p src/Api -s src/Api -c MainDbContext -o Persistence/Migrations`
  then `database update`. Confirm the Expenses category→type data migration on a copy of dev data.
- **Run**: `dotnet run --project src/Api` (Scalar at `/scalar/v1`), `cd web && npm start` (4200 → API 5113).
  Manual checks:
  - Revenues page + revenue types (مبيعات طوب present); expense types page with General/Car scopes.
  - Create a car (owned) → add expense + revenue → summary shows net; rented car → expenses only, type
    picker shows car-scoped types only.
  - Client: add expense + revenue → balance column = opening + revenues − expenses; open client report for a
    date range, export PDF + Xlsx.
  - Worker: record advance → appears on main Expenses page (owner=Worker) and as outstanding balance; record
    settlement → balance drops; worker report over a range exports PDF + Xlsx.
  - Tasks: create a task due today with a due time → appears in "مهام اليوم" card; toast fires near due time.
  - Log in, leave idle past 15 min, confirm session persists (2h token).
- **Permissions**: log in as a Member (read-only) and confirm write/manage actions are hidden/forbidden on
  all new pages.
