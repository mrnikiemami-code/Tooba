PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R3
Parent-Task: TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: EXACT_CANCELLED_INTENT_AND_REFUSE_NON_DURABLE_FIRST_SEND
Expected-Base-SHA: a56814cb7ea97126bcab08da45b1886449fd3bab
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## Position
Read CURRENT TOPs, closure plan256–260,258-R2/ARCHITECT-REVIEW.md and mandatory repository feature skills/design-system rules.
258-R2 fixes are accepted within source review EXCEPT the two exact gaps below. Preserve public reconcile, mixed receipt UI, source-specific capacity, immediate lineage, audit, existing single action owner and quantity/tracking authority. Do NOT repeat previous implementation or broad tests.
One task/worker; same repository/main. Documentation descendants of expected base legitimate; verify HEAD/origin and preserve unrelated edits. No force/amend/reset/stash/rebase.
Save closed; issues1/2 accepted257; issue3 pending here;259/260 NOT ISSUED. User authorized sequential work and final handoff after five defects.

## A. Cancelled result must still match exact intent
FulfillmentUnit.ResolveRecordedResendIntent currently returns ResolvedCancelled BEFORE request normalization/RecordedResendAnswersIntent.
Move full source/group/method/canonical quantity identity validation BEFORE choosing Recorded vs ResolvedCancelled. A changed source or quantity under a cancelled occurrence MUST be fulfillment.resend.intent_conflict, no writes; exact same intent may resolve cancelled, no recreation. Do not remove cancelled history or weaken current actor/checkout/fulfillment authority.
Keep ONE shared identity authority for read-only reconcile and mutation replay; avoid another hand-coded predicate. A blank occurrence is never a replay.
Proof: actual public orchestrator + production adapter on a task-labelled canonical fixture: create resend, cancel pre-dispatch, exact retained request resolves cancelled; same key different source and same key different quantity both conflict with unchanged rows/allocations. Current unauthorized/foreign checkout remains refused. No unrelated lifecycle redesign.

## B. No first POST without reload-durable retained envelope
writePostDispatchAttempt currently returns a successful attempt after sessionStorage fails, using memoryFallback only. Dialog sends POST anyway. Full reload loses the Map and can blindly mint another partial resend. Do not claim in-memory fallback is durable.
Implement the smallest safe repair: distinguish DURABLY persisted review from memory-only/failed storage and refuse a NEW mutation before onSubmit/fetch when full intent cannot be persisted. One concise actionable inline operator error; keep local review/edit values. No new status/idempotency framework, table, endpoint or browser-storage permission workaround.
Existing durable retained review may still be read/reconciled safely if its exact envelope is available; never replace/clear it on a write failure, unknown result or error code. The send boundary must enforce this, not just button disabled styling.
- sessionStorage undefined/getItem throwing/setItem throwing/quota: no NEW POST and no allocation.
- An in-memory old attempt with uncertain progress is not permission to mint a fresh key.
- Successful durable write precedes first POST; lost-response remount submits exact saved request.
- synchronous single-flight and finally retained; thrown transport outcome preserves exact request immediately.
- Viewer switch isolation remains. Successful authoritative completion alone clears that attempt.
If an alternate already-existing durable owning recovery mechanism is used, prove it survives full document reload; do not add infrastructure for this task.
Focused mounted REAL panel/dialog proof: storage failure -> visible inline refusal + zero fetch; restore working storage -> one deliberate durable send; normal lost response -> remount exact request; cancellation identity tests above.

## Scope / environment
ONLY same tooba_alpha5432, FE3000 -> Host5088 SingleStore/store-alpha. No extra/disposable/test/Docker DB,15433, edition/connection switch. Minimal labelled production-path fixtures in SAME DB permitted; exact IDs recorded. Real user order/cart/parcel/payment/service rows read-only.
No migration expected; no raw business SQL/manual DDL/reset/reseed/backfill/deletion. Preserve DataProtection, local CapturedPreview ON/7dates and checked-in OFF.255 parked, live Tipax deferred. No provider call/credentials/fake waybill.
Preserve256 calendars/save,257 quantity/tracking,258 accepted work. Active package child lock remains; parent workflow later259.
Run ONLY relevant identity/storage mounted + public-command focused tests and typecheck/build/publish for changed components. No broad module/base worktree comparisons or repeated green loops; no screenshots. Existing unrelated reds disclosed, no baseline edits.
Deploy canonical changed components; verify health, served BUILD_ID/runtime and read-only order no regression. No real business mutations.

## Evidence and handoff
Persist this TASK verbatim under docs/ai/tasks/TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R3/ and TASK/RESULT/RECEIPTS/result-contract plus exact focused proof under docs/architecture/evidence/TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R3/.
Update AGENTS.md, START-HERE-IF-CHATGPT-IS-LOST.md, SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md and closure plan: issue3 pending Architect review;259/260 NOT ISSUED; five-defect completion not claimed.
Return BEGIN_TOOBA_WORKER_RESULT with full Base/Code/Merge/Evidence SHA, HEAD==origin/main, exact changed paths/cases/fixture IDs/runtime/build/limits. Show changed cancelled intent is refused and failed durable storage sends ZERO network requests. Worker PASS != Architect/user acceptance.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
