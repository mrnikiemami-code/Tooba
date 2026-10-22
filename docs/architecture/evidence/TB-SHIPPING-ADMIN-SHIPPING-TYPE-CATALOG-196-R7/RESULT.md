# RESULT — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6
Channel: tooba-main
Worker-Id: tooba-worker-01
Mode: BOUNDED_TYPE_MIGRATION_SNAKE_CASE_FACTORY_AND_UPDOWN_REPAIR
Expected-Base-SHA: 917677eb57987d95362eac484827acdf1ace4f83
Base-SHA: 917677eb57987d95362eac484827acdf1ace4f83
Verdict: **PASS** for the bounded repair slice. TB-196 remains
`BLOCKED_PENDING_LEGACY_SNAPSHOT_AND_RELATIONAL_ACCEPTANCE`.

## 1. Design-time factory now reuses the canonical module configuration

`FulfillmentDbContextFactory` no longer calls bare `UseNpgsql`. It calls the existing canonical
`ToobaNpgsql.ConfigureModuleContext(...)` (snake_case convention + module history table + migrations assembly)
with a design-time placeholder connection string; no connection is opened and no tenant/shared credential is
resolved. No global `ToobaNpgsql`/Persistence change, no Host/startup change.

Type-surface parity (design-time factory model versus canonical runtime-configured model) for the two type
entities only — table/schema, every column name+store type+nullability, PK name+columns, FK name+columns+target,
index names/columns/uniqueness, CHECK names+SQL — is asserted equal and executed green
(`TypeMigrationDesignTimeMetadataTests.Design_time_factory_matches_canonical_runtime_type_metadata`). Recorded
surface: `canonical-type-surface.txt`, canonical DDL: `canonical-type-ddl.sql`.

## 2. Only the latest unaccepted migration was corrected

`20261022101415_ShippingTypeCatalog` (`Up`/`Down`) now uses the actual canonical identifiers — columns
`shipping_type_id, code, sort_order, is_active, revision, created_at, updated_at` and
`shipping_type_translation_id, shipping_type_id, language_id, name, description`; PK `pk_shipping_types` /
`pk_shipping_type_translations`; FK
`fk_shipping_type_translations_shipping_types_shipping_type_id`; index columns `code`, `(is_active, sort_order)`,
`(shipping_type_id, language_id)`. Owned business-index names and CHECK identities/semantics are preserved:
`ix_shipping_types_code`, `ix_shipping_types_active_sort`, `ix_shipping_type_translations_type_language`,
`ck_shipping_types_code_canonical`, `ck_shipping_types_sort_order_range`, `ck_shipping_types_revision_positive`,
`ck_shipping_type_translations_name_nonblank`, `ck_shipping_type_translations_description_nonblank`.
`Down` drops only the two new tables in dependency order. No older migration/designer was touched and no legacy
`CREATE/ALTER/DROP/rename/index/default/constraint/data` operation was added (see `latest-migration-diff.txt`).

The two type entity blocks in `.Designer` and the current `FulfillmentDbContextModelSnapshot` now carry the
explicit canonical column names plus PK/FK constraint names; every unrelated snapshot block is unchanged
(`snapshot-type-block-diff.txt` contains only the two type entity blocks and their relationship block).

## 3. R6 full-schema gate is fail-fast and its Down bookkeeping is honest

`TypeCatalogPostgresSchemaFixture` now computes
`EvaluateFullSchemaPrerequisite(snapshotEqualsCurrentModel, modelDifferences, substantiveLegacyDifferences)` and
throws a concrete diagnostic **before** any model projection or latest Up when equivalence is unproved, and again
before latest Up when substantive legacy discrepancies exist. Unknown is never converted into equal. Teardown
records `DownExecuted = true` only when the latest history row actually existed and a real Down ran; otherwise it
reports cleanup/no-op.

Executed proof on the disposable target (`r6-failfast/`):

```text
snapshot-vs-current-model differences=191
STOP: snapshot-versus-current-model equivalence is UNPROVEN (191 model differences: ...).
     No model projection, latest Up or Down parity may be claimed from an unproved prerequisite.
latest Up applied=False; Down NOT executed (cleanup/no-op)
type tables after stop = 0
```

Focused no-DB negatives (`TypeCatalogFullSchemaGateTests`): unproved equivalence stops, substantive legacy
discrepancies stop, green prerequisites do not stop.

**R6 process correction recorded:** proceeding to latest Up after a known Gate-1 failure was a process
deviation; the no-op teardown equality was not Down proof; and the 209 (now 191) differ operations were not
established as an invalid comparison merely by prose — they remain an explicit unproven prerequisite.

## 4. Isolated latest-type Up/Down proof (independent of the blocked whole-schema gate)

Fresh disposable `postgres:17-alpine` (`tb196r7-pg`), published only `127.0.0.1:55436` (`port-binding.txt`), role
`tb196`, database `fulfillment_tb196`; parsed with the same fail-closed validation (exact loopback/port/role/db,
multi-host rejected) and live `current_database()`/`current_user` asserted before DDL. No shared
`:5432`/`tooba_alpha`/`store-alpha`/Host/MigrationRunner/Redis/RabbitMQ target was used, and no package install.

Sequence actually executed in one test (`TypeCatalogIsolatedMigrationTests`, passed 1/1):

1. apply the ACTUAL historical chain up to `20261016120000_AddFulfillmentShippingGroups` and capture the
   normalized `pg_catalog` legacy manifest (`isolated-legacy-before.txt`);
2. apply the corrected latest migration once; history row asserted present; manifest diff shows **only** the two
   new owned tables' objects (`isolated-type-objects-after-up.txt`) and the legacy manifest is unchanged;
3. assert zero non-lowercase column names in the two tables (no quoted PascalCase survivor) and the canonical
   PK/FK/index identities above;
4. fresh-context own-store smoke: create `smoke_type` at sort 0 → inactive, revision 1, with a real Persian
   translation; a NEW Npgsql context reads the exact id/code/sort/active/revision and both text values
   (ORM column compatibility only — not storage/race acceptance);
5. execute the real latest Down: history row removed, both type tables absent, and the legacy manifest equals the
   before baseline exactly (`isolated-legacy-after-down.txt`, byte-equal to `isolated-legacy-before.txt`).

## 5. Commands and counts

```text
dotnet build Tooba.Fulfillment.Tests -v q --nologo                                  -> 0 errors
dotnet test --filter TypeMigrationDesignTimeMetadataTests|TypeCatalogFullSchemaGateTests -> Passed 6, Failed 0
dotnet test --filter TypeCatalogIsolatedMigrationTests                              -> Passed 1, Failed 0
dotnet test --filter Historical_legacy_schema_matches_the_regenerated_snapshot_projection (R6 fail-fast)
                                                                                     -> Failed 1 with the STOP diagnostic
```

No whole R6 collection re-run, no storage/race battery, no full module/solution/Host/Architecture suite.

## 6. Scope and hygiene

Changed production files: the design-time factory section of `FulfillmentDbContext.cs`, the latest type
migration, its `.Designer`, and only the two type entity/relationship blocks of the current snapshot. Test-only:
`TypeCatalogPostgresFixture.cs` (fail-fast + honest Down bookkeeping), plus three new focused test files. No
Domain/Contracts/Endpoints/DI/Compose/Host/frontend/Shipping/manifest changes, no legacy repair, no baseline or
expected-rule change, no data/backfill/seed, no shared DDL. TB-185 stays paused; default-OFF/PendingAllocation
untouched.

Cleanup: container stopped and removed; port 55436 has no listener (only kernel TIME_WAIT entries) and no
`tb196*` container remains. Evidence contains no credentials or customer payloads.

Remaining (unchanged by this task): the 3 substantive legacy discrepancies and the unproved snapshot/model
equivalence from R6, and the whole storage/constraint/race/atomicity battery. TB-196 acceptance stays
`BLOCKED_PENDING_LEGACY_SNAPSHOT_AND_RELATIONAL_ACCEPTANCE`. Stop-State: STOP_FOR_ARCHITECT, no automatic next
task.
