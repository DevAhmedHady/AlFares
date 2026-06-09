# Authentication & Authorization (Identity module)

Multi-tenant auth engine in `src/Modules/Identity`. One global **user** can belong to many
**tenants**; within a tenant the user holds tenant-scoped **roles**, and roles grant **permissions**.
Login issues a JWT scoped to **one active tenant**.

## Data model (schema `identity`)

Global: `users`, `permissions` (catalog), `roles` (templates), `role_permissions`.
Per tenant: `tenants`, `tenant_users` (membership), `tenant_roles` (cloned from templates on provisioning),
`tenant_permissions` (tenant_role → permission), `tenant_user_roles` (member → tenant_role).
Plus `refresh_tokens` (rotating, revocable, only the hash is stored).

Permission resolution at login: `tenant_users` → `tenant_user_roles` → `tenant_roles` →
`tenant_permissions` → `permissions.code`.

## Endpoints

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| POST | `/api/auth/register` | open | create a global user |
| POST | `/api/auth/login` | open | `{email,password,tenantId}` → `{accessToken, refreshToken, expiresIn}` |
| POST | `/api/auth/refresh` | open | rotate tokens (old refresh revoked) |
| POST | `/api/auth/logout` | open | revoke a refresh token |
| POST | `/api/tenants` | open (bootstrap) | provision tenant: clone role templates, add owner |
| POST | `/api/admin/tenant-users` | `identity.users.manage` | add a user to a tenant |
| POST | `/api/admin/tenant-user-roles` | `identity.users.manage` | assign a tenant role to a member |

`/api/tenants` is open so the first tenant can be created; guard it with
`.RequirePermission("identity.tenants.manage")` once a bootstrap tenant exists.

## JWT

Access token (HS256, `Jwt:SigningKey`) claims: `sub`, `email`, `tenant_id`, repeated `role`, repeated `perm`.
**One active tenant per token** — switching tenant = a new `login`/`refresh` with that `tenantId`.
Refresh tokens rotate: each refresh revokes the old token and issues a new pair; reusing a revoked token → 401.

Config in `appsettings.json` `Jwt` section (`Issuer/Audience/SigningKey/AccessMinutes/RefreshDays`).
Use user-secrets / env / key vault for `SigningKey` in real environments.

## Protecting routes (any module)

```csharp
using BuildingBlocks.Authentication;

group.MapPost("/", Handler).RequirePermission("catalog.books.write");
```

`RequirePermission` → 401 if unauthenticated, 403 if the `perm` claim set lacks the code.
Inject `ICurrentUser` (`UserId`, `TenantId`, `Permissions`) into handlers for in-code checks.

## Seeding

`IdentitySeeder` (run at startup, idempotent, best-effort) upserts the permission catalog and the
system role templates **Owner / Admin / Member**. Provisioning a tenant clones the templates into that
tenant's `tenant_roles` + `tenant_permissions`. Extend the catalog in
`Persistence/Seed/IdentitySeeder.cs`.

## Setup

```
dotnet ef database update --project src/Modules/Identity --startup-project src/Api --context IdentityDbContext
dotnet run --project src/Api
```
Then: register a user → provision a tenant with that user as owner → login with `{email,password,tenantId}`.
