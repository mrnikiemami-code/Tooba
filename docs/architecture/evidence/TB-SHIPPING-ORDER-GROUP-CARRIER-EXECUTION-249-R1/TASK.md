PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R1
Parent-Task: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — close independently reviewed group-execution defects and deliver canonical order UI
Mode: UNIFY_SERVER_READINESS_AND_CORRECT_GROUP_EXECUTION_INPUTS
Expected-Base-SHA: c30817c6a88dc2ada5b13c0813491a355b72b030
Status: ISSUED
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1 — Binding outcome / continuation authority

User authorizes continued repair/review cycles until a usable delivery, without asking again after each worker result. One worker task at a time; return STOP_FOR_ARCHITECT for independent review. Architect may issue the next correction from actual findings; worker must not self-issue parallel work. Do not interpret automaticNext NONE as withdrawing user's continuing authorization.

249 is Architect PARTIAL despite worker PASS. Fix actual reviewed residuals below, finish 249's unmet functional scope and deploy on the SAME canonical SarvNewVer FE3000 → Host5088, SingleStore/store-alpha/tooba_alpha. Preserve Marketplace platform/seller scope.

No Tipax credentials/test account request, real carrier call, real shipment/order fulfillment/payment action or screenshots. External provider verification is deferred by user, not a blocker to software completion.

2 — Directly inspected defects at c30817c6

Read AGENTS/module instructions, 249 task/evidence and these exact seams:

frontend app/admin/admin-order-group-carrier-execution.tsx
frontend app/vendor-panel/seller-order-group-carrier-execution.tsx
OrderCommittedCarrierShipmentFactsReader
ListShippingGroupCarrierExecutionCapabilitiesHandler
ShippingCarrierShipmentDirectory
existing order items/manual shipping/fulfillment workflows.

Source facts:

Sender mobile still labels messages.carrierUsername; sender address labels carrierOriginCity; sender name labels accountName. Labels were not actually fixed.
senderByGroup default postalCode = group.postalCode (RECIPIENT postal code); sender postal code has no editable field.
Client uses transportMode === CarrierApi OR methodCode.includes("carrier"). Remove heuristic method-name routing.
Capabilities handler simply maps Order facts; it does not resolve account activation/config version/provider configuration, existing operation state or sender readiness. Create separately checks some of these, so button readiness is misleading.
Order facts blocks only Cancelled/PendingPayment/Submitted/ReservationRequested; every other status falls through. Derive existing lawful eligibility with full fulfillment/reservation/terminal rules; do not infer any other enum value means shippable.
Sender contact/address remain client-supplied without authoritative configured sender source/completion provenance.
Unknown mapped to Unsupported; enum decoder accepts numeric states but Unknown checks only string. canTrack/canCancel come from provider capabilities rather than actual operation state.
refresh ignores shipRes.ok, can silently replace failed shipment read with empty list; initial load errors disappear because carrierGroups.length===0 returns null.
No synchronous in-flight ref; busyGroupId is one scalar and can be cleared by another group's finally. No refresh cancellation/stale response guard, no visible catch for network errors.
No package-completion path for older accepted orders: 249 result says package_incomplete until new capture. A committed order cannot be fixed by silently recapturing/repricing the customer's purchase.
Shared UI is exported as Admin component and imported into seller UI: inspect module organization, extract neutral shared execution UI rather than seller depending on admin feature ownership.
249 TASK.md copies are only short summaries (5/15 lines), not the verbatim issued task. Preserve this full task; append clarification to prior evidence without inventing lost content.
3 — One authoritative execution readiness

Implement one application-level readiness service/projection consumed by read and Create. Order owns order/group/business eligibility; Shipping owns carrier account/provider/operation configuration. Keep Contracts-only seams and correct ownership, no foreign persistence joins.

Readiness returns exact group + allowed actions + owning localized reason(s):

Correct actor/tenant/fulfillment owner and accepted method.
Existing fulfillment policy for payment/reservation, cancelled/terminal/delivered groups, quantities already allocated/shipped and permitted remaining lines.
Accepted provider/account/service/config provenance, current account active/usable, required reference bindings/callback configuration and actual sender readiness.
Existing operation Pending/Succeeded/Failed/Unknown, receipt/tracking and reconciliation constraints.
Required package and destination facts with origin vs destination separate.
Create rechecks the same authority before any external mutation. Race handling still enforced durably, not UI-only.
Do not make external HTTP calls just to calculate page capabilities.
Do not invent paid-only policy if a legitimate existing COD path is supported.
Do not silently swap accepted account/service, misroute a Manual or Free group or infer carrier from labels.
A disabled account must not silently remain usable for track/cancel; follow explicit existing safe policy and report clear reason.
4 — Authoritative sender/package facts without fabricating data

Load configured sender name/mobile/address/postal code from exact owner origin/profile through owning ports. Never copy recipient postal code. If actually absent, allow explicit authorized completion, correct field names, validation and durable provenance. No credential fields in order UI.

Use accepted package facts; no constant defaults, guessed weights, product rewrites or invented value.
For legitimate missing package facts on old orders, provide an authorized operational package-completion path without repricing/changing immutable accepted order facts or silently replacing accepted carrier choice. Explicitly distinguish legacy unsupported provenance from completable physical package facts.
Retain existing multi-parcel representation if available; correctly account for group line membership and remaining quantities. If a new minimal package representation is necessary, implement within correct owner and use additive migration. Do not repeatedly submit the whole group for each parcel.

5 — Finish integrated admin/seller UI

Use repository frontend skill/design components. Existing persisted order groups and line membership are authority. Operations must be recognizable inside existing group shipping area; no detached generic form, raw group IDs, arbitrary account/service selection, technical code labels or hardcoded FA-only behavior where locale is supported.

Correct sender labels, dedicated sender postal code only when missing.
Readiness pending/error/empty/unconfigured visible and distinct.
Unknown vs unsupported vs failed localized correctly; enum values normalized at decoding boundary.
Action availability derives from server operation/group state, including existing operation; no fresh create action after already-started/unknown operation.
Track allowed for reconciliation when server permits; do not disable reconciliation solely because create result is UNKNOWN.
Per-operation synchronous click lock before await, correct per-group concurrent cleanup, bounded fetches and try/catch/finally.
Cancel/stale guard on refresh/route change, preserve typed completion data and errors.
Manual/Free existing shipping, money, payment, returns and seller sections preserved.
Seller uses neutral shared component with same lawful functionality, not admin module ownership or an empty scaffold.
6 — Focused evidence

Write meaningful behavioral/relational tests, not only source-string guards:
A. Existing user screenshot's PendingPayment order: no create; clear existing payment reason; no user mutation.
B. Eligible API group + disabled/unconfigured/rotated account/missing bindings/sender: capability and Create agree, zero external calls.
C. Same seller multiple groups/owners: exact isolation, foreign tamper refuses, correct independent operations.
D. Distinct sender/recipient city and postal codes passed correctly; no guessed values.
E. Already-created, Pending, UNKNOWN and terminal states: consistent actions; no blind duplicate create.
F. Network/list failure remains visible; no false empty shipments; numeric enum UNKNOWN normalizes correctly.
G. Same-tick duplicate and concurrent different-group actions: synchronous containment and no lock cleanup race.
H. Missing package completion on legacy order does not change accepted price/method/history.
I. Existing Manual/Free/return/fulfillment controls unchanged.
J. Unauthorized/terminal/reservation/quantity-invalid mutations refused in server before operation/provider call.

Controlled provider boundary only. Disposable identified DB for relational proof; never reset canonical data. Run affected builds/typecheck and focused adjacent tests once after corrections; no full-solution replay loop.

7 — Deploy and finish

First10 focused minutes: file-level plan and realistic estimate; checkpoint at25, one cumulative ledger, report overruns before extending. Do not replace implementation with another broad report.

Canonical source/runtime/DB identity before rollout, private backup before necessary DDL, official runner only for pending migrations, preserve DataProtection keys. No alternate ports/database/user handoff. Preserve unsaved user work and current cart/order/draft/rates.
Deploy actual corrected Host/FE to3000/5088. Inspect authorized read-only current order UI/capabilities, including user's problematic order locator 01a185a2-2e5a-7000-a3b4-eee8c3710158 when it exists. No fulfill/cancel/payment action on it.
No new credentials or real carrier calls. Existing runtime Manual/Free/captured preview stay working.

8 — Recovery and final result

Append correction that 249 worker PASS was Architect PARTIAL with the above source-confirmed gaps.
Update AGENTS + both recovery TOPs + carrier plan honestly. Never report "external test only remains" if any software/UI/deploy item is still missing.

Task:
docs/ai/tasks/TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R1/TASK.md
Evidence:
docs/architecture/evidence/TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R1/{TASK.md,RESULT.md,RECEIPTS.md}
Preserve full task verbatim.

Normal commit/push, no force/reset/amend/stashing user changes. Reconcile advanced main carefully.
Return full BRIDGE-WAKE-V1 result with Base/Code/Evidence SHA, migration ledger, canonical runtime provenance, exact user-visible stage, functional proofs, controlled/live limits, remaining gaps and elapsed time.
PASS only if reviewed residuals + parent mandatory software scope are delivered and canonical deployed. Otherwise exact PARTIAL; do not blame missing Tipax credentials.
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK