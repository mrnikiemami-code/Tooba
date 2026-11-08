PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R6
Parent-Task: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R5
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — remove error-code-based abandonment of unresolved operations
Mode: RETAIN_EXISTING_OCCURRENCE_UNTIL_AUTHORITATIVE_RESOLUTION
Expected-Base-SHA: 567a531b0c27be281405ffe9da523c06c5f31f8d
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

Exact decision

R5's two requested fixes are accepted within their bounded code/test scope:
changed-review remint is blocked, and exact occurrence parcel reconciliation precedes remaining quantity.
Do not redo those changes. Only the newly introduced no-claim release exception below prevents recovery acceptance.
This is one small correction, not another workflow redesign.

Defect and concrete reproduction

operational-group-occurrence-store.ts treats these bare codes as proof that no durable claim/side effect exists:
workflow_occurrence_required, workflow_group_foreign, consolidate_requires_multi_seller,
consolidate_incompatible, order.operation.denied.
releaseOperationalGroupAttemptAfterNoClaimRefusal then deletes the retained envelope.
OperationalCard invokes that release automatically on such an outcome.

But "thrown before ClaimAsync on THIS HTTP request" does NOT mean "this occurrence was NEVER claimed":

request A claims occurrence and persists a constituent parcel, then response/following step fails;
retry of that SAME occurrence encounters permission loss (order.operation.denied), or changed lifecycle compatibility;
those checks run BEFORE _occurrences.GetAsync, so the server has not proved absence of the existing record;
frontend deletes the only unresolved hint and may mint a new operation when permissions/readiness return.

This is an introduced regression, not a hypothetical new requirement. A bare error code cannot authorize forgetting prior durable progress.

Required minimal repair

Remove code-list-only deletion from both normal action and reconcile paths/state helper.
Any generic denial/compatibility/foreign/transport error must retain existing occurrence and full request.
Authorization loss hides/disables mutation as appropriate; it never proves no prior side effect.
Only authoritative completed/resolved result clears an existing unresolved operation.
If supporting correction after a genuinely new pre-write refusal, require a structured server result proving THIS occurrence has no durable claim/progress under the proper authorized scope. Never infer it from code or loosen access checks to obtain it.
Prefer removing the optional unsafe release mechanism now over building a new status framework. If no server no-claim proof is implemented, keep the request retained and explain the actionable refusal; do not claim editable/new-operation recovery for that state.
No "reset/delete attempt" shortcut, automatic new occurrence, replacing failed request, or loss of exact stored envelope.
Preserve 253-R4/R5 durable intent, explicit reuse, actor scoping, single-flight and exact-key crash recovery.
Keep professional current UI; no new layout, warning panel duplication or raw diagnostics.

Focused proof only

State + actual mounted flow:
existing retained envelope → order.operation.denied → envelope/occurrence unchanged;
restore authority, changed review → no new occurrence/request; exact reconcile still possible.
Same retained case with consolidate_incompatible/foreign refusal and 5xx/network retains it.
Authoritative success clears; explicit next action obtains fresh occurrence.
If structured no-claim support is added, prove absent vs EXISTING occurrence outcomes separately, with no progress destroyed and no unauthorized information disclosure.

Use existing test fixtures; no repeated full PostgreSQL matrix is needed for frontend-only removal.
If backend no-claim logic is added, test the exact existing-record/absent-record distinction using existing disposable setup once.

Run affected mounted/state tests, typecheck and production build. Host publish only if backend changed.
No broader scans, full-suite loops, screenshots or provider tests.

Canonical delivery and recovery

Original D:\Users\User\source\repos\SarvNewVer, FE3000→Host5088 SingleStore/store-alpha/tooba_alpha.
Deploy the corrected affected runtime; preserve keys, migration 20261108153000, checked-in gates OFF and local CapturedPreview ON with AvailableDateCount=7.
No new migration expected; no user order/cart/pack/create/dispatch/cancel/payment/business-data changes.
No live Tipax/account request, alternate DB/runtime, reset/reseed/manual DDL or user tabs automation.
Read-only URL:
http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf

Persist exact TASK.md and RESULT/RECEIPTS in this ID's task/evidence dirs.
Append R5 Architect review: both requested fixes accepted, error-code-based no-claim release not accepted.
Update all three TOPs with actual final scope:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Preserve Marketplace-only grouping/proven-origin rule, Store separation, deferred Tipax and retained issues.
Commit/push normally, local==origin/main; no amend/force/reset/stash/rebase.

Return full worker envelope with exact Base/Code/Evidence SHA, focused proof and deployed runtime/build identity.
Do not add another automatic task. Do not claim User/Visual/Architect ACCEPT.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
