PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R2
Parent-Task: TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — finish real-page return receipt and lost-response recovery
Mode: CLOSE_PUBLIC_COMMAND_REPLAY_GATE_REAL_PAGE_RECONCILE_ENTRY_AND_MIXED_RECEIPT_UI
Expected-Base-SHA: 29d3ce3688e58273e92e51fe1b1c782e485ebc1b
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Read first / bounded correction
Read CURRENT TOPs, closure plan256–260, 258-R1/ARCHITECT-REVIEW.md and repository mandatory backend/frontend feature skills and design-system rules.
Worker258-R1 PASS is PARTIAL after independent source review. Preserve its accepted source-scoped capacity, immediate lineage, aggregate exact-intent replay, operator audit and domain mixed-condition receipt validation. This task fixes only three integrated gaps; do not rebuild the lifecycle or repeat the whole module.
Issue1 tracking + issue2 remaining quantity accepted257, save256-R2 closed. Issue3 still open;259/260 NOT ISSUED. User authorized sequential work with final handoff after all five defects. One worker/task; no automatic next task or user acceptance pause now.
Verify actual HEAD/origin and safe worktree. Issue/review/recovery doc descendants of expected base are legitimate; no force/reset/amend/stash/rebase or unrelated overwrite.

## 2. A — exact replay must reach the owning state THROUGH the public command
Confirmed source: AdminOrderOperationsOrchestrator.Operations.cs ResendShipmentAsync / RecordReturnReceiptAsync call RunProjectedCoreAsync without a reconciliation path. EnsureProjectedActionAllowedAsync requires a current projected action. Once resend consumes source capacity, resend_shipment disappears; once receipt commits, record_return_receipt disappears. Retry is rejected before directory/aggregate replay.
Repair the actual endpoint -> MediatR -> Order orchestrator -> production Fulfillment adapter path:
- exact recorded resend occurrence + exact source/group/method/quantities returns its recorded parcel even when fresh resend readiness is false;
- exact recorded receipt reconciles its existing receipt after the parcel became Returned, no second receipt/capacity;
- recorded non-delivery/start-return attempts likewise resolve from their owning facts if a response was lost; never repeat or overwrite actor/time/history;
- current actor permission, checkout/fulfillment/seller/source relationship, ownership and applicable package safety are checked on EVERY request, including replay;
- changed intent is a typed conflict with zero writes; cancelled recorded resend remains a resolved cancelled historical occurrence, never recreated by retry;
- an absent record must NOT turn a readiness failure into a new allocation. Only a legitimately eligible fresh command may mutate.
Use a narrow owning reconcile path or exact recorded-evidence branch; do NOT enable general allowReturnLifecycleFallback/return.manage bypass, remove projection guards globally, trust a client flag as authorization or treat bare error text as proof.
Return an authoritative result the UI can recognize for this exact retained request; success/resolved cancellation must not be inferred from a stale list or unrelated parcel. Preserve same-key replay before capacity gates and the non-empty occurrence requirement.
No generic idempotency/status framework. Existing owning durable records are the evidence.

## 3. B — one real-page recovery entry survives state refresh
Confirmed source: operational section mounts the dialog only after a normal action button, and resend button exists only while caps.canResend. The section does not load retained attempts. A remounted OPEN dialog test is insufficient.
On the existing parcel card, read the exact viewer/checkout/fulfillment/source/mode retained scope and show ONE concise contextual reconcile entry while an unresolved envelope exists, even when the normal action has disappeared after partial/full server progress.
- open the existing purposeful dialog with the retained envelope, read-only reviewed quantities/note and one reconcile action;
- retry sends the saved request verbatim through the REAL panel/API callback, not a test stub that returns true;
- page refresh/remount, close/reopen and lines-reference changes preserve it; changed review cannot overwrite it or mint a new occurrence;
- one same-tick submit = one request; catch thrown/undefined/network/5xx/typed refusal and retain the full envelope immediately, not only after a later remount;
- only exact authoritative completion/resolution clears the hint; a deliberate next operation then uses a new key;
- viewer/scope switch clears stale in-memory view and does not inherit another actor's hint; current authority governs the reconcile action too. A retained browser value is never authorization;
- storage unavailable cannot silently lose an uncertain attempt and allow blind remint after reload; choose a bounded safe behavior with existing owning reconciliation, or refuse an unsafe new send with concise copy. Do not claim durable recovery from an in-memory Map alone.
Cover receipt, return and non-delivery retained modes as applicable, not only resend. Keep exactly one parcel/action owner and no duplicate seller aside controls. No new parallel page, raw diagnostics, technical paragraphs or inline selection clutter.

## 4. C — operator can actually record usable + damaged for one parcel line
Confirmed source: dialog ReceiptDraft has one quantity and one condition per line, submit emits one receipt item. Domain support alone is not delivery.
Use a compact review row with explicit «سالم» and «آسیب‌دیده» quantities for each line (or an equally clear design-system pattern).
Example: parcel line3, operator enters سالم2 + آسیب‌دیده1 -> ONE request with TWO items of that same line, conditions Usable/Damaged, exact quantities2/1. Durable receipt grants only2.
- validate positive submitted entries, numeric validity and SUM <= that parcel line's carried quantity;
- show an inline error for excess/invalid entries; do not Math.min-clamp or silently change the operator's reviewed quantities;
- zero categories omitted, missing goods do not become usable by default; intentional final-receipt confirmation wording stays concise;
- retained read-only description and replay show BOTH categories accurately; server remains authoritative;
- exact duplicate receipt replay is lawful, changed receipt conflict preserves the original and grants no capacity.
No incremental receipt redesign, RMA redesign or Inventory saleable-stock integration; keep one final receipt explicit.

## 5. Focused proof — integration, not isolated helper substitution
ONE database tooba_alpha @5432 only. No disposable/test/parallel/Docker DB,15433, edition/connection/runtime switch.
User orders/parcels/payment/service records read-only. Minimal task-labelled fixtures through owning production domain/application paths in this SAME DB permitted; record exact IDs, no raw business SQL/manual DDL/reset/reseed/backfill/sweeping delete.
Required meaningful cases:
A production public command/orchestrator first resend commits -> response lost -> reload real projection shows source capacity0/no fresh action -> exact same command returns exact parcel with zero new rows; changed quantity/source conflicts; unauthorized/foreign checkout refuses. This MUST cross the formerly blocking RunProjectedCoreAsync gate.
B same for committed receipt after Returning -> Returned; recorded transition retries do not reattribute actor/time or duplicate facts.
C mounted REAL section + panel from no dialog open, retained attempt + refreshed server detail/capabilities false -> visible one reconcile entry -> open -> SAME envelope/key posted; exact completion clears; changed review cannot remint. Do not mount dialog manually open as the only proof.
D thrown transport outcome + close/remount, viewer switch, storage unavailable and same-tick submit safety; no second new operation under uncertainty.
E mounted mixed receipt inputs2usable/1damaged -> exact real payload -> owning API persistence/readback grants2; excess total4 refuses WITHOUT clamping/post; retained mixed request replays verbatim.
Retain focused258-R1 source capacity/lineage/exact replay and257 quantity/tracking checks as relevant. No broad full-module/base worktree reruns or repeated green loops. Affected suites + typecheck/build/publish only. Existing unrelated reds disclosed, no allowlist/baseline changes.
No screenshots required. Read-only DOM of known existing order can verify no visual regression; it cannot substitute for these fixture integration cases.

## 6. Canonical deployment / boundaries
Source D:\Users\User\source\repos\SarvNewVer.
FE127.0.0.1:3000 -> Host127.0.0.1:5088, SingleStore/store-alpha/tenant-alpha/tooba_alpha. Deploy changed components and prove served BUILD_ID/API/runtime.
Preserve DataProtection, CapturedPreview locally ON/7dates and checked-in OFF.255 parked, live Tipax deferred. No provider call/credentials/fake waybill.
No new migration expected for these integrated gaps. If indispensable, explain owning need and apply only additive migration after private backup using existing MigrationRunner; no manual DDL or invented historical facts.
Preserve approved authoring tabs/save,256 scheduling,257 quantities/tracking and258-R1 source capacity/immediate lineage/audit.
Active consolidated-package child locks remain; parent workflow belongs later259. No silent unlock or forced scope expansion.
Do not mutate real review order01a1b92d-e97e-7000-999a-143350d0edaf.

## 7. Evidence / result / stop
Persist exact TASK.md under docs/ai/tasks/TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R2/ and TASK/RESULT/RECEIPTS/result-contract + focused evidence under docs/architecture/evidence/TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R2/.
Update AGENTS.md, START-HERE-IF-CHATGPT-IS-LOST.md, SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md and closure plan: issue3 pending Architect review, not five-defect complete;259/260 still NOT ISSUED.
Return BEGIN_TOOBA_WORKER_RESULT with full Base/Code/Merge/Evidence SHAs, HEAD==origin/main, changed paths, exact public-command + real-panel proof (which layers executed), fixture IDs, runtime/build, limits. Do not claim a lower-level directory test proves public endpoint replay or a manually opened dialog proves real-page recovery.
Worker PASS is not Architect/user acceptance.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
