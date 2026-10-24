# TB-SHIPPING-ADMIN-HOST-REFRESH-PREFLIGHT-207 — Result

Mode: BOUNDED_CURRENT_HOST_BUILD_AND_READ_ONLY_STARTUP_CHANGE_PLAN
Parent: TB-SHIPPING-ADMIN-LIVE-EXISTING-DRAFT-READINESS-206
Channel `tooba-main` · Worker `tooba-worker-01` · Agent `cursor` · Task row `0a4468c8-3aae-4cef-b958-4e559d4db5be`

## Verdict

**PREFLIGHT_READY_PENDING_APPROVAL — no restart, no migration, nothing mutated.**

- Isolated current `Tooba.Host` build from current `main`: **SUCCESS** (0 errors).
- Read-only schema truth established: the real dev database is **behind on 9 migrations**, and **7 of them are Shipping migrations that a Host restart will never apply** (Shipping has no `IModuleSchemaMigrator`). So a binary-only refresh would turn today's honest `404` into a `42P01 relation does not exist` failure, not into a working editor.
- There is **no** fully non-mutating startup mode in source; the exact effect set is tabled below for approval.
- Baseline / base SHA: `a932fa2915fed4827635d0cb8dbb0260baf35945` verified as pre-work `HEAD == origin/main`; working tree preserved (only pre-existing untracked historical evidence copies, untouched).

## Runtime provenance (re-verified, both processes untouched)

| Port | Process | Started | Path |
| --- | --- | --- | --- |
| 5088 | `Tooba.Host.exe` PID 24304 (reconfirmed, not just trusted) | 2026-10-22 06:41:43 | `src\backend\Host\Tooba.Host\bin\Debug\net8.0\Tooba.Host.exe` |
| 3000 | `node` PID 19108 (`next` start-server) | 2026-10-22 06:45:12 | `src\frontend` |

Startup context of the running process: `ASPNETCORE_ENVIRONMENT` of the *live* process is **UNKNOWN to this worker** (process environment not readable without instrumentation). What *is* verified: Development behaviour is active (development `/v1/admin/dev-context` answers 200), `launchSettings.json` declares `ASPNETCORE_ENVIRONMENT=Development` + `Tooba__ShippingOneLevelCatalog__Enabled=true`, and `appsettings.Development.json` resolves `Tooba:Edition = SingleStore` with only `store-alpha` Active. No credential, connection string or secret is reproduced here.

Frontend: `src/frontend/.env.local` → `TOOBA_HOST_ORIGIN=http://127.0.0.1:5088`; `next.config.ts` rewrites `/v1/:path*`. **No frontend change is needed** for a Host refresh.

## Startup-effects table (current configuration, SingleStore + Development)

`Program.cs` after `builder.Build()`; every row is **unconditional** unless the "existing suppression" column says otherwise.

| # | Startup operation | Gate in current config | Schema/data effect | Real suppression exists? |
| --- | --- | --- | --- | --- |
| 1 | `MarketplaceDevelopmentBootstrap.ApplyAsync` | `IsDevelopment && Edition == Marketplace` | n/a | **Not executed** — Edition is `SingleStore`, so this branch is skipped (source `Program.cs:359`) |
| 2 | `DevelopmentSchemaMigrator.MigrateSchemaOnlyAsync` | `IsDevelopment && Edition != Marketplace && RunLegacyBootstraps == false` | Runs **every registered** `IModuleSchemaMigrator` in `Order` 1…31 → EF `MigrateAsync` per module (DDL + history inserts), then `IModuleSchemaMigrationStep`s (Catalog/Localization/Promotion post-steps) | **NO.** `RunLegacyBootstraps=false` only skips the legacy *Catalog seed*, not this call (`Program.cs:388`) |
| 3 | `ContentDevelopmentSeedHost.ApplyAsync` | same Development block | Content demo snapshot writes | NO (only `try/catch` + error log) |
| 4 | `SupportDevelopmentSeedHost.ApplyAsync` | same | Support demo writes | NO |
| 5 | `WalletDevelopmentSeedHost.ApplyAsync` | same | Wallet demo writes | NO |
| 6 | `ShippingOneLevelCatalogDevelopmentSeed.ApplyAsync` via `DevelopmentTenantCommerceContext` on `store-alpha` | same | Fulfillment-owned one-level shipping catalog backfill writes | NO |
| 7 | Legacy Catalog seed / `CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync` | `RunLegacyBootstraps == true` only | not executed | **YES** — the only genuinely suppressed operation; message logged `Use POST /v1/admin/catalog/demo/reset-and-seed` |
| 8 | `OutboxDispatcherHostedService` (Host) | always registered (`Program.cs:174`) | polls each module `outbox_messages` and updates processing state (writes) | NO |
| 9 | `UnpaidOrderExpiryWorker` (Order), `PaymentReconciliationWorker` (Payment), `CartExpiryWorker` (Cart) | always registered by module DI | timed background writes/expiry | NO (interval config is a cadence knob, not an off switch) |
| 10 | `AuthorizationSchemaHostedService` (AccessControl) | `BootstrapIfConfiguredAsync` — internally config-gated | authorization schema bootstrap when configured | only via its own configuration |
| 11 | `LanguageBootstrapHostedService` (Localization) | always registered | `MigrateAndSeedAsync` on Localization; seeds fa/en only when the table is empty | NO |

Migrator order (`ModuleSchemaMigrationOrder`): Catalog 1, Offer 2, Pricing 3, Inventory 4, Tax 5, Party 6, Identity 7, Cart 8, Order 9, Payment 10, Fulfillment 11, Promotion 12, Reviews 14, ProductQnA 15, BulkInquiry 16, Wishlist 17, AddressBook 18, CustomerProfile 19, UserPreference 20, OperatorProfile 21, Localization 22, Content 23, Media 24, PageComposition 25, Story 26, Notification 27, AccessControl 28, Support 29, **Geography 30 and Shipping 31 exist as constants but are deliberately NOT registered** (`ShippingModule.cs` "No schema migrator is registered" + `GeographyModule.cs` same rationale; Shipping DDL is CLI-migration-runner only).

## Read-only database truth (verified live, no writes)

Probe: `scripts/readonly-sql-probe.mjs` (task-owned, dependency-free PostgreSQL wire client). Startup options force `default_transaction_read_only=on` and `statement_timeout=5000`; only `SELECT`s were issued; the connection string/password is never printed.

Verified before reading: `current_database = tooba_alpha`, `current_user = admin`, `default_transaction_read_only = on`. Alias `tenant-alpha` (the Active single-store tenant, so the same target `MigrateSchemaOnlyAsync` assigns). `__ef_migrations_history` exists per module schema (34 found), so history is real, not missing.

### Shipping — applied 6, **pending 7**

Applied: `20261014144742_InitialShipping`, `20261015030159_OptionLevelRatePolicy`, `20261015045130_OperatingSchedule`, `20261015063838_WeightAndCountRateTiers`, `20261015071836_ShippingPackagingPolicy`, `20261016211322_OptionScopedCoverageRestriction`.

Pending (none of these can be applied by a Host restart — no Shipping migrator is registered):
`20261022055446_AuthoringDraftPersistence`, `20261022065333_AuthoringGroupRateRows`, `20261022075711_AuthoringRateRowConstraintParity`, `20261022081351_AuthoringPresentationTranslations`, `20261022085811_AuthoringWeeklySchedule`, `20261022093803_AuthoringMethodSettings`, `20261022195929_AuthoringDraftTypeAssignment`.

These are exactly the draft/group/rate/presentation/week/settings/type-assignment tables the editor reads. **This is the decisive finding of this task.**

### Fulfillment — applied 10, pending 2 (a Host restart *would* apply these)

- `20261022101415_ShippingTypeCatalog` — Up: `CreateTable fulfillment.shipping_types` + `fulfillment.shipping_type_translations` (additive; no legacy column/constraint/row change, no backfill/seed).
- `20261022182208_AddFulfillmentOutboxPendingIndex` — Up: single `CreateIndex ix_outbox_messages_pending` on `fulfillment.outbox_messages (occurred_at)` with filter `processed_at IS NULL AND dead_lettered_at IS NULL`.

### Preexisting index collision — **NO**

`pg_indexes` shows `ix_outbox_messages_pending` already present in 29 other module schemas (each on its own `<schema>.outbox_messages`, identical definition). It is **absent from `fulfillment`** (absent too from `returns`, `settlement`, `wallet`, `localization`). So the pending fulfillment index migration will find no same-name index in its own schema: **collision = NO, R10 fail-closed risk not triggered by a name clash**. If the same migration is ever re-pointed at a schema that already carries the index, `CreateIndex` would raise `42P07` and fail closed — that is the guard to respect, not to work around.

### Geography — 1 applied (`20261014113949_InitialGeography`), no pending

## Isolated build (once, from current sources)

Command (task-owned, `--no-restore`, both output and intermediate redirected outside the repo so the locked `bin\Debug` and shared `obj\Debug` are never touched):

```
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release --no-restore
  -p:BaseOutputPath=D:\temp\tb207-build\bin\ -p:OutDir=D:\temp\tb207-build\out\
```

- Result: **0 Errors, 42 Warnings**, `Time Elapsed 00:00:45.50`, exit path clean, no stderr.
- Routing verified for references too: referenced module assemblies also emit to `D:\temp\tb207-build\out` (e.g. `Tooba.Shipping.Endpoints.dll`, `Tooba.Geography.Infrastructure.dll`), so the debug output tree stayed untouched.
- Output: `D:\temp\tb207-build\out` — 776 files + `Tooba.Host.exe` (151,552 bytes), `Tooba.Host.runtimeconfig.json` present, `appsettings.Development.json` copied next to the binary.
- SHA-256 (first 16 hex): `Tooba.Host.dll FD7C37F4CC13E95F`, `Tooba.Shipping.Endpoints.dll 9F826441F2B6431D`, `Tooba.Fulfillment.Infrastructure.dll 5F6824C725EA1A3F`. `/health` and route smoke of the new binary were **not** run (executing it would start the mutating startup path).
- Binaries are **not committed**; they remain only in the task-owned temp folder for a later approved refresh.

## Refresh / rollback plan (for review — nothing executed)

**Rollback (captured, no secrets):** current executable `D:\Users\User\source\repos\SarvNewVer\src\backend\Host\Tooba.Host\bin\Debug\net8.0\Tooba.Host.exe`, working directory the same folder, Development environment, port 5088, edition SingleStore, tenant `store-alpha`. That directory is untouched by this task, so rollback = restart it from that folder exactly as today.

**Candidate refresh:** new executable `D:\temp\tb207-build\out\Tooba.Host.exe` (source SHA = this commit), working directory `D:\temp\tb207-build\out`, same environment/edition/port `5088`, `appsettings.Development.json` already co-located. Frontend stays on `3000` and its rewrite needs no change.

**Non-mutating startup mode available today: NONE** (source- and config-proven above; `RunLegacyBootstraps` is not such a switch). Therefore a refresh is a *combined* runtime + schema + data event, split into two separately approvable steps:

1. **Schema step (shipping only, no Host restart):** apply the 7 pending Shipping migrations with the existing `Tooba.MigrationRunner` against `tooba_alpha`. Host will never do this, and the editor cannot work without it. This step is pure additive DDL (EF `MigrateAsync`) with no seed.
2. **Runtime step (binary swap on 5088):** with the new binary, the same startup block will then apply the 2 pending **Fulfillment** migrations and also run Content/Support/Wallet seeds, the shipping one-level backfill, and the background workers. If those effects are not approved, the runtime step must wait: there is no supported "migrate nothing, seed nothing" switch to hide behind.

If approval is only for the runtime step, sequence must still be 1 → 2, otherwise the first editor read fails with `42P01`.

**Smallest next action for approval:** one bounded task that (a) runs `Tooba.MigrationRunner` for Shipping only and verifies the 7 ids land in `shipping.__ef_migrations_history`, then (b) stops the old Host and starts the new isolated binary on 5088 with the same environment. Approving (a) is independent of approving (b)'s seed/worker effects. Names of the pending changes are listed above — do not approve "all migrations" generically.

## Limits

- No restart, no migration run, no seed, no DDL/DML, no `Tooba.Host.exe` execution, no build of anything but the Host, no shared process/port/config change, no package install, no Interception/mocks, no business payload read.
- Concurrency state of the running Host's own in-memory view is not modelled; only its binary/source chronology.
- The genuine auth boundary flagged by 206 (`401` on `/v1/admin/shipping-services`) is still unproven after route refresh; dev-context is not by itself an authorization for shipping reads.
- Production-Functional-Acceptance: PENDING. Live-Write-Acceptance: PENDING. Visual: none claimed.
- Stop-State: STOP_FOR_ARCHITECT. Automatic-Next-Task: NONE.

## Process

- Claimed 2026-10-24T16:54:52Z; build started 16:57:47Z, finished ~16:59:00Z; new work stopped at the minute-14 line. No deviation.
- Cleanup: no task-owned server/browser was started (none was needed); `3517`/`9555` remained free; shared `3000`/`5088` untouched and still listening; temp build folder intentionally retained as the refresh artifact, no sensitive file created (the probe reads credentials from the existing settings file at runtime and prints none).
- Scoped diff: this `RESULT.md`, `scripts/readonly-sql-probe.mjs`, the verbatim task receipt, and one recovery pointer.
