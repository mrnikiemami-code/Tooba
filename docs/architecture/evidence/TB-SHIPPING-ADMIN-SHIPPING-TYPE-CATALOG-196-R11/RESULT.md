# TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R11 — RESULT

Status: **PASS — twelve already-existing database facts are now described by the canonical Fulfillment model.**
No migration, no database rename, no default/insert-generation change. Whole TB-196 remains blocked pending the
five defaults and the full relational/storage acceptance.

| Item | Value |
| --- | --- |
| Mode | `BOUNDED_EXISTING_CHECK_AND_HISTORICAL_OBJECT_NAME_MODEL_ALIGNMENT` |
| Expected base | `8aaf599b1212ea50c1fc75a149892b7317b5b9df` |
| Actual base | `8aaf599b1212ea50c1fc75a149892b7317b5b9df` (fast-forward from `d49ce7b2d`, `HEAD == origin/main` before edits) |
| Claim-UTC | 2026-10-22T18:39:11Z |

## 1. The exactly authorized facts (2 checks + 10 names)

Added inside the existing entity configuration of `FulfillmentDbContext` (no new file, no new abstraction):

**Checks (described verbatim as the historical raw SQL created them — protection kept, not replaced by the stricter
domain validator):**

* `ck_shipping_methods_code_canonical` = `length(method_code) BETWEEN 1 AND 32 AND method_code = lower(method_code) AND method_code !~ '[^a-z0-9_:]'`
* `ck_shipping_methods_option_code_canonical` = `legacy_option_code IS NULL OR (length(btrim(legacy_option_code)) BETWEEN 1 AND 64 AND legacy_option_code = lower(legacy_option_code))`

Both originate in `20261015120000_AddShippingOneLevelCatalog.cs:31-42` (raw `CREATE TABLE ... CONSTRAINT ... CHECK`).

**Physical names pinned with `HasDatabaseName` / `HasConstraintName`, shapes untouched:**

| # | Object | Pinned name | Verified shape (unchanged) |
| --- | --- | --- | --- |
| 1 | FK `shipping_method_translations` | `fk_shipping_method_translations_method` | `(shipping_method_id)` → `shipping_methods(shipping_method_id)`, `Cascade` |
| 2 | `consolidated_package_members` | `ix_consolidated_package_members_package_id` | `(consolidated_package_id)`, non-unique |
| 3 | `consolidated_package_members` | `ix_consolidated_package_members_shipment_active` | `(shipment_id)` unique `WHERE released_at IS NULL` |
| 4 | `shipping_groups` | `ix_shipping_groups_fulfillment_group` | `(fulfillment_id, shipping_group_id)` unique |
| 5 | `shipping_method_translations` | `ix_shipping_method_translations_method_lang` | `(shipping_method_id, language_id)` unique |
| 6 | `shipping_methods` | `ux_shipping_methods_legacy_binding` | `(legacy_service_code, legacy_option_code)` unique `WHERE legacy_option_code IS NOT NULL` |
| 7 | `shipping_service_option_translations` | `ix_shipping_service_option_translations_option_lang` | `(shipping_service_option_id, language_id)` unique |
| 8 | `shipping_service_options` | `ix_shipping_service_options_service_code` | `(shipping_service_id, code)` unique |
| 9 | `shipping_service_options` | `ix_shipping_service_options_service_id` | `(shipping_service_id)` non-unique |
| 10 | `shipping_service_translations` | `ix_shipping_service_translations_service_lang` | `(shipping_service_id, language_id)` unique |

Each was re-verified against its authoritative raw-DDL origin (`20260910120000`, `20260910170000`,
`20261015120000`, `20261016120000`) and against the current model before editing; no shape mismatch was found, so no
`STOP` was required.

## 2. Metadata regeneration (the only metadata exception)

Regenerated with the **unchanged** existing factory/tooling (`dotnet ef` 9.0.16 / EF Core 8.0.11; no install or
upgrade) using a task-owned temporary scaffold as a metadata carrier, then deleted by exact path:

* `FulfillmentDbContextModelSnapshot.cs` — regenerated from the current model.
* `20261022182208_AddFulfillmentOutboxPendingIndex.Designer.cs` — only its `BuildTargetModel` body replaced, keeping
  `[DbContext(typeof(FulfillmentDbContext))]`, `[Migration("20261022182208_AddFulfillmentOutboxPendingIndex")]`,
  `partial class AddFulfillmentOutboxPendingIndex` and the file name.
* The scaffold's own `Up`/`Down` (RenameIndex/DropForeignKey operations) was **never executed or retained**; the
  scratch `.cs` + `.Designer.cs` were removed and no migration id/history row was created.

Diff classification of the snapshot (every added/removed line, `metadata-before-after.diff`): 9 `HasDatabaseName`
pairs, 1 `HasConstraintName` pair, `ToTable` gaining exactly the 2 check constraints — **nothing else**. No property,
type, nullability, default, value-generation, index-shape, delete-action or foreign-reference change.

Byte parity: `20261022182208_...cs` `C6600940CBEF7D07…` unchanged, `20261022101415_ShippingTypeCatalog.cs`
`979328491371CD0F…` unchanged, its `.Designer.cs` `50098BD09979F5F8…` unchanged, and all other migrations/designers
untouched. Historical designers are no longer forced to equal the evolving current model.

## 3. Tests

* `HistoricalObjectNameAndCheckMappingTests` (new, 3 tests): exact ten-object pinning with ordered columns/uniqueness/
  filter/FK principal+delete action; exact two-check SQL (whitespace-normalized only); and a **negative demonstration**
  that the same comparator reports a renamed index, a deleted object, a missing CHECK and a silently relaxed CHECK body
  — i.e. names and checks are not filtered out.
* `TypeCatalogModelMetadataParityTests` (updated, 3 tests): the "latest TargetModel equals current" assertion now selects
  the **actual latest** migration from the assembly (asserted to be `20261022182208_AddFulfillmentOutboxPendingIndex`)
  while keeping the real initializer/differ, zero-operation assertions, normalized semantics and the original
  type-specific identifiers/concurrency assertions. R10's own current/latest parity test remains strict.
* `HistoricalNameAlignmentResidualDiffTests` (new, executed proof).

## 4. Executed residual-diff proof (isolated)

Task-owned `postgres:17-alpine` from the local image, published only `127.0.0.1:55436`, role `tb196`, DBs
`fulfillment_tb196` and `fulfillment_tb196_model`; port verified free beforehand; both targets parsed with
`NpgsqlConnectionStringBuilder` through the existing fail-closed `ValidateTarget` (exact host/port/role/database) before
any connection, and live `current_database()` / `current_user` / `inet_server_port()` asserted per database.

| Side | How it was obtained |
| --- | --- |
| Primary | the **real unchanged migration chain** applied through R10 `20261022182208_AddFulfillmentOutboxPendingIndex` (never model-created) — 218 normalized objects |
| Model | the current canonical model projected with the provider's own `GenerateCreateScript()` in the separate model DB — 218 normalized objects |

Result: **residual = 0**, difference = exactly the **10 raw default lines** (the five known pairs). All twelve R11
objects (2 checks, 1 FK, 9 indexes) are present and identical on both sides, the R10 pending index is present on both
sides, and the primary manifest re-read after the projection is byte-equal (no DDL on the primary beyond its own
migration chain). Exclusion was by exact table identity only (`fulfillment.__ef_migrations_history`), not by broad
tokens.

`residual-default-lines.txt` (verbatim: 5 `missing-in-model` + 5 `unexpected-in-model` pairs for
`items.quantity_packed=0`, `items.quantity_processing=0`, `shipments.provider_metadata_version=0`,
`shipments.shipping_method_code=''`, `shipments.shipping_method_label=''`).

## 5. Defaults — explicitly not touched

No `HasDefaultValue`, no `ValueGeneratedOnAdd/Never`, no dropped default, no CLR initializer/insert-behavior/domain
validation/data change. The five defaults stay visible as differences; they are **not** accepted as permanent
asymmetry, and no whitelist, baseline refresh or full-schema gate relaxation was introduced. The blocked full-schema
collection was not used and its substantive/default/name gate was not modified.

## 6. Focused commands

```text
dotnet build Tooba.Fulfillment.Tests                                            -> 0 errors
dotnet test --filter HistoricalObjectNameAndCheckMappingTests|TypeCatalogModelMetadataParityTests
                                                                                -> Passed 6 / Failed 0
dotnet test --filter OutboxPendingIndexMigrationTests.New_migration_executes
             |OutboxPendingIndexMigrationTests.Snapshot_and_new_target_model
             |TypeMigrationDesignTimeMetadataTests                              -> Passed 5 / Failed 0
dotnet test --filter HistoricalNameAlignmentResidualDiffTests (executed proof)  -> Passed 1 / Failed 0
```

The R10 database-owning lifecycle test was **not** run in this task (offline facts only), so no competing migration
run touched the target. One bounded harness-only correction was needed (removed an unused model-differ assertion that
failed to compile on a missing `IDesignTimeModel` using); no behaviour was weakened and no test was skipped.

## 7. Isolation / cleanup

Container removed after the run; `127.0.0.1:55436` has no listener. Only the mapping file, the regenerated snapshot and
R10 designer, the three test files, this evidence directory, the received-task receipt and a recovery worker pointer
were written. No shared `:5432` / Redis / RabbitMQ / app port, no image or package install, no reset/stash/rebase/
force-push, unrelated work preserved.

## 8. Scope statement

This is model/metadata alignment only: no customer-facing or deployed-schema claim, no full TB-196 acceptance, no
production activation. `TB196-Acceptance: BLOCKED_PENDING_DEFAULTS_AND_RELATIONAL_ACCEPTANCE`,
`Production-Functional-Acceptance: PENDING`, `Stop-State: STOP_FOR_ARCHITECT`, `Automatic-Next-Task: NONE`.
