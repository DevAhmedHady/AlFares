# Adding a Module

This solution is a **modular monolith**. Each module is its own project under `src/Modules/<Name>`,
references only the shared building blocks (never another module), and is discovered automatically by
the host. Use the `Catalog` module as the reference implementation and `Ordering` as a minimal example.

## Architecture at a glance

```
src/
  Shared/
    SharedKernel/     Entity, AggregateRoot, ValueObject, Result, Error, IDomainEvent  (no framework deps)
    BuildingBlocks/   IHandler/ICommand/IQuery/IDispatcher, Validation+Logging decorators,
                      IEndpoint, IModule, ResultExtensions, Mapster (AddMappings), AddModuleDbContext
  Modules/
    <Name>/           one self-contained vertical-slice module (this guide)
  Api/                thin host: AddBuildingBlocks() + AddModules(...) + MapModuleEndpoints()
```

The host (`src/Api/Program.cs`) contains **no business logic**:

```csharp
builder.Services.AddBuildingBlocks();
builder.Services.AddModules(builder.Configuration,
    typeof(CatalogModule).Assembly,
    typeof(OrderingModule).Assembly);
...
app.MapModuleEndpoints();
```

`AddModules` scans the given module assemblies and wires, per module: the `IModule.Register` services,
every `IHandler<,>` (decorated with Validation → Logging), every FluentValidation validator, and every
Mapster `IRegister` profile. Mapster maps are compiled at startup, so a bad map fails the boot.

## Conventions

| Concern | Convention |
|---|---|
| Project | `src/Modules/<Name>/<Name>.csproj`, root namespace `<Name>` |
| Module entry | `<Name>Module : IModule` |
| Domain | `<Name>.Domain` — aggregate, value objects, errors, repository interface |
| DTOs | `<Name>.Contracts` — `XxxRequest`, `XxxResponse` (transport shapes) |
| Features | `<Name>.Features.<Action>` — `<Action>Command`/`Query`, `Handler`, `Validator` |
| Mapping | `<Name>.Mapping.<Name>MappingConfig : IRegister` |
| Persistence | `<Name>.Persistence` — `<Name>DbContext` (own schema), EF configs, repository, design-time factory |
| Endpoints | `<Name>.Endpoints` — one `IEndpoint` per route |
| Routes | `<Name>Routes` constants |

Mapping rules: Mapster handles **entity → response** (unwrap value objects with `.Map(...)`) and
**request → command**. Domain creation stays in the aggregate factory (`Xxx.Create(...) → Result<T>`)
so invariants are never bypassed.

## Checklist

1. **Create the project** `src/Modules/<Name>/<Name>.csproj` (copy `Catalog.csproj`): reference
   `Shared/BuildingBlocks` and the `Microsoft.EntityFrameworkCore.Design` package.
2. **Domain** — aggregate(s) deriving from `AggregateRoot`, value objects from `ValueObject`, an
   `Xxx.Create(...)` factory returning `Result<T>`, an errors class, and a repository interface.
3. **Contracts** — request/response records.
4. **Features** — for each use case a command/query (`: ICommand<T>` / `: IQuery<T>`), a handler
   (`: ICommandHandler<,>` / `: IQueryHandler<,>`), and an optional FluentValidation validator.
5. **Mapping** — a `<Name>MappingConfig : IRegister` with the entity→response and request→command maps.
6. **Persistence** — `<Name>DbContext` with `mb.HasDefaultSchema("<name>")`, `IEntityTypeConfiguration`s,
   the repository implementation, and an `IDesignTimeDbContextFactory<<Name>DbContext>`.
7. **Endpoints** — one `IEndpoint` per route; bind a Request DTO, map to a command/query, dispatch,
   `.ToHttpResult(...)`.
8. **Module** — implement `IModule`:
   ```csharp
   public sealed class <Name>Module : IModule
   {
       public string Name => "<Name>";
       public void Register(IServiceCollection services, IConfiguration config)
       {
           services.AddModuleDbContext<<Name>DbContext>(config, Name, <Name>DbContext.Schema);
           services.AddScoped<I<X>Repository, <X>Repository>();
       }
       public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
           endpoints.MapEndpointsFromAssembly(typeof(<Name>Module).Assembly);
   }
   ```
9. **Register with the host** — add a `ProjectReference` in `src/Api/Api.csproj` and one
   `typeof(<Name>Module).Assembly` entry in `AddModules(...)`. Nothing else in the host changes.
10. **Migrations**
    ```
    dotnet ef migrations add InitialCreate \
      --project src/Modules/<Name> --startup-project src/Api \
      --context <Name>DbContext --output-dir Persistence/Migrations
    dotnet ef database update --project src/Modules/<Name> --startup-project src/Api --context <Name>DbContext
    ```

## Choosing the database (per module)

`AddModuleDbContext<TContext>(config, moduleName, schema)` resolves the target:

- **Shared database** (default): no `ConnectionStrings:<Name>` entry → the module uses
  `ConnectionStrings:Default`, isolated by its own Postgres **schema** and its own migrations-history
  table (`<schema>.__ef_migrations_history`). Modules never collide.
- **Separate database**: add `ConnectionStrings:<Name>` in `appsettings.json` → the module gets its
  **own database**.

No code change is needed to switch — only configuration.
