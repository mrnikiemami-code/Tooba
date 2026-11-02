PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R2
Parent-Task: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — close executable completion and operation-state paths
Mode: PROVE_COMPLETION_TO_CREATE_AND_UNIFY_MUTATION_ACTION_GATES
Expected-Base-SHA: 84cae82b2cdc5a5f142b850b9fca9a0c352a08a4
Status: ISSUED
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1. Deliver the working path, not another structural PASS

Continue the SAME delivery. 249-R1 is Architect PARTIAL after independent source review. User authorizes sequential correction tasks until usable handoff. One task at a time, result then Architect review. No test-account request, real carrier calls, real user order fulfillment/payment/cancellation or screenshots.

Read full 249/249-R1 tasks, current AGENTS/module instructions and code. Preserve this task verbatim. Canonical SarvNewVer FE3000→Host5088 / SingleStore/store-alpha/tooba_alpha remains the delivery, not another port/DB.

2. Source-confirmed blockers at 84cae82b

A. Completion can never unlock:
OrderCommittedCarrierShipmentFactsReader returns package_completion_required when accepted package provenance is incomplete. ShippingCarrierExecutionReadiness adds facts.IneligibilityReasonCode before reading completion, then keeps that reason even after packageReady=true from stored completion. hardReasons therefore still blocks CanCreate and EvaluateCreate. Fix resolution of completable reasons AFTER deriving effective facts. Immutable missing routing/provider/owner identity must remain blocked, not healed by package completion.

B. Track/Cancel gates disagree:
Readiness disables these for inactive accounts, but ShippingCarrierShipmentDirectory.TrackAsync/CancelAsync only checks context/Username, not IsActive or shared operation-state permission. Direct HTTP can bypass the UI condition. Apply one authoritative operation-level gate in capabilities, list/get projections AND mutations. Unknown may track for reconciliation but cannot blindly cancel/create. Cancelled/terminal/provider state must be explicit.

C. Sender completion is not authoritative:
ResolveSenderForCreate prefers requested sender fields over configured/completed values. Do not allow Create to silently rewrite configured sender identity/address. Separate authorized durable completion/update from execution, resolve authoritative sender first, validate accepted origin. Do not use carrier account DisplayName as a person's sender name unless that field is explicitly configured as sender authority.
Persist the exact execution input before external side effects, not only sender completion after provider returns; unknown/crash reconciliation needs durable provenance.

D. Order eligibility remains insufficient:
Facts reader still only blocks Cancelled and three payment-lock statuses; all remaining statuses have no fulfillment/remaining-quantity/reservation check. Reuse real fulfillment capability/quantity policy; prove shipped/delivered/returned/otherwise exhausted groups cannot create another carrier operation. Do not assume every committed or non-pending seller order is eligible, and do not invent a blanket paid-only rule.

E. UI request and feedback defects:
Shared UI Create sends empty GUID account and service "ignored" to the existing DTO. Use a dedicated locator/intent request through actual endpoint validators; remove obsolete protected fields from current request shape or make compat semantics explicit. End-to-end listener/delegate validation must prove no placeholder bypass.
Create/track/cancel handlers have finally but no catch, so network exceptions have no owning visible action error. Surface typed localized action failure without losing completion input.
refresh depends on status and messages; setting action status triggers effect rereads. Separate action state from refresh dependencies; sequence/abort guards must prevent late refresh replacing a newer identity/state. No cascading reads on every feedback change.
hasEditableSenderPostal can hide missing postal field once other contact fields are complete; make required/optional postal policy explicit for the provider and keep genuinely missing required fields editable. Never copy recipient postal.
Render all shipment states localized, not raw enum names/codes.

3. Implementation constraints

Fix these owning seams, do not redesign modules or reopen unrelated UI.
Preserve the accepted customer method/account/service/money and exact existing group membership. Manual/Free use existing transport workflows; no label heuristics.
Legacy missing provider/routing identity is explicitly unsupported unless an authorized operational mapping can preserve accepted choice without repricing; do not claim all old orders are completable.
Multi-parcel/remaining-line behavior from parent tasks remains binding: either real owned representation with quantity accounting, or explicitly report unmet scope; one operation key for the entire group is not proof of arbitrary parcel splitting.
Use Contracts only across owners, additive migration only if necessary, no foreign DbContext or hardcoded business facts.

4. Mandatory behavior proof (small, real integration)

The previous 44 unit + 4 decoder tests did not establish the working completion path.
Use real Order/Shipping services and identified disposable PostgreSQL; controlled provider HTTP only at outer boundary.
Prove:

Incomplete physical package → save completion via actual handler/validator/store → GET capabilities now ready → Create succeeds once using saved package, immutable Order evidence unchanged.
Same setup with missing accepted routing/account identity stays blocked with an exact reason, zero provider calls.
Configured sender cannot be overwritten by Create; lawful completion persists independently and survives reload/failed execution.
Inactive/unconfigured account and invalid operation states: capabilities/list/get and direct Track/Cancel/Create agree; zero outbound mutation when denied.
UNKNOWN tracks once for reconciliation, no fresh create; cancelled/terminal/exhausted group cannot execute; foreign owner denied.
Same-tick concurrent Create produces one outbound request; durable pending input exists before provider receives request.
Actual frontend request passes current endpoint validation without empty account/ignored service placeholders.
Component interaction: completion → ready → create/track state; network failure visible, typed input preserved, no status-driven fetch loop, stale response rejected.
Existing manual shipments, payment/returns and summary remain intact.

Do not fake the entire readiness or Order facts reader in the positive integration path. Unit tests remain useful for individual policies but cannot replace this proof.
Run focused relevant checks once after corrections and affected build/typecheck. No broad replay loop.
If no relational proof can run, report PARTIAL and exact environment failure; no all-complete claim.

5. Canonical runtime verification

Inspect DB/edition/source/process identity and preserve user edits/tabs/keys. Apply necessary additive migration via official runner after private backup; no reset/reseed/hand SQL.
Deploy corrected canonical frontend/backend 3000/5088, no alternate target.
Authorized READ-ONLY inspection of user's order 01a185a2-2e5a-7000-a3b4-eee8c3710158 (if present) must show accepted method/normal existing shipping controls and correct payment block, no detached raw form. A401 unauth response is an auth check, NOT proof the authorized screen works.
Do not acquire credentials by bypass, inject identity, mutate/fulfill user's order or contact Tipax. Use existing sanctioned authorized session/API mechanism; if unavailable disclose that UI acceptance limit.
Preserve local working captured-preview state and Manual/Free, checked-in defaults unchanged.

6. Time, docs and final

Checkpoint first10 minutes: exact plan + estimate; checkpoint25 minutes: completed path and remaining work, cumulative time. Do not repeatedly certify partial scaffolds.
Correct 249-R1 worker PASS append-only to Architect PARTIAL and document above defects. Update all3 recovery TOPs and plan with measured state.
Evidence/task:
docs/architecture/evidence/TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R2/{TASK.md,RESULT.md,RECEIPTS.md}
docs/ai/tasks/TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R2/TASK.md
Normal commit/push, no force/reset/amend/discard of user work. Actual Base/Code/Evidence SHA and runtime provenance required.
PASS requires mandatory working-path proof and canonical deployment, not just new interfaces/decoder tests. Only external Tipax acceptance may remain deferred.
Return BRIDGE-WAKE-V1 result with exact stage, results, migration ledger, actual timing and any unmet scope.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.
END_TOOBA_TASK
