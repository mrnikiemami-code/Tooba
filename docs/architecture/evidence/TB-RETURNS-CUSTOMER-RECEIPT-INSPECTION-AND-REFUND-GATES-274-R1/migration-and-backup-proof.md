# Migration, backup and apply proof — TB-274-R1 (B storage integrity + E)

## 1. The new additive migration

`returns` → **`20261116140705_BoundedReceiptIntegrityConstraints`** (`Migration` + `.Designer.cs` +
`ReturnsDbContextModelSnapshot.cs` updated consistently; the already-applied parent migration
`20261116122318_AddCustomerReceiptsAndDecisionReason` was **not** edited or re-applied).

What it adds, and nothing else:

| Object | Definition |
| --- | --- |
| `ck_customer_return_receipt_lines_quantity_positive` | `CHECK (quantity > 0)` on `returns.customer_return_receipt_lines` |
| `ck_customer_return_receipt_lines_condition_defined` | `CHECK (condition IN ('Usable','Damaged'))` on the same table |
| `ak_return_items_return_request_id_return_item_id` | unique `(return_request_id, return_item_id)` on `returns.return_items` |
| `ak_customer_return_receipts_return_request_id_return_receipt_id` | unique `(return_request_id, return_receipt_id)` on `returns.customer_return_receipts` |
| `ix_customer_return_receipt_lines_owning_receipt` / `_owning_item` | the two supporting indexes |
| `fk_customer_return_receipt_lines_receipts_owning_case` | composite FK `(return_request_id, return_receipt_id)` → receipts `(return_request_id, return_receipt_id)` `ON DELETE CASCADE` |
| `fk_customer_return_receipt_lines_return_items_owning_case` | composite FK `(return_request_id, return_item_id)` → items `(return_request_id, return_item_id)` `ON DELETE RESTRICT` |

The two previous single-id FKs (`fk_customer_return_receipt_lines_receipts_return_receipt_id`,
`…_return_items_return_item_id`) are dropped by the same migration and replaced by the composite pair, so a line can no
longer store "header case A + line/item case B". No cross-module FK, no trigger, no new table, no lifecycle change, no
data repair/backfill/status rewrite. The cumulative upper bound stays in the serialized owning code by design — a
cross-row bound is not expressible as a single-row CHECK and was not faked.

Verified on the live store after apply (`docker exec … psql \d returns.customer_return_receipt_lines`):

```
Check constraints:
    "ck_customer_return_receipt_lines_condition_defined" CHECK (condition::text = ANY (ARRAY['Usable'::character varying, 'Damaged'::character varying]::text[]))
    "ck_customer_return_receipt_lines_quantity_positive" CHECK (quantity > 0::numeric)
Indexes:
    "ix_customer_return_receipt_lines_owning_item" btree (return_request_id, return_item_id)
    "ix_customer_return_receipt_lines_owning_receipt" btree (return_request_id, return_receipt_id)
Foreign-key constraints:
    "fk_customer_return_receipt_lines_receipts_owning_case" FOREIGN KEY (return_request_id, return_receipt_id) REFERENCES returns.customer_return_receipts(return_request_id, return_receipt_id) ON DELETE CASCADE
    "fk_customer_return_receipt_lines_return_items_owning_case" FOREIGN KEY (return_request_id, return_item_id) REFERENCES returns.return_items(return_request_id, return_item_id) ON DELETE RESTRICT
```

`dotnet ef migrations has-pending-model-changes` on the Returns project → **"No changes have been made to the model
since the last migration."** (model, designer and snapshot agree).

## 2. Ordered chronology (local time, +03:30) — real commands, real exits

| # | Step | Evidence |
| --- | --- | --- |
| 1 | Canonical target + all-module plan: `SingleStore` / `store-alpha` / `tooba_alpha` / `tenant-alpha`, Returns `current=20261116122318_AddCustomerReceiptsAndDecisionReason`, `pendingCount=1` (only this task's migration) | `D:\temp\tb274r1-backup\plan-pre-apply.log` (17:39:24, `result=ok`) |
| 2 | Read-only data compatibility: both receipt tables carry **0 rows**; no receipt has ever been recorded in the canonical database (no live return case exists), so no stored row could violate the new CHECKs | current store read; parent 274 apply records |
| 3 | **Fresh pre-apply `pg_dump` #1** — 2,979,578 B, SHA256 `4D3060A62DD66047F33841F97D9773DB960E3BEE08F7273CDDFEC0B0046D87BC`, `pg_dump` exit 0 | `backup.log` (17:40:14) |
| 4 | Readable-archive verification of #1 — `pg_restore --list` exit 0, 1445 TOC lines, 0 stderr bytes | `verify.log` (17:40:29) |
| 5 | **Apply attempt #1 → FAILED** `42703: column "ReturnRequestId" named in key does not exist` (the first generated key columns were unmapped PascalCase). EF's migration transaction rolled the whole attempt back: the next dump contains **none** of the new objects and still shows the old single-column FKs | `apply.log` (17:41:02, `result=failed`) |
| 6 | Migration/snapshot/designer corrected to the mapped snake_case columns | code in this change |
| 7 | **Fresh pre-apply `pg_dump` #2** (the actual pre-apply backup of the applied change) — 2,979,578 B, SHA256 `31D47A05360045904846661C09F064EA11381D52C8F713FC2BA261B589527A52`, `pg_dump` exit 0 | `backup2.log` (17:44:10) |
| 8 | Readable-archive verification of #2 — `pg_restore --list` exit 0, 1445 TOC lines, 0 stderr bytes, same instant | `backup2.log` (17:44:11) |
| 9 | **Apply #2 by `Tooba.MigrationRunner`** (repository runner, store-alpha only, no manual DDL) — Returns `succeeded=true`, `currentMigration=20261116140705_BoundedReceiptIntegrityConstraints`, `pendingMigrations=[]`, `durationMs=128`; every other module `pendingMigrations=[]` | `apply2.log` (completed 17:44:21, `apply2-exit=0`) |
| 10 | Post-apply plan — Returns `current=20261116140705…`, `pendingCount=0`, `result=ok` | `plan-post-apply.log` (17:44:53) |
| 11 | Post-apply facts — `returns.__ef_migrations_history` newest row `20261116140705_BoundedReceiptIntegrityConstraints`; `customer_return_receipts` = 0 rows, `customer_return_receipt_lines` = 0 rows, `return_request_decisions` = 0 rows; constraint set as printed in §1 | current read |

Backups are private artifacts under `D:\temp` and are **not** committed or uploaded. No restore, no `Down`, no reapply,
no manual DDL, no second database/container, no seed/backfill/cleanup/delete.

## 3. Original TB-274 archive — verified NOW, labelled honestly

The parent 274 receipt claimed `D:\temp\tb274-backup\tooba_alpha-pre-tb274.dump`, 2,966,890 B, SHA256
`ADCE728BD0D5732C09F5EECDC219287859CB7FE56173F9F5BD07A10BDC64CD16`, but never showed a readable-archive check. The
archive was verified **at 2026-11-16T18:33:02+03:30** (current verification, *not* a retroactive pre-apply receipt):

```
size        = 2,966,890 B      (unchanged)
sha256      = ADCE728BD0D5732C09F5EECDC219287859CB7FE56173F9F5BD07A10BDC64CD16   (unchanged)
pg_restore --list : exit 0, 1431 TOC lines, 0 stderr bytes
```

Its creation stamp (`11/16/2026 4:13:46 PM`) still precedes the parent 274 apply, and the archive is readable. Nothing
was restored and nothing was written back.

## 4. Safety properties of the applied change

- Additive only: two CHECKs, two alternate keys, two indexes, and a composite-FK swap on one Returns table.
- Idempotent at the runner level (migration history), applied exactly once (`pendingMigrations: []` afterwards).
- No business row was created, updated or deleted by the migration; both receipt tables and the decision table are
  still empty.
- The migration does not depend on any business data value, so no data repair or compat shim was needed.

## 5. Limits

- The private dumps cannot be independently inspected by the Architect; this proof records the exact paths, sizes,
  hashes, exit codes, TOC line counts and timestamps so the claims are falsifiable against the same machine.
- The first apply attempt (§2 row 5) is disclosed as a real failed attempt; the successful apply is the one recorded in
  the migration history. The first dump (#1) is retained and is *not* the pre-apply backup of the applied change —
  dump #2 is.
