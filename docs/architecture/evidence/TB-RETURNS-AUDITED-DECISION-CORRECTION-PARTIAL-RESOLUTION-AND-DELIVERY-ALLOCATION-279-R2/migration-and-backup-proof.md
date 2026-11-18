# TB-279-R2 — MIGRATION AND BACKUP PROOF

## R2 adds no schema change

```
git show --name-only 3b1ce75d9 | Select-String -Pattern "Migrations"
  (no output)
```

The only schema change in the 279 chain is the R1 migration
`20261118023434_Tb279R1PerConditionPortionsAndCustodyProof`, applied ONCE by `Tooba.MigrationRunner` after a fresh
`pg_dump` (3 031 123 B, SHA256 `C025224B…EEADEE`) verified with `pg_restore --list` (exit 0, 1475 TOC lines), with
`pendingMigrations []` afterwards. R2 depends on those columns (condition portions, custody proof kind) and adds none.

## Why no migration was needed

Every R2 change is a code/config/contract change over existing columns:

- the reviewed versions are read from the existing decision/resolution rows;
- the five portions already exist as the per-condition columns added by R1;
- the custody proof kind uses the existing `proof_kind` column;
- the occupancy read uses the existing `return_delivery_allocations` rows;
- the wire additions are request/response members, not persisted columns.

## Verification performed

- a `dotnet build Tooba.slnx -c Release` over the changed model/endpoint/command surface succeeds with 0 errors, which
  also proves the EF model still matches the applied schema;
- no `EnsureCreated`, no ad-hoc SQL, no DDL statement was executed;
- no data was reset, reseeded, backfilled, cleaned or restored.

## Honest limits

- The migration plan was not re-listed against the live database in this task because no migration was touched; the last
  verified state is the R1 post-apply `pendingMigrations []`.
- The pre-apply backup from R1 remains the recovery point of record for this chain.
