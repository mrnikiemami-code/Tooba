# RESULT — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R5
Channel: tooba-main
Worker-Id: tooba-worker-01
Mode: BOUNDED_TYPE_CATALOG_POSTGRES_SNAPSHOT_STORAGE_AND_ATOMICITY_PROOF
Expected-Base-SHA: 6c5098dddac5d48ec8932f5bbfa22874bc8fc0e5
Base-SHA: 6c5098dddac5d48ec8932f5bbfa22874bc8fc0e5
Verdict: **BLOCKED** — the checked-in latest type-catalog migration cannot execute on real PostgreSQL 17.
No production, EF mapping, migration/designer/snapshot file was changed.

## F1 — BLOCKER: `20261022101415_ShippingTypeCatalog` fails with SQLSTATE 42703

Reproduced 5/5 times on a fresh disposable `postgres:17-alpine` target through the canonical production
context configuration (`ToobaNpgsql.ConfigureModuleContext`, i.e. `UseSnakeCaseNamingConvention` +
module history table), applying the checked-in migration chain with `IMigrator.MigrateAsync`.

PostgreSQL's own statement capture (see `raw/10-postgres-failing-statement.txt`):

```text
ERROR:  column "code" does not exist at character 466
HINT:   Perhaps you meant to reference the column "shipping_types.Code".
STATEMENT:  CREATE TABLE fulfillment.shipping_types (
        "ShippingTypeId" uuid NOT NULL,
        "Code" character varying(64) NOT NULL,
        "SortOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "Revision" bigint NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_shipping_types" PRIMARY KEY ("ShippingTypeId"),
        ...
```

`20261022101415_ShippingTypeCatalog.cs` was scaffolded with **PascalCase** column identifiers
(`name: "Code"` etc. via `migrationBuilder.CreateTable`), while the same migration's CHECK constraint text
and the checked-in model use **snake_case** (`char_length(code) ...`, `shipping_type_translations.language_id`).
Under the production naming convention the table is created with quoted `"Code"` and the CHECK body then
references the non-existent folded identifier `code`, so the whole `CREATE TABLE` (and therefore the
migration, and therefore the module schema) fails. The legacy chain (10 migrations up to
`20261016120000_AddFulfillmentShippingGroups`) applies cleanly; the failure is exactly the new type migration.

Consequences: the two owned tables never exist, so Gate 1E (additive Up/Down parity), Gate 2 (fresh-context
storage/negatives), Gate 3 (revision/unique races, mid-save rollback/cancellation) and Gate 4 (exact
constraint ownership through the real catch path) could not be executed. Their assertions are committed and
are expected to fail/prove out once the migration is repaired by an authorized task.

Per the task's own rule this is a reported blocker, **not** authority to edit the migration, the designer,
the snapshot, the EF mapping or to create reconciliation DDL.

Baseline hashes of the files that must remain untouched (verified unchanged at finish):

| File | SHA256 |
| --- | --- |
| `20261022101415_ShippingTypeCatalog.cs` | `805896E07B87670FB21BEE64D30A09628526CACEBD64681065AAC4BF6DB612C2` |
| `20261022101415_ShippingTypeCatalog.Designer.cs` | `1C13A9CC139A0A39BACE439AD48F89A5F302AF0A3D7362B5BA0AD63C3FE29E58` |
| `FulfillmentDbContextModelSnapshot.cs` | `0FC257379D86737F102D4D4D58B2DD468629003D7CEDA58ADAC1888DA54C72D6` |
| `FulfillmentDbContext.cs` | `E3F898C76BA121EA872340624832DC2D01A67F53082CC772B1AECD265E99E685` |

The designer and the snapshot carry the snake_case check text (`char_length(code) ...`) that the migration's
own `CREATE TABLE` cannot satisfy, i.e. this is scaffolding inconsistency, not a provider artefact.

## Gate 1 (A–D) — executed: parity NOT achieved, 3 substantive legacy discrepancies (BLOCKED per 1D)

Executed and recorded in `raw/`:

- historical chain applied to `fulfillment_tb196` up to `20261016120000_AddFulfillmentShippingGroups`
  (10 rows in `fulfillment.__ef_migrations_history`, 192 legacy manifest lines) — `raw/02-historical-legacy-manifest.txt`;
- model target `fulfillment_tb196_model` materialized from the checked-in snapshot/design-time model via
  `GenerateCreateScript()` after an `IMigrationsModelDiffer` comparison — `raw/03-model-expected-legacy.txt`
  (216 projected objects; the two new type tables and migration history excluded from both sides);
- normalized `pg_catalog` manifests compared and classified — `raw/04a-historical-vs-model-substantive.txt`,
  `04b-...-defaults.txt`, `04c-...-names.txt`.

Classification: 3 substantive, 5 column-DEFAULT, 10 naming differences. The 3 substantive discrepancies are
**exact legacy objects with provenance**, and per Gate 1D they are a BLOCKER (no repair was attempted):

| # | Object | Expected (model) | Historical (migrated) | Provenance |
| --- | --- | --- | --- | --- |
| 1 | index `ix_outbox_messages_pending` on `fulfillment.outbox_messages (occurred_at) WHERE processed_at IS NULL AND dead_lettered_at IS NULL` | declared | **missing** | declared only in `FulfillmentDbContextModelSnapshot.cs` / `...ShippingTypeCatalog.Designer.cs` (`HasDatabaseName("ix_outbox_messages_pending")`); **no migration ever creates it** — model-ahead drift |
| 2 | constraint `ck_shipping_methods_code_canonical` on `fulfillment.shipping_methods` | not declared | **present** | created by the hand-written raw SQL of `20261015120000_AddShippingOneLevelCatalog`; the model does not declare it — migration-ahead drift |
| 3 | constraint `ck_shipping_methods_option_code_canonical` on `fulfillment.shipping_methods` | not declared | **present** | same migration as #2 |

The 5 DEFAULT differences are migration-time defaults that the model does not declare
(`items.quantity_packed = 0`, `items.quantity_processing = 0`, `shipments.provider_metadata_version = 0`,
`shipments.shipping_method_code = ''`, `shipments.shipping_method_label = ''`); the 10 naming differences are the
same foreign key/index shapes under hand-written names (for example `ux_shipping_methods_legacy_binding` versus
the model-derived `ix_shipping_methods_legacy_service_code_legacy_option_code`). Both are reported explicitly,
not ignored, and are pre-existing (they involve no `shipping_type*` object).

Snapshot-model agreement: `IMigrationsModelDiffer` over
`IModelRuntimeInitializer.Initialize(ModelSnapshot.Model)` reported 209 operation-level differences. That path is
not the design-time pipeline, so the number is recorded as an observation only; snapshot agreement remains
**UNPROVEN** rather than claimed as drift. `GenerateCreateScript()` output was used as the model projection while
that equivalence was unproven, which is itself a limitation of this evidence.

## Small R5 fixture correction — executed

`One_active_non_fa_default_is_used_without_any_fa_fallback` now supplies `fa` as KNOWN-INACTIVE and `ar` as the
single active default, and asserts through the real registered adapter that exactly one language is active,
that it is `ar`, and that it is the declared default before create/read.

```text
dotnet test ... --filter "FullyQualifiedName~One_active_non_fa_default_is_used_without_any_fa_fallback"
-> Passed! Failed: 0, Passed: 1, Skipped: 0, Total: 1
```

## Isolation, target parse and cleanup

- Resource: one fresh disposable container `tb196r6-pg` (`postgres:17-alpine`), published only as
  `127.0.0.1:55436` (`raw/12-port-binding.txt`), role `tb196`, databases `fulfillment_tb196` and the
  authorized comparison database `fulfillment_tb196_model`. Port 55436 was verified free before start.
- Target strings are parsed with `NpgsqlConnectionStringBuilder` and rejected unless host is exactly
  `127.0.0.1`, port `55436`, role `tb196` and the database is one of the two named ones; multi-host
  strings are rejected. Live `current_database()`, `current_user` and `inet_server_port()` were asserted
  before any DDL (server-side port is 5432 inside the container; the host binding is 55436).
- Offline negatives (foreign host, multi-host, `localhost`, shared 5432, shared `tooba_alpha`,
  `postgres` database, wrong role) all fail closed; no shared database was contacted.
- No Host, migration runner, Redis/RabbitMQ, shared app, `:5432`, `tooba_alpha` or other database was used;
  no package/image install.
- Cleanup: container stopped and removed; 55436 verified free afterwards (`0` listeners). Manifest/log files
  contain no credentials.
- Note on `raw/07-after-down-legacy.txt` / `08-after-down-diff.txt`: the collection teardown comparison is
  byte-equal, but it is a **no-op equality** — the latest migration never applied, so `Down` was never recorded
  and its manifest was never changed. This is explicitly **not** Down-parity proof.

## Commands and counts

```text
dotnet build src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj -v q --nologo
  -> 0 errors

dotnet test ... --filter "FullyQualifiedName~ShippingTypePostgresStorageTests"
  -> Failed: 32, Passed: 0  (all failures are the fixture's Gate 1E blocker plus the resulting unexecuted
     storage facts; the collection teardown also fails on the recorded legacy diff)

dotnet test ... --filter "FullyQualifiedName~One_active_non_fa_default_is_used_without_any_fa_fallback"
  -> Passed: 1
```

## What remains

- **F1 must be decided by the Architect**: the type-catalog migration is not executable against real
  PostgreSQL under the production naming convention. This needs an authorized repair task (re-scaffold with
  the production convention / align column identifiers with the snapshot) before any relational or
  snapshot acceptance.
- After that repair: repeat Gate 1 with explicit default/name normalization, then Gate 1E additive Up/Down,
  Gate 2 storage matrix, Gate 3 races/rollback/cancellation and Gate 4 exact-constraint ownership — the
  committed assertions are ready and currently unexecuted.
- TB-196 remains `BLOCKED`; no production functional acceptance, no activation, no UI claim.
  Stop-State: STOP_FOR_ARCHITECT. No automatic next task.
