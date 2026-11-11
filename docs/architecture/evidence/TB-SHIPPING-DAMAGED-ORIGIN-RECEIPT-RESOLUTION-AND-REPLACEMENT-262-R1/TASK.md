PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262-R1
Parent-Task: TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: REPAIR_PUBLIC_PENDING_REPLACEMENT_RESUME_STALE_CANCEL_AND_ATOMIC_ORDER_HOLD_RETENTION
Expected-Base-SHA: a67e514ead76a058788add1be571512791db6646
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Bounded correction
Read CURRENT TOPs,262/ARCHITECT-REVIEW.md, original262 TASK including USER RESERVATION OVERRIDE, mandatory repo feature/Contracts/design-system rules.
262 worker PASS is PARTIAL. Preserve accepted source quantity authority/separate Inventory ownership/replacement linkage/UI/Toast; repair ONLY three real seams below. No generic framework/new product policy.
One worker/task then STOP. User forbids loops: focused failing tests below, affected checks/build/publish once, no full-module/solution test/base parity/green repeats/screenshots. Current docs descendants legitimate; safe HEAD/origin; no force/reset/amend/stash/rebase/unrelated overwrite.

## 2. A — public command resumes incomplete replacement
RunDamagedResolutionAsync currently returns ANY recorded.Snapshot before ResolveDamagedGoodsCoreAsync. Recorded includes durable no-stock decision and Reserved with no live parcel after crash/cancel. Retrying public command can say ok with no parcel and UI clears envelope.
Distinguish recorded intent identity from workflow completion:
- exact incomplete REPLACEMENT occurrence must continue its recorded decision/reservation/parcel attempt through production owning execution, with current actor/checkout/fulfillment/source checks; no second decision/reservation;
- stock still unavailable => precise pending/blocker, retained exact request and reachable contextual retry; no generic success/clear/new mint;
- healthy stock becomes available => SAME intent reserves/continues once and returns actual linked parcel;
- reserve/link/parcel fault retry and cancelled replacement restart reach the actual owning continuation through PUBLIC orchestrator/endpoint, not only direct directory;
- an already completed live replacement returns its exact parcel zero allocation. Explicit referral returns 'referral recorded, awaiting handling', never financial/delivery completion. Order-cancelled resolution is historical, never resurrected;
- changed intent typed conflict zero writes, no bypass fresh authorization/owner/package safety.
UI must retain and expose recorded pending decisions even after OutstandingDamagedQuantity became0 and fresh action vanished. On reload without local hint, server-owned pending-resolution facts must provide lawful continuation of EXACT recorded decision, not force a new incompatible decision/key. Reuse existing dialog; server record is authority.
No bare error-code release; no status-only success that hides unfinished customer obligation.

## 3. B — cancelling old parcel cannot detach current replacement
Directory.CancelShipmentAsync unconditionally invokes RetainReplacementStockForCancelledParcel. That method currently clears resolution link/increments Attempt without checking requested parcel is current linked replacement.
Make state change idempotent and exact:
- only FIRST valid pre-dispatch cancellation of CURRENT resolution.ReplacementShipmentId may detach that allocation and advance parcel attempt once;
- replay cancellation of same already cancelled parcel is no-op;
- replay cancel of old parcel AFTER replacement retry created a new linked parcel must preserve new link, attempt, held reservation and quantities;
- dispatched/in-transit/delivered can't cancel; current actor/public operation guards retained;
- only one live parcel may reference/use this resolution's held units. Add minimal durable invariant if needed; do not rely on UI hiding old button.
Reservation NEVER released by parcel cancel. New legitimate parcel attempt reuses SAME order-bound hold; original consumed sale reservation untouched.

## 4. C — retention must hold before first reserve commit
Current ReserveReplacementReservationAsync(...now+7days) then separate CommitReservationForPaidOrderAsync has a crash window. A replacement hold may expire while order active.
Inventory-owned replacement reservation must be created with no expiry/paid-order retention purpose ATOMICALLY from its first durable commit. Do not allow caller/default TTL to reintroduce expiry for this purpose. Reuse existing Inventory atomic reservation primitives/Contracts; no rewrite of unrelated abandoned-cart timeout rules.
A crash after Inventory reserve before Fulfillment link/commit must leave ONE non-expiring order-bound hold recoverable by exact key. Same active-order hold reused, not released/reacquired.
Only authorized ORDER cancellation may release unconsumed hold once; no claim of reachable post-dispatch whole-order cancellation if existing domain forbids it. Preserve disclosed actual ordering limit, no cancellation-policy redesign.
Dispatch consumes NEW hold once, not original reservation. No new hold on retry/old cancel replay; consumed holds never released/resurrected. No historical business-data backfill.

## 5. Targeted proof — actual layers, same DB only
Only canonical tooba_alpha5432 / FE3000 -> Host5088 SingleStore/store-alpha. No extra/disposable/test/Docker DB15433/edition switch. User/screenshot rows read-only.
Minimal labelled production-path fixtures IN SAME DB authorized, exact IDs. No raw business SQL/manual DDL/reset/reseed/backfill/deletion.
A PUBLIC command no-stock => recorded pending blocker; retry still pending not success; add healthy availability through owning task fixture path => SAME occurrence yields linked parcel; zero second decision/hold. Reload real panel from server pending facts exposes retry even no fresh capacity/local hint.
B PUBLIC command crash after reserve/before link and after link/before parcel => exact continuation, no premature completed response. Cancel current parcel -> retry public command => same hold/new parcel. Completed replay same ids.
C cancel old P1 twice, create P2, cancel P1 again => resolution still linked P2, attempt unchanged, one active parcel/hold. Verify persistence with independent contexts/stale request.
D intercept immediately after reserve durable commit BEFORE any follow-up retention/attach: ExpiresAt alreadyNULL; expiry processing with future injected clock retains it. Retry recovers exact hold. No clock change.
E authority/changed intent/package lock refusals unchanged; mounted real panel Toast/pending/retry behavior and durable exact envelope. No duplicate success or clearing on incomplete result.
Do not repeat broad suites/concurrency stress; focus on these introduced gaps. Typecheck/build/publish relevant components only. Necessary minimal additive migration if invariant requires it: private backup + existing MigrationRunner; old rows readable, no manual DDL.

## 6. Deploy / evidence / stop
Source D:\Users\User\source\repos\SarvNewVer, same ports/DB. Preserve keys/CapturedPreview locallyON7dates/checked-inOFF;255 parked/Tipax deferred. No provider calls/fake labels/real user mutation.
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262-R1/TASK.md, TASK/RESULT/RECEIPTS/result-contract + actual public-path cases docs/architecture/evidence/TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262-R1/.
Update AGENTS.md, START-HERE-IF-CHATGPT-IS-LOST.md, SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md and261–262 correction plan:262-R1 pending Architect review, not user ACCEPT/all complete.
Return BEGIN_TOOBA_WORKER_RESULT full Base/Code/Merge/Evidence SHA, HEAD==origin/main, changed paths, exact pending-vs-complete result, stale-cancel proof, atomic no-expiry fault proof, fixture IDs/runtime/build/limits. No further task proposed.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
