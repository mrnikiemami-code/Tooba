# TB-274 — migration, pre-apply backup and canonical-target proof

## 1. Authorized schema change

| Item | Value |
| --- | --- |
| Migration id | `20261116122318_AddCustomerReceiptsAndDecisionReason` |
| Module | Returns only (`returns` schema) |
| Up | add nullable `reason varchar(512)` to `returns.return_request_decisions`; create `returns.customer_return_receipts` and `returns.customer_return_receipt_lines` |
| Down | drop the two new tables and the column (never executed) |
| Data change | none — no status rewrite, no backfill, no seed, no business row touched |
| Model metadata | `ReturnsDbContext.cs` mappings + `ReturnsDbContextModelSnapshot.cs` |

Keys and links actually created (read-only verified on the canonical database):

- `pk_customer_return_receipts`, `pk_customer_return_receipt_lines`;
- `fk_customer_return_receipts_return_requests_return_request_id` (cascade — an orphan receipt body is impossible);
- `fk_customer_return_receipt_lines_receipts_return_receipt_id` (cascade);
- `fk_customer_return_receipt_lines_return_items_return_item_id` (restrict — the requested line is the owning identity);
- `ix_customer_return_receipts_return_request_id_occurrence_key` **unique** (one durable physical occurrence);
- `ix_customer_return_receipt_lines_receipt_item_condition` **unique** (one (item, condition) portion per occurrence);
- supporting `ix_customer_return_receipts_return_request_id`,
  `ix_customer_return_receipt_lines_return_item_id`, `ix_customer_return_receipt_lines_return_request_id`;
- quantities use the existing `numeric(18,6)` precision; the condition column is `varchar(32)`.

Declared limit: positive quantity and defined condition are enforced by the owning aggregate
(`ReturnReceipt.Create`) and the persisted six-decimal rule, not by a database `CHECK` constraint. The database enforces
PK/FK/uniqueness.

## 2. Pre-apply backup (real, verified BEFORE the apply)

| Item | Value |
| --- | --- |
| Path | `D:\temp\tb274-backup\tooba_alpha-pre-tb274.dump` (private, outside git) |
| Size | 2,966,890 bytes |
| SHA256 | `ADCE728BD0D5732C09F5EECDC219287859CB7FE56173F9F5BD07A10BDC64CD16` |
| Taken | 2026-11-16 16:13:46 local |
| Format | `pg_dump` custom format of the canonical `tooba_alpha` |

## 3. Target and pending-set verification

- ONE canonical database: `tooba_alpha` at `127.0.0.1:5432`, edition `SingleStore`, store scope `store-alpha`.
- `D:\temp\tb274-backup\plan-pre-apply.json` (52,103 bytes) is the all-module runner plan inspected before the apply; the
  only pending migration in it was this task's additive Returns migration. No unexpected pending migration existed.

## 4. Apply (once, owning runner only)

`D:\temp\tb274-backup\apply.log`:

```
"pendingMigrations": [],
"succeeded": true,
"durationMs": 122
```

Applied exclusively through the repository `Tooba.MigrationRunner` for `store-alpha`. No manual DDL, no psql write, no
reset/reseed/restore, no second or disposable database, no docker/port/edition change.

## 5. Post-apply read-only verification

| Check | Result |
| --- | --- |
| `returns.__ef_migrations_history` newest row | `20261116122318_AddCustomerReceiptsAndDecisionReason` (7 total) |
| `returns.customer_return_receipts` rows | `0` |
| `returns.customer_return_receipt_lines` rows | `0` |
| `returns.return_request_decisions.reason` | `character varying(512)`, nullable, present |
| Constraints / indexes | listed in §1, all present |

No demo case, order, reservation, hold, stock or money row was created, and no rollback-only test left a committed row
(every relational fact prints `committedRows=0`).

## 6. Restore note

Restore was never executed. If it were ever required, the documented command shape is:

```
pg_restore --clean --if-exists --no-owner -U <user> -d tooba_alpha D:\temp\tb274-backup\tooba_alpha-pre-tb274.dump
```

It is recorded for completeness only; this task neither restored nor re-applied anything.
