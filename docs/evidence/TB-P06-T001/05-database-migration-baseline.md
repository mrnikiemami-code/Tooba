# 05 — Database migration baseline (TB-P06-T001)

| Aspect | Policy |
|---|---|
| Strategy | Per-module EF Core migrations in each module Infrastructure assembly |
| History table | Per-schema `__ef_migrations_history` via `ToobaNpgsql` |
| Development | `ProductWorkspaceDevelopmentBootstrap.ApplyAsync` migrates all module contexts on startup |
| Production | **No auto-migrate on startup** — explicit ops step required |
| Destructive reset | **Forbidden** in production path; dev bootstrap only in Development |
| Marketplace vs Single-Store | Separate connection references; tenant DB per Single-Store tenant |
| MassTransit SQL transport | `AddPostgresMigrationHostedService` for transport infra only (not business schemas) |

Recommended production command pattern (ops): run module migrations per tenant DB using documented EF tooling against each `ConnectionReference` — deferred to dedicated P06 migration-runner task if needed.

No tenancy architecture redesign in this task.
