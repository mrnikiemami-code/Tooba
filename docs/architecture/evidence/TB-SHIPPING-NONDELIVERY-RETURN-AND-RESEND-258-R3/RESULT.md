# RESULT — TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R3

Status: **PASS** within the issued two-gap repair scope.
Decision: **CANCELLED_INTENT_EXACT_IDENTITY_BEFORE_RESULT_AND_NEW_SEND_REFUSED_WITHOUT_RELOAD_DURABLE_ENVELOPE**.

This is NOT Architect ACCEPT and NOT user VISUAL/FUNCTIONAL ACCEPT.

- Expected base: `a56814cb7ea97126bcab08da45b1886449fd3bab`
- Working-tree base: `5104375d5` (Architect issue/docs commits only; expected base is an ancestor)
- Code/evidence SHA: recorded in the task commit
- Worker: one, no parallel task. `STOP_FOR_ARCHITECT`; automaticNext NONE.

## A. A cancelled occurrence answers only its EXACT intent

`FulfillmentUnit.RecordedResendAnswersIntent` is the ONE identity authority and no longer short-circuits on
`Cancelled`: it is now a pure identity predicate (source parcel, accepted shipping group, effective method,
canonical per-line quantities). Both consumers decide the state AFTER it:

- `ResolveRecordedResendIntent` (read-only public-command reconcile): identity mismatch → `Conflict`
  (`fulfillment.resend.intent_conflict`, zero writes) even when the recorded parcel is cancelled; identity match →
  `ResolvedCancelled` / `Recorded`.
- `ReplayRecordedResend` (mutating replay): identity mismatch → `fulfillment.resend.intent_conflict`; identity match
  on a cancelled parcel → `fulfillment.resend.occurrence_cancelled` (history retained, never recreated).

A blank occurrence key is still `NotRecorded` (the owning command refuses it as `fulfillment.resend.occurrence_required`);
current actor permission, checkout/fulfillment/parcel ownership and the parcel-card action owner are unchanged.

## B. No first POST without a reload-durable retained envelope

`session/post-dispatch-attempt-store.ts` now distinguishes durability from memory:

- `writePostDispatchAttempt` returns `{ attempt, durable }`. `durable: true` only after a successful
  `sessionStorage.setItem` that is verified by read-back; `undefined`/`getItem`/`setItem` throwing, quota or a
  silently-dropping storage → the attempt is kept in the in-memory mirror with `durable: false`.
- The module-level "storage is broken forever" latch was removed; storage is probed on every call, so a restored
  storage serves the next attempt (a transient failure cannot permanently disable durability).
- `dialog.send()` is the SEND BOUNDARY: it writes the complete envelope first and, when it is not durable,
  shows one concise actionable inline refusal («…برای جلوگیری از ارسال تکراری، درخواست ارسال نشد…») and returns
  BEFORE `onSubmit`/fetch. The reviewed values, the open review and the previously recorded envelope are kept.
- `clearPostDispatchAttempt` is still reached only on an authoritative completion; write failures, unknown
  outcomes and error codes never clear or replace a durable envelope. Single-flight (`useRef`) and viewer-scope
  isolation are untouched.

Result: failed/absent durable storage produces ZERO network requests and ZERO allocations; with working storage the
envelope is durable BEFORE the POST leaves the browser; an uncertain outcome remounts and replays the exact saved
request.

## Proof (focused only)

| Suite | Result |
| --- | --- |
| `PostDispatchException258R1Tests` (pure, includes new R3 cancelled-identity case) | **12/12** |
| `PostDispatchExceptionRelational258Tests` on real `tooba_alpha` | **11/11** |
| `AdminOrderPostDispatchReplayTests` (7 pure + 2 relational, incl. new R3 public-command case) | **9/9** |
| `admin-order-operational-delivery.mount.test.ts` (mounted, incl. new R3 storage case) | **40/40** |
| `dotnet build Tooba.slnx -c Release` | 0 errors |
| `npx tsc --noEmit` | 0 |
| `npm run build` | 0 |
| `dotnet publish` (Host) | 0 |

New fixture ids (task-labelled, real canonical database) are in `tb258-relational.txt` and
`tb258r3-orchestrator-relational.txt`. The cross-module public-command case proves, on real storage through
`AdminOrderOperationsOrchestrator` → `FulfillmentAdminOperationsAdapter` → `FulfillmentDirectory`:
`exact=resolvedCancelled changedSource=conflict changedQuantity=conflict rows=shipments2/receipts1/resends1` and the
cancelled parcel still `Cancelled`.

## Deploy

- No migration (no schema change; nothing re-applied).
- Host republished to `artifacts/publish-primary` and restarted (PID **32080**, `/health` 200).
- FE rebuilt and restarted (PID **47852**, BUILD_ID `Ljnu38AmJOn-eqg7FcKKq`, `/fa` 200; previous BUILD_ID 404).
  The served chunk `9127-ec36efb7727c0afc.js` contains the read-back durability branch (`{attempt,durable:!0}` /
  `{attempt,durable:!1}`).
- ONE database `tooba_alpha` @ 127.0.0.1:5432; Edition `SingleStore`, tenant `store-alpha` → `tenant-alpha` →
  `tooba_alpha`; no disposable/test/parallel/Docker DB, no 15433, no edition/connection/port/appsettings change.
- DataProtection key file unchanged (`key-d82e31a6-…xml`, 2026-11-02); no manual DDL/business SQL/reset/reseed/
  backfill; user rows read-only; CapturedPreview state untouched (local ON / checked-in OFF).

## Limits

- No live Tipax/post provider call; no waybill.
- Origin receipt remains a one-time final receipt.
- No Inventory/Returns contract write.
- Package safety is still a refusal for a locked child (parent workflow is 259 territory).
- Concurrency proven with two real contexts + durable DB guards, not a stress test.
- Browser storage durability is proven for `sessionStorage` (reload-durable by definition); the in-memory mirror
  is explicitly NOT claimed as durability.

## Ledger

save prerequisite CLOSED; issue1 tracking FIXED 257; issue2 remaining quantity FIXED 257; **issue3 pending
Architect review of this 258-R3 result**; issue4 common-or-separate parcels 259 NOT ISSUED; issue5 due
workqueue/shared operational order view 260 NOT ISSUED. Five-defect completion is NOT claimed.
