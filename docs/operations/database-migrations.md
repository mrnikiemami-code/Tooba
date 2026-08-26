# Database Migrations — Operations Guide

Production database upgrades for Tooba modular monolith.

## Prerequisites

- .NET 8 SDK
- Access to target PostgreSQL instance(s)
- `Tooba:Edition`, `Tooba:PostgreSQL:ConnectionReferences`, and edition-specific tenant/marketplace config in appsettings or environment variables
- **Backup** target database(s) before `apply`
- Ensure no concurrent migration runner on same database (advisory lock; see below)

## Tool

Project: `src/backend/Host/Tooba.MigrationRunner`

```bash
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- <command> [options]
```

Commands: `status` | `plan` | `apply`

## Options (Single-Store)

| Flag | Description |
|---|---|
| `--tenant <id>` | Migrate one tenant |
| `--tenants id,id` | Migrate explicit tenant set |
| `--all-tenants` | Migrate all Active tenants (explicit; never default) |

Single-Store **requires** one of the above. Default without flags exits with code 3.

## Marketplace example

```bash
# Inspect current state
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- status

# Dry-run (no writes)
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- plan

# Apply pending module migrations
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- apply
```

## Single-Store tenant example

```bash
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- plan --tenant store-alpha
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- apply --tenant store-alpha
```

## All-tenant explicit example

```bash
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- plan --all-tenants
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- apply --all-tenants
```

## Module order

19 modules in fixed order (Catalog → Offer → … → Content). Matches Development bootstrap. See `ModuleMigrationRegistry.cs`.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | Migration failure |
| 2 | Advisory lock not acquired |
| 3 | Usage / validation error |

## Rollback limitations

EF Core migrations are forward-only in this operational model. Rollback requires restoring from backup or authoring/compensating migrations in the owning module — not automated by the runner.

## Concurrent execution

`apply` acquires a PostgreSQL advisory lock per database (2-minute timeout). Second concurrent runner receives exit code 2.

## Failure recovery

- Review JSON stdout and structured logs for failed module
- Fix underlying issue (connectivity, migration SQL, permissions)
- Re-run `plan` then `apply` — EF applies only pending migrations
- Prior successfully applied modules remain committed

## Secrets handling

- Configure secrets via environment variables or secure config — never commit credentials
- Runner prints connection **reference keys** and logical database names only
- Passwords and raw connection strings are never logged

## Development vs Production

| Environment | Behavior |
|---|---|
| Development | Host auto-migrates via `ProductWorkspaceDevelopmentBootstrap` on startup |
| Production | Use migration runner explicitly; Host does not auto-migrate |

## MassTransit transport

MassTransit SQL transport schema is separate from business module EF migrations (Host messaging registration). Do not conflate with module runner operations.

## Health / readiness

Pending migrations are an operational deployment gate, not an automatic `/health/ready` failure in the current baseline.
