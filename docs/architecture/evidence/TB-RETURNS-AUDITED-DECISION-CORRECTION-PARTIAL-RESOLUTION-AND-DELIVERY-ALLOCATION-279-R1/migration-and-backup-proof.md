# TB-279-R1 — migration and backup proof

One minimal additive Returns-owned migration was authorized and used. No foreign schema, no drop, no rename of a
shipped fact, no data backfill/repair/reset/seed, and the already-applied 279 migrations were not edited.

## Migration

```
20261118023434_Tb279R1PerConditionPortionsAndCustodyProof
```

Applies only to the `returns` schema:

* `return_item_resolutions`: the reviewed per-condition split — `AcceptedUsableQuantity`, `AcceptedDamagedQuantity`,
  `RejectedReceivedUsableQuantity`, `RejectedReceivedDamagedQuantity`, `RejectedUnreceivedQuantity` (nullable so a
  pre-R1 row reads honestly as an incomplete plan), one row per `(return_request_id, return_item_id, version)` under the
  append-only plan, with the FK back to the owning case/item.
* `return_item_custody`: one actual handback fact per `(case, item, condition, occurrence key)` with its proof
  reference, server-derived recipient, quantity check `> 0` and the local composite ownership FKs used by the existing
  receipt model.

Model snapshot and configuration were regenerated; `has-pending-model-changes` was checked consistent before apply.

## Pre-apply backup (REQUIRED, verified BEFORE apply)

```
pg_dump  D:\temp\tb279r1-backup\tooba_alpha-pre-tb279r1.dump
         bytes   = 3 031 123
         sha256  = C025224B6C0BDC9F06F3A180AF02109DEF41915E1F07BB70D3EFEFBB17DEADEE
         created = 2026-11-18 06:44:11
pg_restore --list <dump> > restore-list.txt
         exit    = 0   (restore-list.err empty, 1475 TOC lines)
```

Runner plan contained only this task's intended Returns migration.

## Apply

`Tooba.MigrationRunner` was run ONCE from the publish directory; `apply.log` records `succeeded=true` and
`pendingMigrations: []` for every module (e.g. Shipping `currentMigration 20261112120000_AddProcessingHoursAndTimingPolicy`,
`pendingMigrations []`, `durationMs 139`). No manual DDL, no Down, no re-apply.

Post-apply `__ef_migrations_history` (descending):

```
20261118023434_Tb279R1PerConditionPortionsAndCustodyProof
20261118004500_NormalizeApprovedAmountColumn
20261117164135_Tb279AuditedDecisionCorrectionResolutionCustodyAndAllocation
```

## Post-apply data state

```
decision_revisions=0  item_resolutions=0  custody=0  delivery_allocations=0
return_requests=3 (all Completed, approved_amount NULL, original updated_at preserved)
```

No failed apply attempt occurred in this task, so no second backup was needed.
