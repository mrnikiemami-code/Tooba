# TB-SHIPPING-MVP-ACTIVATION-053 — Shipping composition + Development schema activation (program 4 of 6)

Status: STOP_FOR_ARCHITECT. Mode: `BOUNDED_SHIPPING_MODULE_COMPOSITION_AND_DEV_SCHEMA_ACTIVATION`.

## 1. What this task changed (functional, not report-only)

### A. Host composition

`ToobaModuleComposition` now registers `new ShippingModule()` **once**, appended after
`new GeographyModule()`, and `Tooba.Host.csproj` gained the minimal project reference to
`Tooba.Shipping.Infrastructure`. Effect: `ShippingDbContext` and the existing
`IShippingRateQuoteLookup` seam become resolvable in the Host container, scoped, exactly once.

It is deliberately the same shape as the Geography precedent (044):

- `ShippingModule` (like `GeographyModule`) registers **no** `IModuleSchemaMigrator`;
- no hosted/background service, no outbox worker, no endpoint, no new price engine;
- composition is registration-only — `AddServices` adds descriptors and executes no query.

Why no migrator: the running Development Host executes **every** registered
`IModuleSchemaMigrator` at startup (`Host/Development/DevelopmentSchemaMigrator` →
`MigrateOrderedAsync` → `Database.MigrateAsync()`). Registering one here would make the next
Host start emit `CREATE SCHEMA/TABLE` DDL implicitly, which this task forbids.

### B. Canonical order slot + CLI descriptor (metadata only)

- `ModuleSchemaMigrationOrder.Shipping = 31`, appended after `Geography = 30`, in the single
  authoritative order source (`Tooba.Persistence/ModuleSchemaMigrator.cs`). Values stay unique.
- `ModuleMigrationRegistry.All` gains one descriptor, `Descriptor<ShippingDbContext>("Shipping",
  ShippingDbContext.Schema)`, appended **last** (after `Geography`). Shipping's schema has no
  foreign key outside itself, so it neither reorders nor constrains any existing descriptor.
  This is a CLI descriptor only: it never applies anything by itself and is unreachable from
  Host startup because no Shipping migrator is registered.
- No `--module` selector was invented and `MigrationRunner apply` (all descriptors) was never run.

### C. InitialShipping applied to the proven local Development tenant (authorized optional)

Applied ONLY `20261014144742_InitialShipping` through the context-scoped EF path
(`dotnet ef database update --project …/Tooba.Shipping.Infrastructure --context ShippingDbContext`)
using the module's existing design-time factory (`TOOBA_DESIGN_TIME_CONNECTION`). This is 047's
proven method; `MigrationRunner apply` was NOT used (it targets all 34 descriptors).

## 2. Proven target, baseline and backup (before any DDL)

Target = **`tooba_alpha`** on the local `postgres-db` container (`postgres:17`, `127.0.0.1:5432`):

- `appsettings.Development.json` names tenant `store-alpha` → `ConnectionReference: tenant-alpha`
  → `Host=127.0.0.1;Port=5432;Username=admin;Database=tooba_alpha`;
- `appsettings.json` and `appsettings.Production.json` carry `"Tenants": []` and
  `"ConnectionReferences": {}`, so no production/staging/shared target exists to confuse;
- the running Host holds live `pg_stat_activity` connections to `tooba_alpha` from `172.18.0.1`.

Baseline fingerprint (read-only): `schemas=53`, `tables_total=243`, `shipping_schema=0`,
`geography_tables=10`, `history_tables=33`, `history_rows=117` — no shipping schema existed.

Backup (before any DDL): `pg_dump -Fc tooba_alpha` → 2,427,186 bytes,
sha256 `c8601b76c22bb6231d386045d0bd5c0a54e9f35fc28e27c6c7437ccd6bbcebad`, created
2026-10-14T15:13:11Z, 1158 TOC lines / 243 `TABLE DATA` entries, restorability validated with
`pg_restore -l`. Kept **outside the repository** at `D:\Users\User\tooba-backups\tooba_alpha_pre053.dump`.

## 3. Measured delta (exact, shipping-only)

| metric | before | after |
|---|---|---|
| schemas | 53 | 54 |
| tables_total | 243 | 247 |
| shipping schema | 0 | 1 |
| geography tables | 10 | 10 (unchanged) |
| migration history tables | 33 | 34 |
| migration history rows | 117 | 118 |

Exactly +1 schema, +4 tables (all inside `shipping`: `rate_policies`, `coverage_grants`,
`outbox_messages`, `__ef_migrations_history`), +1 history row. `shipping.__ef_migrations_history`
contains only `20261014144742_InitialShipping`; `dotnet ef migrations list` shows no pending
migration. All three shipping tables are **empty** (`rate_policies=0`, `coverage_grants=0`,
`outbox_messages=0`) so the module stays fail-closed.

Storage-level invariants reached the database: 10 shipping check constraints, and both scope
unique indexes created with `NULLS NOT DISTINCT`:

```text
CREATE UNIQUE INDEX ux_rate_policies_service_scope ON shipping.rate_policies USING btree
  (service_code, country_id, administrative_area_id, city_id, service_area_id) NULLS NOT DISTINCT
CREATE UNIQUE INDEX ux_coverage_grants_service_scope ON shipping.coverage_grants USING btree
  (service_code, country_id, administrative_area_id, city_id, service_area_id) NULLS NOT DISTINCT
```

Rollback: `drop schema shipping cascade;` returns the database to the recorded pre-state
(53 schemas / 243 tables / 117 history rows). EF `database update 0` alone is **not** a complete
rollback (it leaves the empty `shipping` schema and an emptied history table — measured in 046).
Full fallback: restore `tooba_alpha_pre053.dump` (sha256 `c8601b76…ebad`).

## 4. Verification

- Build: `Tooba.MigrationRunner` (→ `Tooba.Host` → all modules) with output redirected outside the
  repo: **0 errors**, 11 warnings, all pre-existing and unrelated (none in the touched files).
- Focused tests: `Tooba.Shipping.Tests` → **63 passed / 0 failed** (58 pre-existing + 5 new guards in
  `Composition/ShippingModuleCompositionGuardTests.cs`) covering: one context + one lookup and zero
  schema migrators from the module root; no `AddModuleSchemaMigrator<ShippingDbContext>` anywhere in
  the backend; exactly one `new ShippingModule()` in the backend and it is the Host composition;
  unique canonical `Shipping = 31` slot after `Geography = 30`; Shipping descriptor appended last in
  the CLI registry.

## 5. Not done / boundaries honoured

- The running Host was **not** restarted and no deployment happened, so the new binary is not live;
  the legacy checkout authority therefore still serves customer prices, and the shadow rate
  evaluator (`StorefrontShippingRateShadowEvaluator`) remains unwired in DI (verified: it appears
  only in its own file and tests). Composing `ShippingModule` changes no customer-facing behaviour.
- `Tooba.Host.Tests` could not be executed in-session: the running Host locks
  `Tooba.Host/bin/Debug/net8.0`, and redirecting the test output outside the repo breaks that suite's
  `FindRepoRoot()` walk-up (same harness artifact recorded in 047/052). The affected composition
  guards were reviewed statically instead — they assert `Contains`/`DoesNotContain` membership, not
  an exact module count, so an appended module cannot break them.
- Documentation debt (not fixed, because the file was allowed "only if a DI issue requires it"):
  `ShippingModule`'s XML remarks still say the module is not wired to the Host. That text is now
  stale and should be corrected by a later authorized doc touch.
- No default rates/grants, no seeds, no `appsettings`/`Program.cs`/`DevelopmentSchemaMigrator`
  change, no Geography data/guard change, no Order/Checkout/AddressBook/Fulfillment/Payment/
  Settlement/Storefront/frontend change.
