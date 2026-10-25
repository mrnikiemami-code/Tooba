# TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214-R2 — RESULT

Mode: `BOUNDED_CAPTURE_OWN_ENTRY_CLEANUP_AND_ACTUAL_STALE_CONTEXT_PROOF`
Parent: `TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214-R1`
Base-SHA: `a130f0e295f3d31ca691cc7f697509721f710a81`
Verdict: `READY_FOR_ARCHITECT_REVIEW` (STOP_FOR_ARCHITECT; no 215 envelope)

Scope: ONE existing member — `ShippingAuthoringDraftDirectory.CaptureRevisionAsync` — plus its storage
facts. No new table, no schema/domain/reader/flag/consumer/publication change. Prior 214 table and 214-R1
domain fixes retained. No legacy flattening.

## 1. Failure-leak repair (source)

Two root-sourced defects from the 214-R1 review, repaired only inside this member:

1. **Pending failed insert.** `rollback(false)` preserved caller state but left `snapshotAdded` pending, so a
   later `SaveChanges` on the same context could publish the failed capture.
   Now every terminal path that did not commit detaches ONLY the capture's own attempted entry through
   `DetachOwnedSnapshot(snapshot)` (`context.Entry(snapshot).State = EntityState.Detached`). The caller's
   tracker is never `Clear`ed and the caller root/children are never detached.
2. **Leaked current transaction on the unique-recovery path.** The exact-23505 catch previously set
   `transaction = null` after rollback and then read the winner, potentially leaving a completed
   `CurrentTransaction`.
   Now `RollbackAsync` → `DisposeCaptureTransactionAsync` (own transaction) → `transaction = null` →
   detach own snapshot → **only then** the single winner recovery read. The `finally` disposes any
   remaining own transaction (tolerant/idempotent release).

Preserved behaviours: `SaveAuthoringReplacementAsync` semantics and callers untouched; the exact unique
identity filter (`23505` + `ux_authoring_revision_snapshots_owner_revision`) unchanged; all no-write
preflight rejections unchanged (clean-tracker refusal, not-found, revision conflict); capture still returns
actual provider-persisted facts; historical reader unchanged. Original exception identity and the caller's
`CancellationToken` identity are preserved by the plain rethrow. Cleanup uses `CancellationToken.None`
only because the failure may itself be that cancellation; the recovery read is ONE bounded lookup, not a
retry loop.

## 2. Two overclaimed facts replaced with actual behaviour

- `Stale_tracked_copies_never_leak_into_a_capture_and_dirty_work_is_never_flushed`: the successful
  B-token capture now runs through the **actual stale context A** (the previously used `freshInA` was
  removed). It asserts A's stale tracked root and rate-group child keep the same object identities, the
  stale revision is not refreshed, the stale child still holds its two old rows, A's tracker stays clean,
  and the captured payload is B's FULL graph (revision, single 700 g row, 1 111.00 packaging).
  The stale-token capture through the same context is still the canonical conflict.
- `Two_real_contexts_capturing_the_same_revision_store_exactly_one_row`: both captures are now started with
  `Task.Run` and each signals the bounded gate INSIDE its task; the gate return is asserted and the arrival
  counter must equal exactly 2 before releasing. Two real contexts, one persisted row, same id, same time,
  same provider payload.

## 3. Added failure/cancellation/unique-recovery facts (test interceptors only, no production hooks)

- `A_failure_before_the_capture_insert_propagates_and_publishes_nothing_later`: sentinel thrown before the
  real capture save; the SAME sentinel instance propagates, `AuthoringRevisionSnapshots.Local` is empty,
  a later `SaveChanges` on that context returns **0**, preloaded caller instances keep identity and stay
  tracked, and the database still holds no new row.
- `A_real_caller_cancellation_at_the_capture_write_boundary_writes_no_row`: the caller's real token is
  cancelled at the capture write boundary; `OperationCanceledException` carries the caller's token
  identity, no row is stored, own entry detached, later `SaveChanges` returns 0.
- `A_real_unique_violation_returns_the_independent_winner_and_leaves_no_current_transaction`: an
  independent task-owned winner for the same (owner, draft, revision) is committed by a separate context
  after the idempotent pre-check and before the capture INSERT, so the **real** `23505` fires on
  `ux_authoring_revision_snapshots_owner_revision` and the **real** catch executes. Asserts the injected
  winner id is returned, `Database.CurrentTransaction` is `null`, no pending attempted entry remains,
  later `SaveChanges` returns 0, and exactly one row exists.
  Existing negative matcher facts (wrong state / wrong constraint name) are retained and were not weakened.

## 4. Executed proof

Target: task-owned disposable `127.0.0.1:15432` (`shipping_tb214` / `tb214`) via
`ShippingCaptureRelationalTestTarget`; one existing local `postgres:17` container
(`tooba-tb214r2-pg`), real migration chain applied only there.

```
dotnet build Tooba.Shipping.Infrastructure  -> 0 errors
dotnet build Tooba.Shipping.Tests           -> 0 errors
TOOBA_TB214_POSTGRES=Host=127.0.0.1;Port=15432;Database=shipping_tb214;Username=tb214;Password=local
dotnet test --filter FullyQualifiedName~ShippingAuthoringRevisionSnapshotStorageTests
  -> 8 executed / 8 passed / 0 failed / 0 skipped
     The_capture_table_and_its_exact_unique_index_exist_after_the_real_migration_chain          576 ms
     A_capture_is_the_complete_graph_and_stays_immutable_after_a_later_edit                    592 ms
     A_capture_racing_a_replacement_is_either_the_complete_old_or_new_graph_never_a_hybrid      60 ms
     A_real_caller_cancellation_at_the_capture_write_boundary_writes_no_row                    129 ms
     Two_real_contexts_capturing_the_same_revision_store_exactly_one_row                        77 ms
     Stale_tracked_copies_never_leak_into_a_capture_and_dirty_work_is_never_flushed             80 ms
     A_failure_before_the_capture_insert_propagates_and_publishes_nothing_later                 51 ms
     A_real_unique_violation_returns_the_independent_winner_and_leaves_no_current_transaction    82 ms
```

Only actually executed cases are counted; stale test source is not claimed as runtime proof. No wildcard
guard/compatibility/structure/snapshot batches were run, and no old writer battery was replayed.
Container removed afterwards; `127.0.0.1:15432` verified silent and no `tooba-tb214*` container remains.

## 5. Honest limits

- Timing: actual elapsed exceeded the hard 12-minute box (build/defect repair/test-fix rounds). The box was
  not met; behaviour numbers above are unaffected but this is not a process PASS.
- This is a captured-candidate staging primitive only. **No** publication, live pointer, activation,
  readiness proof, Storefront/Order consumer, or module/storefront certification is claimed.
- `214` table still is not applied to any shared database; §29 user semantics, calendar 213, FE 119 tests
  and owner decisions were not reopened.
- Default-OFF storefront gate, paused TB185 / PendingAllocation, user draft/revision/unsaved work,
  Host 5088 and FE 3000 unchanged; no HTTP write, no shared migration, no Host/FE restart.

## 6. 214-R1 report correction (history preserved)

`TB-...-214-R1/RESULT.md` overstated two facts, now corrected without rewriting that record:
the stale-context success path had run in a NEW context (`freshInA`) rather than the stale one, and the
two-context concurrency fact signalled a `Barrier` synchronously before the first `await` while ignoring
its return value. Both are now executed as claimed above.
