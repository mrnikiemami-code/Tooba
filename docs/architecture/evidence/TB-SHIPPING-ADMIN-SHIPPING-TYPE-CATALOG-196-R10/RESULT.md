# TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R10 — RESULT

Status: **PASS — one additive pending-outbox index migration + isolated executed proof**.
Whole TB-196 remains `BLOCKED_PENDING_HISTORICAL_SCHEMA_AND_RELATIONAL_ACCEPTANCE`; the CHECK / default / naming
findings and the storage/relational battery stay open.

| Item | Value |
| --- | --- |
| Mode | `BOUNDED_FULFILLMENT_PENDING_OUTBOX_INDEX_MIGRATION_AND_ISOLATED_PROOF` |
| Expected base | `f3021742944786c2bd5316fb9ba21f2ab5acaf98` |
| Actual base | `f3021742944786c2bd5316fb9ba21f2ab5acaf98` (fast-forward from `49f5b8721`, `HEAD == origin/main` before edits) |
| Claim-UTC | 2026-10-22T18:21:12Z |

## 1. Architect decision applied

Only the approved shape: schema `fulfillment`, table `outbox_messages`, name `ix_outbox_messages_pending`,
non-unique btree on `occurred_at`, predicate `processed_at IS NULL AND dead_lettered_at IS NULL`.
`migrationBuilder.CreateIndex(...)` (transactional, **no** `IF NOT EXISTS`, **no** `CONCURRENTLY`, no
`suppressTransaction`). `Down` = `DropIndex` of exactly that index. Nothing else.
R9's `IF NOT EXISTS`/`Down IF EXISTS` suggestion is superseded and was **not** implemented.

## 2. Production change (one migration)

`20261022182208_AddFulfillmentOutboxPendingIndex` (+ its `.Designer.cs`) under
`Tooba.Fulfillment.Infrastructure/Persistence/Migrations`.

* The `.cs` was scaffolded with the existing `dotnet ef` tool (9.0.16 driving EF Core `8.0.11`) against the unchanged
  canonical model, then its (necessarily empty) `Up`/`Down` bodies were replaced with the approved `CreateIndex` /
  `DropIndex`. The generated `TargetModel` was kept as generated — no hand-written metadata.
* Generated forward SQL (evidence `forward.sql`):
  `CREATE INDEX ix_outbox_messages_pending ON fulfillment.outbox_messages (occurred_at) WHERE processed_at IS NULL AND dead_lettered_at IS NULL;`
  plus the migration-history insert.
* Generated reverse SQL (evidence `down.sql`): history-row delete + `DROP INDEX ix_outbox_messages_pending;`.

Byte parity of everything else (SHA-256 prefix before == after):

| Artifact | Hash |
| --- | --- |
| `FulfillmentDbContextModelSnapshot.cs` | `2930301A6470186A…` unchanged (no snapshot edit, no scaffold residue) |
| `FulfillmentDbContext.cs` (context + factory) | `3C3C459259805E57…` unchanged |
| `20261022101415_ShippingTypeCatalog.cs` | `979328491371CD0F…` unchanged |
| `20261022101415_ShippingTypeCatalog.Designer.cs` | `50098BD09979F5F8…` unchanged |
| All 11 older migrations/designers, `OutboxMessageMapping`, shared persistence | untouched (`git status` shows only the new migration pair) |

## 3. Offline proof

* Operation universe: `UpOperations` is exactly one `CreateIndexOperation` (name/schema/table/non-unique/`occurred_at`/
  filter) and `DownOperations` exactly one `DropIndexOperation`; the generated SQL contains no `IF NOT EXISTS` and no
  `CONCURRENTLY`.
* `IMigrationsModelDiffer` on initialized relational models: checked-in snapshot vs canonical `IDesignTimeModel` = **0**
  operations; the NEW latest `TargetModel` vs the same canonical model = **0** operations. R8's type-target proof is
  unchanged and still green.

## 4. Executed proof (disposable target only)

Target: task-owned `postgres:17-alpine` container (existing local image), published **only** `127.0.0.1:55436`, role
`tb196`, db `fulfillment_tb196`. Port was verified free before creation. The connection string was parsed with
`NpgsqlConnectionStringBuilder` (via the existing fail-closed `ValidateTarget`) **before** any connection; multi-host /
`localhost` / wrong-port / wrong-role / wrong-db all reject. Live provenance asserted: `current_database()=fulfillment_tb196`,
`current_user=tb196`, `inet_server_port()=5432` (host binding is the published 55436). No credentials in evidence.

Baseline: the **real existing migration chain through `20261022101415_ShippingTypeCatalog`** was applied (not a
model-created database), then three synthetic outbox rows (pending / processed / dead-lettered) were seeded. No
dispatcher ran. Evidence `manifest-baseline.txt` = 221 normalized `pg_catalog` objects.

| Step | Executed result |
| --- | --- |
| Up | index created; `indisunique=false`, `indisvalid=true`, `indisready=true`, columns `occurred_at`, predicate `((processed_at IS NULL) AND (dead_lettered_at IS NULL))`; exactly **1** history row |
| Manifest delta after Up | **exactly 1** line = that index definition; every other object byte-identical |
| Row preservation | complete row values (including `md5(payload)`, all nullable columns and all three states) and `count(*)` identical before/after |
| Genuine Down | new history row absent, only that index removed, manifest byte-identical to `manifest-baseline.txt`, rows identical (`manifest-after-down.txt`) |
| Reapply Up | one index, one history row |
| Migrate to the same target again | no schema and no data change (history gating; no `IF NOT EXISTS` needed) |
| Down again, then preexisting **wrong-shape** same-name index | migration failed `42P07 relation "ix_outbox_messages_pending" already exists`, no history row, row state unchanged, object preserved with its original definition — no adoption, drop or rebuild |
| Same, with **exact-shape** same-name index | identical fail-closed behaviour (`42P07`, no history row, object preserved) |
| Cleanup | the task-owned conflicting object was dropped by its exact name/table identity; baseline manifest and rows restored |

`Down` was never invoked on an unapplied migration to delete a preexisting index; no error-mapper or shared
reconciliation change.

## 5. Focused commands / counts

```text
dotnet build Tooba.Fulfillment.Tests                       -> 0 errors
dotnet test --filter OutboxPendingIndexMigrationTests      -> Passed 3 / Failed 0
dotnet test --filter TypeCatalogModelMetadataParityTests|TypeMigrationDesignTimeMetadataTests
                                                           -> Passed 6 / Failed 0
```

No full module/Shipping/Host/solution build, no broad suite, no storage/unique/revision-race battery.
The blocked whole-schema fixture was **not** used as a prerequisite and was not edited, filtered or suppressed.

## 6. Remaining gates (truthful)

CHECK/default/naming reconciliation, snapshot-vs-historical-schema equality and the relational/storage battery remain
`PENDING_BLOCKED`; index-only acceptance does not close TB-196. Index creation is transactional and can block writes on
a large live `outbox_messages`; size/window are unknown and **no production deployment is authorized**. If any
environment already has this index out-of-band, that needs a separately reviewed adoption/deployment plan — the
migration deliberately fails closed (`42P07`) instead of silently adopting it (proved above with both a wrong-shape and
an exact-shape object).

## 7. Isolation / cleanup

Disposable container `tb196r10` removed after the run; `127.0.0.1:55436` has no listener. No shared `:5432` /
`tooba_alpha` / `store-alpha` database, no Host/startup migrator. Only the two new migration files, the new test file,
this evidence directory, the received-task receipt and a recovery worker pointer were written. No reset/stash/rebase/
force-push; pre-existing untracked helpers untouched.
