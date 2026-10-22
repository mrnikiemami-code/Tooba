# TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2 — RESULT (worker)

Protocol: BRIDGE-WAKE-V1 · Channel: tooba-main · Worker: tooba-worker-01
Parent: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1
Mode: BOUNDED_AUTHORIZED_DRAFT_TRANSACTION_GUARD_AND_PROOF_HARDENING
Expected-Base-SHA: 260904c8ca5ae51c7a06af6103c1e2d215050d8f
Status: PASS (worker-measured; not Architect ACCEPT)

Architect decision implemented: OPTION 1 — keep the already authorized single Shipping-owned staged
replacement transaction and the immediate uniqueness constraints. No deferred-constraint migration was
created.

## 1. Guard correction (old assumption -> precise boundary)

`OperationalQuoteCompatibilityTests.The_operational_slice_adds_no_migration_and_no_money_movement`
previously asserted that the literal `BeginTransaction`/`DbTransaction` never appears anywhere in Shipping
and therefore rejected the authorized TB-190-R1 persistence boundary. It is replaced — not deleted — by
`ShippingTransactionBoundary` (`Compatibility/ShippingTransactionBoundary.cs`), a side-effect-free detector
over the same `OperationalBoundarySource` inputs, plus:

- `The_operational_slice_adds_no_migration_and_no_money_movement` — keeps the operational non-existence of
  the operational migration, and now asserts zero startup-migration authority and zero operational
  write/money authority across the whole Shipping module.
- `Exactly_one_reviewed_shipping_transaction_exists_and_it_is_the_staged_draft_replacement` — zero
  unexpected transaction sites, the reviewed boundary keeps exactly one begin/commit, at least two staged
  saves and a rollback path.
- Retained unchanged (equal or stronger coverage): approved-consumer set, Contracts-only observation pair,
  registration-only Order edge, default-OFF gate, capacity enum, legacy read seams, no success-shaped zero.

Rules the detector enforces:

| Rule | Meaning |
| --- | --- |
| `UNEXPECTED_SHIPPING_TRANSACTION` | a transaction start/type outside `ShippingAuthoringDraftDirectory::ReplaceRegionGroupsAsync` (or a transaction type outside the reviewed pair) |
| `SHIPPING_STARTUP_SCHEMA_AUTHORITY` | `AddModuleSchemaMigrator` / `MigrateAsync(` / `EnsureCreated(` anywhere in Shipping, **including the allowed file** |
| `SHIPPING_OPERATIONAL_WRITE_AUTHORITY` | capacity reservation / `PendingAllocation` / payment draft / money projection assignment inside Shipping |
| `AUTHORING_TRANSACTION_SHAPE` | the reviewed boundary no longer opens exactly one transaction |

No filename-only exemption: the allowed file is inspected line by line and its enclosing member is resolved
by a backward declaration scan, so the same file is not skipped and its other members are not excused.

Negative fixtures (all executed, all pass): unexpected transaction in another Shipping file; transaction in
another member of the allowed file; a second begin inside the reviewed member; operational money/capacity
leak; startup migration inside the allowed file; the reviewed boundary stripped of its transaction.
**Static-only limitation stated explicitly:** this is a source scanner. It is not a data-flow proof and it
does not claim runtime enforcement; the executed PostgreSQL facts are the behavioural half.

## 2. Repeatable PostgreSQL isolation

`ShippingRelationalTestTarget` parses with `NpgsqlConnectionStringBuilder` BEFORE any connection and compares
against test-owned constants `127.0.0.1` / `55432` / `shipping_tb190r1` / `tb190r1`. Multi-host lists, `5432`,
`tooba_alpha`, `store-alpha`, `postgres`, wrong roles and a missing configuration are all rejected offline
(`ShippingRelationalTargetIsolationTests`, 12 executed facts). Fail-closed: a missing/invalid configuration
throws; nothing is skipped and nothing reports success without a server.

Before the first write the suite proves the live target with `current_database()`/`current_user`, so the
parsed intent is verified against the server actually reached. The container binding was verified
operationally: `5432/tcp=>127.0.0.1:55432`, image `postgres:17-alpine`, task-only database and role.
Test XML that claimed missing configuration "skips" facts was corrected; the implementation throws.

## 3. Deterministic concurrency and cancellation

- `TwoReaderBarrierInterceptor` + `TwoReaderGate`: both participants arrive at their FIRST `SavingChanges`,
  i.e. after each read and prepared revision 1 and before either wrote. The interceptor records the
  concurrency token's ORIGINAL value, so `OriginalRevision == 1` on both sides is the exact predicate the
  later UPDATE uses. The gate is bounded (20s) and a bare `BEGIN` holds no row lock, so nobody waits holding
  a conflicting lock. Either participant may win; the winner/loser are derived from the actual results, the
  stored tree is compared to the WINNER's tree, and the ACTUAL losing context (`loser.Index`) is asserted to
  hold no pending change.
- `CancelOnSecondSaveInterceptor`: the caller token is cancelled at the start of the SECOND `SavingChanges`
  (after the staged write, before commit). `OperationCanceledException` propagates, a fresh-context readback
  keeps revision 1 and the previous full tree, no staged order survives, and a valid retry succeeds.
- `FailOnSecondSaveInterceptor` (injected failure after the staged write) is retained.
- Reorder, delete-shift, insert-before and selected-node move/identity are retained.

## 4. Measured commands and counts (this run)

```
dotnet build …/Tooba.Shipping.Tests.csproj                                        -> 0 errors
dotnet test --filter OperationalQuoteCompatibilityTests                           -> 19/19 pass
dotnet test --filter "ShippingAuthoringDraftRelationalTests|ShippingRelationalTargetIsolationTests"
                                                                                  -> 17/17 pass
dotnet test (whole Tooba.Shipping.Tests, isolated DB available)                    -> 558 pass / 0 fail / 0 skipped / 558
```

The full suite was executed while the isolated database was still running (before cleanup), so no fact was
skipped. TB-190-R1's historical 538/1/539 and its 4 relational results remain worker-reported history and are
not restated as this run's numbers.

## 5. Production change scope

One comment-only XML correction in `ShippingAuthoringDraftDirectory` replacing the stale "one SaveChanges /
no manual transaction" claim with the actual two-stage, single-transaction/single-commit relational strategy
and the single-save non-relational test path. No production executable C#, contract, endpoint, handler,
validator, DI, schema, migration, snapshot, flag or authority was changed. No production behaviour repair was
needed; no new runtime flaw was found.

## 6. Isolation, cleanup

Fresh task-owned container `tooba-shipping-tb190r2` (`postgres:17-alpine`) on `127.0.0.1:55432`, database
`shipping_tb190r1`, role `tb190r1`. Port 55432 was verified free before start and has zero listeners after
`docker rm -f`. Shared `:5432` / `tooba_alpha` / `store-alpha`, shared apps and shared ports untouched. No
credentials logged. Setup well inside the 8-minute bound; every command under 180 seconds.

## 7. Not claimed

Architect ACCEPT, product/editor completion, certification, activation, rate/editor behaviour, frontend
integration, relational proof of any other module, or a new architecture wave. `Stop-State:
STOP_FOR_ARCHITECT`; no automatic next task.
