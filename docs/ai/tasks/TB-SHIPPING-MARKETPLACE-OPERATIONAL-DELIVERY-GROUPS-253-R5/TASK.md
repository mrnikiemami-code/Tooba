PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R5
Parent-Task: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R4
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — close the two remaining uncertain-operation recovery paths
Mode: BLOCK_UNRESOLVED_REMINT_AND_RECONCILE_PARCEL_BEFORE_REMAINING_GATE
Expected-Base-SHA: 1dfdecbaf1365c21fbea86ab13922f63b358b74d
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1. Exact bounded review

R4 durable full envelope, explicit reuse-only execution, canonical migration/deploy and recovery updates are retained.
Architect still marks recovery PARTIAL for TWO reproducible source paths below.
Fix these, demonstrate the precise failures RED→GREEN, deploy, update recovery and stop.
No new product requirement, visual redesign, additional provider work or general audit.
Read relevant AGENTS/feature guidance; preserve all prior accepted ownership/origin/financial rules and unrelated edits.

2. Defect A — unresolved attempt can silently mint a second operation

Files:
app/admin/operational-group-occurrence-store.ts
app/admin/admin-order-operational-delivery-section.tsx

resolveOperationalGroupAttempt currently mints a NEW occurrence whenever the current request fingerprint differs from the stored unresolved attempt.
The normal Create button remains enabled when retainedAttempt/reconcileHint exists; it recomputes request from current quantities/reuse checks and calls that resolver.
The extra reconcile button does not close the unsafe normal-button path.
A failure can leave real parcels; an edited quantity or refreshed projection followed by the main button must never overwrite the unresolved local envelope with a new occurrence.

Required:

Until authoritative resolution, all actions for that group either replay/reconcile the retained envelope or remain disabled. No remint, no overwrite of stored request, no new operation on changed review.
Enforce at the state/resolver boundary as well as the JSX button. Callers cannot bypass by using the normal path.
Once the recorded operation is authoritatively completed, allow explicit next operation with fresh occurrence.
If an owned typed refusal proves NO claim/side effect and correction is allowed, use an explicit safe transition; do not classify network/5xx/timeout as that refusal.
Scope changes clear in-memory refs/retained state for the previous viewer; local hints are not authorization.
Preserve single-flight, exact quantities, one inline error and current compact UI. No generic reset/delete button that abandons uncertain allocations.

3. Defect B — reconciliation after Create persists but BEFORE member-link persists is unreachable

File:
MarketplaceOperationalGroupWorkflow.BuildPlan

Current sequence:
durable occurrence claimed;
_fulfillment.CreateShipmentAsync persists member A parcel with occurrence DispatchKey;
RecordMemberAllocationAsync fails/crashes before linking it;
retry reads occurrence with NO member link and facts whose remaining packed quantity is consumed;
BuildPlan tests lines against remaining quantity and throws workflow_selection_invalid;
later execution block RecordedOccurrenceParcels(...DispatchKey) is never reached.

R4's exact-key recovery code exists after this gate, but cannot run for this case.

Required:

During complete-plan preparation, before remaining-quantity checks/binder for a member lacking a durable member link, inspect exact recorded occurrence DispatchKey.
Verify recorded parcel group/method/full quantities match immutable durable envelope. Reconcile only those exact IDs into plan/link; never sweep open parcels or create replacement.
Treat mismatched recorded evidence as typed conflict without new allocation; state/lifecycle must remain lawful.
Apply whole-plan validation before writes; do not move unvalidated linking/allocation into preflight.
Keep true first allocations gated by remaining quantity and existing exact binding/metadata.
Continue to preserve distinct next occurrence, replay, actor authority, financial history and explicit reuse.

4. Only the missing focused proof

A. Real mounted React + resolver:

partial server progress, network/5xx response loss;
edited quantity/reuse set and/or new projection, then invoke normal Create path (not only reconcile button);
assert retained occurrence+full envelope unchanged; no second fresh operation/network payload;
remount still reconciles same envelope even if creation readiness false;
authoritative success → deliberate next action uses fresh occurrence;
viewer switch cannot act on prior retained refs.
No source-string/static-only substitution.

B. Disposable PostgreSQL real workflow:

use an interceptor/fault seam failing EXACTLY on Order member-link persistence AFTER Fulfillment Create has committed but BEFORE RecordMemberAllocation persisted;
assert 1 occurrence-keyed parcel exists, NO member link for A, 0 package at failure;
retry original envelope recovers exact A ID, creates only genuinely missing B, records links, exactly one package;
changed request on same occurrence conflicts and adds zero rows;
exact parcel evidence mismatch fails closed, no replacement.
Do not substitute failure before common-package persistence AFTER all links were recorded: that is a different already-proven case.

Run new targeted cases and adjacent affected tests once; typecheck/build and Host publish. No full matrix repeated six times or unrelated suites.
Do not amend tests to permit remint or drop the exact fault checkpoint.

5. Deploy and preserve

Deploy corrected original SarvNewVer code on canonical FE3000→Host5088 SingleStore/store-alpha/tooba_alpha.
Preserve DataProtection, applied migration 20261108153000_AddOperationalGroupOccurrences and local CapturedPreview Enabled/SelectionEnabled/CheckoutEnabled/AvailableDateCount=7; checked-in OFF.
No new migration expected; if genuinely necessary, private backup + scoped runner/readback, no manual DDL/reset/reseed/history rewrite.
No real user order/cart/pack/create/dispatch/cancel/payment/merchant writes. Controlled disposable proof only.
No screenshots or Tipax live calls/accounts. No alternate final runtime/edition/database.
Read-only authorized review:
http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf
Keep existing professional layout; Store groups remain separate.

6. Recovery and completion

Task/evidence dirs for this ID: exact readable TASK.md, RESULT.md, RECEIPTS.md.
Append concise R4 Architect correction: durable-envelope/reuse improvements retained, unresolved-remint and create-before-member-link recovery incomplete.
Update all three TOPs:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Record two exact executed regressions, final deployed SHA/build and honest limitations. Preserve prior history, Tipax deferred, TB185 paused and known retained issues.
Commit/push normally, verify local==origin/main, no amend/force/reset/stash/rebase.
Return full worker envelope with Base/Code/Evidence SHA and canonical review URL.
PASS only for actual repaired paths + deployed version; no Architect/User Visual ACCEPT claim.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
