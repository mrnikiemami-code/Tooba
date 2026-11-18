# TB-279 — Migration and pre-apply backup proof

Two additive migrations were authored in 279 and each was applied exactly once by the owning runner, after a fresh,
verified private dump. No manual DDL, no `Down`, no restore, no reset, no reseed and no backfill was executed at any point.

## 1. Migrations

| # | Migration id | What it adds |
| --- | --- | --- |
| 1 | `20261117164135_Tb279AuditedDecisionCorrectionResolutionCustodyAndAllocation` | `returns.return_request_decision_revisions`, `returns.return_item_resolutions`, `returns.return_item_custody`, `returns.return_delivery_allocations`, and `returns.return_requests.approved_amount` |
| 2 | `20261118004500_NormalizeApprovedAmountColumn` | renames the column created by #1 to the EF-mapped snake_case name `approved_amount` |

Migration #2 exists because of a real in-task defect: #1 emitted `"ApprovedAmount"` (quoted PascalCase) while the model
maps `approved_amount`, so the live detail read failed with `42703`. The fix is an additive rename migration — the
originally applied migration was not edited or re-run.

Current history (read from the live database, newest first):

```
MIG 20261118004500_NormalizeApprovedAmountColumn
MIG 20261117164135_Tb279AuditedDecisionCorrectionResolutionCustodyAndAllocation
MIG 20261116140705_BoundedReceiptIntegrityConstraints          (274-R1, untouched)
MIG 20261116122318_AddCustomerReceiptsAndDecisionReason        (274, untouched)
```

## 2. Pre-apply backups (verified BEFORE each apply)

Both dumps live outside the repository, in `D:\temp\tb279-backup`, and both were verified with
`pg_restore --list` (exit 0, non-empty TOC, empty stderr) **before** the corresponding apply.

| Applies to | File | Bytes | SHA256 | Dump time (UTC) |
| --- | --- | --- | --- | --- |
| #1 | `tooba_alpha-pre-tb279.dump` | 3 011 929 | `F33032033DA575DFD5B7C54EA2B26EAF579B134881BF9E69769C6C806361DE05` | 2026-11-17T16:43:03Z |
| #2 | `tooba_alpha-pre-tb279fix-20261118041935.dump` | 3 028 309 | `B2C3CA9202580D22DA0FAF9ABBAEA7011B39BCB04688B2F2D5E376422EC31149` | 2026-11-18T00:49:35Z |

(The hashes and sizes above were re-computed from disk while writing this record, so they describe the files as they
currently exist — they are a current verification, not a retroactive receipt.)

## 3. Apply receipts

| Step | Trace | Outcome |
| --- | --- | --- |
| plan #1 (20:16:58 +03:30) | `df7203f4…` | `returns current=20261116140705_BoundedReceiptIntegrityConstraints pendingCount=1`; every other module 0 |
| apply #1 attempt 1 (20:17) | `18d725da…` | **failed** `42703: column "ReturnRequestId" referenced in foreign key constraint does not exist` — constraint-ordering defect in the generated migration; nothing to repair manually |
| apply #1 attempts 2–3 (20:18, 20:20) | — | same `42703` family while the migration source was being corrected |
| apply #1 (20:22:46) | `3d0e00a1…` | `pendingCount=0 result=ok`, four tables created, `approved_amount` present |
| plan #2 (04:19:09) | `839c8ffd…` | after the naming defect was discovered: `returns pendingCount=0` at that moment |
| backup #2 verify (04:19:35) | — | `pg_restore --list` exit 0, listing written, stderr empty |
| plan #2 re-check (04:49:07) | `1c1320ad…` | `returns pendingCount=1` (the naming fix) |
| apply #2 (04:49:50) | `0eb4b83d…` | `pendingCount=0 result=ok`, `current=20261118004500_NormalizeApprovedAmountColumn` |

The failed attempts are declared, not hidden. They never left a half-migrated schema: the next plan reported the same
single pending migration and the same `current`, and after the successful applies every new table existed with 0 rows.

## 4. Post-apply schema verification (live database, this session)

```
TABLES   return_delivery_allocations, return_item_custody, return_item_resolutions,
         return_request_decision_revisions   (all present, all 0 rows)
COLUMN   returns.return_requests.approved_amount  numeric(18,4)  nullable=YES
DATA     receipts=3 attempts=3 decisions=3 revisions=0 resolutions=0 custody=0 allocations=0
CASES    01a1e6b0-…  Completed refund=2407839.0000 approved=NULL updated=2026-11-17 00:31:26.384627+00
         01a1e8fc-…  Completed refund=2407839.0000 approved=NULL updated=2026-11-17 13:21:05.594614+00
         01a1e9c5-…  Completed refund=2407839.0000 approved=NULL updated=2026-11-17 14:03:06.669021+00
```

All three stored cases carry their original `updated_at`, a NULL `approved_amount` (no 279 resolution was ever
published on a real case) and their original `refund_amount`, so the migration is additive in practice, not only in
declaration.

## 5. Limits

- The apply receipts are the runner's own JSON logs kept under `D:\temp\tb279-backup` (scratch, untracked) plus the
  history rows above; the Architect can re-plan at any time and should see `pendingCount=0`.
- No restore was ever exercised, so the dumps are verified-by-listing, not verified-by-restore.
- The relational/`SkippableFact` harness was not extended in 279, so the migration's constraint behaviour is exercised
  by the live apply and the 274-R1 relational facts, not by a new committed relational test.
