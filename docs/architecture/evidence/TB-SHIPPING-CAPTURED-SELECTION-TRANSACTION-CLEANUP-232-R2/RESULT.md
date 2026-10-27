# TB-SHIPPING-CAPTURED-SELECTION-TRANSACTION-CLEANUP-232-R2 — RESULT

Worker: tooba-worker-01 · Base: `92e1d9a039f6e4f4c4a583d47483642d64936b41` (main, fast-forward; the three
pre-existing Shipping line-ending drifts never staged) · Status: **EXECUTED**, NOT accepted, NO user Visual
ACCEPT, no live/HTTP writer, no frontend/Host/runtime change, no shared migration apply.

Scope: R1 group correlation, fresh preflight, coupon refusal and ordinary rollback were ACCEPTED WITHIN SCOPE by
the Architect and were **not reopened**. This task closes ONE transaction-lifetime / primary-failure blocker in
the same adapter. No Domain/schema/price/coupon/options/planner change.

## A — the transaction this adapter opens is now owned, disposed exactly once, and never adopted

`StorefrontShippingDraftStore` (`ReplaceCapturedSelectionAsync` + `CreateCapturedSelectionAsync`):

- **Disposal on EVERY path.** Each owned `IDbContextTransaction` is consumed exactly once: the success path in
  one awaited `finally` (`DisposeOwnedAsync`), every failure path inside the same cleanup that performs the
  rollback, and the local handle is set to `null` immediately after that consumption so the `finally` cannot
  double-dispose it. Begin-failure leaves the handle `null` and disposes nothing. A secondary exception from
  `DisposeAsync` is swallowed **only** in cleanup and can never change the primary result.
- **No adoption/nesting.** A caller-owned transaction (`db.Database.CurrentTransaction is not null`) is refused
  **before any mutation** with the existing `storefront.captured_selection.pending_work` reason: an open caller
  unit of work is exactly "other unsaved changes still in progress", and the adapter neither nests nor adopts it.
  The caller's transaction is not rolled back and not disposed by us; it stays current and usable.
- **Rollback before dispose, detach independent of rollback.** `RollbackAndDisposeAsync` rolls back with
  `CancellationToken.None` (never the caller token), then disposes, then reports whether the rollback was
  **confirmed**. Only "already completed" (`InvalidOperationException`) and any real provider/rollback failure are
  treated as *unconfirmed*; neither is re-thrown and neither replaces the primary failure.
- **Exact, pre-captured detach.** Previous groups **and their lines** plus the attempted groups **and their
  lines** are captured before the first mutation, and `RestoreRejectedAttemptAsync` detaches exactly those entries
  plus the root — after the rollback attempt, so it runs **even when rollback throws**. No context clear, no global
  line sweep, no retry, no silent caller flush; unrelated graphs keep their state.
- **Recovery read only when the rollback is confirmed.** The rolled-back root is re-read from PostgreSQL only when
  `RollbackAndDisposeAsync` confirmed it, and only after our transaction is disposed. If the rollback is
  unconfirmed the attempt is left detached and **no fabricated restored graph** is produced (recorded limitation
  below). A failing recovery query is swallowed for the same reason: the primary exception wins.
- **Conflict mapping unchanged and narrow.** Only `DbUpdateConcurrencyException` and the existing exact root-PK
  uniqueness violation map to `conflict`; an unexpected provider failure is neither success nor conflict.

## B — proof (real provider seams only, ONE disposable database)

Isolated task-owned `postgres:17-alpine` `127.0.0.1:15433/order_tb231/tb231`, identity asserted before DDL, normal
Order migration chain, container removed, `15433` verified closed. No production hook: the seams are test-only EF
interceptors (`ISaveChangesInterceptor`, `DbCommandInterceptor`, `IDbTransactionInterceptor`) registered on this
class's `DbContextOptions`, plus EF's own DiagnosticSource `Microsoft.EntityFrameworkCore.Database.Transaction.*`
events — because EF has **no** interceptor callback for transaction disposal and a cleared
`Database.CurrentTransaction` is not disposal.

New exact class `StorefrontCapturedSelectionTransactionTests` — **5/5**:

1. `A_successful_create_and_replacement_dispose_their_own_transaction` — create and replace each emit exactly one
   `TransactionDisposed` (1 then 2), `Database.CurrentTransaction` is null afterwards, and the same context starts
   and commits its OWN explicit next transaction (3), so no handle is retained; the replacement persisted
   revision 2 with the new area group and `SaveChangesAsync()` returns 0.
2. `Cancellation_at_the_write_boundary_keeps_the_caller_token_and_disposes_the_transaction` — the caller CTS is
   cancelled **inside the second `SaveChangesAsync`** (test interceptor at the write boundary, not a pre-cancelled
   token): the thrown `OperationCanceledException` carries the **caller's** token, the rollback ran with a
   non-cancelled token (1 attempt), the owned transaction was disposed (1), `CurrentTransaction` is null, no
   `Added/Modified/Deleted` group/line survives for a later `SaveChanges` (0), the unrelated draft is still
   `Unchanged` with its note and revision, and the database still holds revision 1 with the old group/line.
3. `A_rollback_failure_never_replaces_the_primary_failure_and_still_detaches_the_attempt` — a real provider
   primary failure (duplicate cart-line uniqueness) plus an injected `TransactionRollingBack` fault: the **same**
   primary instance propagates (`Assert.Same` against the exception EF reported in `SaveChangesFailed`), the
   scripted rollback fault is not surfaced (1 attempt, 1 fault), **no recovery read happens** because the rollback
   was never confirmed, the attempt is still detached so nothing leaks into a later `SaveChanges` (0), the owned
   transaction is disposed (1), and the unrelated draft/graph is untouched.
4. `A_recovery_read_failure_preserves_the_primary_failure` — same primary failure with the recovery SELECT failing:
   the failure is counted (1) and the **primary instance** is still what surfaces; nothing is left pending.
5. `A_caller_owned_transaction_is_refused_without_being_adopted_or_disposed` — with a caller transaction open the
   write is refused with `storefront.captured_selection.pending_work` before any mutation, the caller transaction
   is still `Database.CurrentTransaction` (not rolled back, not disposed: 0 disposal events), and it still commits;
   stored state stays revision 1 with the old group.

RED→GREEN on the reviewed source: with the R1 adapter restored, the same lifetime fact fails with
`Expected: 1, Actual: 0` (`TransactionDisposed` never fired for create/replace — the leak is real, not a source-text
claim). GREEN after the repair.

Then the existing 232 classes ran ONCE together with this one: **31/31** (`StorefrontCapturedSelectionStorageTests`
+ `StorefrontCapturedSelectionWriterTests` + `StorefrontCapturedPreviewWriteGuardTests` + the new class). No
285/310/312/full Storefront/Shipping/Host/solution/guard sweep. Order.Infrastructure and Order.Tests build 0
errors, 0 warnings.

## HONEST LIMITS

- The injected rollback fault is a **scripted** `TransactionRollingBack` exception, not a real database outage; it
  is deliberately distinguished from "transaction already closed". In this fixture the subsequent `DisposeAsync`
  performs the implicit rollback, so the DB ends old — the **adapter**, however, treats recovery as unconfirmed and
  fabricates nothing.
- With an unconfirmed rollback there is **no** restored in-memory graph: the attempt is detached and the caller
  must re-read. That is the recorded limitation of this repair, not a "recovered context".
- EF provides no disposal interceptor: disposal is proven through its own DiagnosticSource event, and
  `Database.CurrentTransaction` being null is explicitly **not** used as proof.
- The cancellation fact exercises the second `SaveChangesAsync` (state already deleted/re-saved) via a test
  interceptor, not an OS-level thread abort mid-command.
- Writer remains model-bound (never exercised over HTTP/live); `DestinationServiceAreaId` stays NULL; sequential
  CAS facts remain sequential, not a simultaneous race; 231/232 migrations stay WRITTEN/sharedNOTAPPLIED and the
  stale EF snapshot gap is retained; `CommitAsync`/`ProjectPaymentDraftAsync` stay `not_ready`, `SelectionEnabled`
  checked-in false.
- Retained unfixed: 230-R1 empty-key retry/in-flight wait, cross-owner label/decoder, Offer API proof, 227 mobile
  empty-note, 225-R1 lost unsaved LOCAL edits.
- The user's visible review window, live Host/FE, previewON/count7, runtime data, buyer/cart, draft/rates/week/
  pointer/assignments were NOT touched (no browser, no enumerate/reload/close).
- Elapsed ≈62 min vs HARD20 — recorded **NONCOMPLIANT**, not a process PASS.

NEXT (Architect): review this cleanup proof; the writer becomes acceptable only if you accept it. Then commit-time
destination/cart/pointer/quote revalidation and coupon read-write-final parity, then final order composition /
Order-only historical read. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
