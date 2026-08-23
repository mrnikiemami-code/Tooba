# Tooba — PostgreSQL Persistence Foundation

Status:

```text
P01 foundation — candidate layout; not an ADR; not a P01 Gate
```

Task:

```text
TB-P01-T004
```

```text
No global business DbContext
No cross-module SQL JOIN
No cross-module EF navigation
No cross-module transaction assumption
```

PostgreSQL is the canonical relational database. No SQL Server. No SQLite as architecture truth or as a substitute for this foundation’s tests.

## What was implemented

- EF Core 8 + `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.11 + NodaTime plugin.
- Shared helpers in `Tooba.Persistence` (options, snake_case, per-module migrations history table). Not a mega model.
- Neutral disposable sample module `PlatformProbe` owning `PlatformProbeDbContext` and schema `platform_probe`.
- Connection selection reuses T003: `CommerceContext` → `ConnectionReference` → `IDatabaseConnectionResolver` → Npgsql string → scoped DbContext.
- UUID v7 helper for keys. NodaTime `Instant` for probe timestamps.
- Design-time factory using `TOOBA_DESIGN_TIME_CONNECTION` (placeholder default; no production secrets).
- Unit tests for tenant connection isolation without Docker. Optional Testcontainers smoke test skips clearly when Docker is unavailable.

## EF Core / Npgsql versions

| Package | Version |
| --- | --- |
| Microsoft.EntityFrameworkCore | 8.0.11 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 |
| Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime | 8.0.11 |
| EFCore.NamingConventions | 8.0.3 |
| Npgsql (Host parse seam) | 8.0.7 |

Physical names: snake_case via EFCore.NamingConventions.

## Module DbContext convention

```text
Modules/<Module>/.../Infrastructure/Persistence/<Module>DbContext
schema owned by that module
migrations owned by that module
history table __ef_migrations_history in that schema
```

PlatformProbe proves the convention. Future Catalog would own `catalog` / `CatalogDbContext`. Do not create those here.

Host composes `AddDbContext<PlatformProbeDbContext>` as **scoped**. Host does not own probe tables.

## Schema and migrations

Schema: `platform_probe`. Table: `probe_records`. Optional `external_reference` UUID with **no FK**.

Create a migration:

```bash
dotnet ef migrations add InitialPlatformProbe \
  --project src/backend/Modules/PlatformProbe/Tooba.PlatformProbe.Infrastructure \
  --startup-project src/backend/Modules/PlatformProbe/Tooba.PlatformProbe.Infrastructure \
  --output-dir Persistence/Migrations
```

Apply later per database (marketplace DB or each Single-Store tenant DB). This task is not a tenant migration orchestrator. Manually running random EF commands per tenant is not the production solution.

Design-time does not require an HTTP request.

## Tenant-aware connection flow

```text
Host pipeline
→ immutable CommerceContext
→ DatabaseConnectionReference
→ IDatabaseConnectionResolver
→ connection string
→ PlatformProbeDbContext (scoped)
```

No Host parsing in persistence. No process-wide DbContext. Connection pools may be reused by Npgsql for the same target.

Jobs later: create a scope with explicit TenantId or Marketplace deployment id. Not implemented here.

`/health` and `/ready` do not open DbContext.

## Transaction boundary

One module-local `SaveChanges` / EF transaction. No `TransactionScope` across modules. Later consistency: outbox/events.

No `IGenericRepository<T>` platform mandate. Probe uses DbContext directly.

## Cross-module guards

No FKs across module schemas. No DbSet/navigation to another module. Cross-module reads later: contracts, projections, gateways, events.

## Tests

- Distinct connection strings for Tenant A / B and Marketplace.
- Missing ConnectionReference fail-closed.
- No `ToobaDbContext` / `AppDbContext` types.
- Testcontainers PostgreSQL smoke test when Docker works; otherwise skip.

## Observability / safety

Sensitive EF logging off. Do not log connection strings or SQL parameters by default. Client ProblemDetails still omit internals.

## Deferred

Outbox, audit timestamps product, optimistic concurrency product, NodaTime domain-wide EF conventions beyond probe, tenant provisioning/migration orchestrator, Identity/Catalog/Pricing schemas, SpiceDB, cache, bus, jobs, UI.

## Carry-forward

- OpenTelemetry contrib package alignment later.
- Diagnostic `/__platform-*` endpoints must be tightened before public deploy.
- Config-backed tenant registry is not the production control plane.
