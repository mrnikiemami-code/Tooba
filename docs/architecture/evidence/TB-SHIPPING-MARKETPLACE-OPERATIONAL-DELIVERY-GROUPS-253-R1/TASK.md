PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R1
Parent-Task: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — close operational-group execution and relational delivery proof
Mode: COMPLETE_GROUP_ACTION_HARDEN_COMPATIBILITY_AND_PROVE_REAL_CONSOLIDATION
Expected-Base-SHA: 1c9bd5d2905bb0a7cfc769efabe94b054caa9a07
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1. Decision and bounded continuation

253 is PARTIAL, not delivery-complete. Preserve its compact delivery-first composition.
Complete the existing path; do not start another UI redesign, provider integration or general audit.
The user authorizes sequential completion on their own canonical environment, with professional UI and recovery updated.

Locked product rule:
Cross-seller operational grouping ONLY under one Marketplace sending authority, proven common origin and compatible accepted service + chosen delivery date/hour. Seller/Store responsibilities remain independent. Store is not a Marketplace alias.
Financial seller orders and accepted shipping-group records remain separate and immutable. One operational group may contain multiple physical parcels.

253 original issued task remains binding where not corrected below. Read actual source and relevant governance/feature skills before coding.

2. Independently reviewed defects — repair at owning seams

A. Compatibility admits incomplete or wrong facts:
MarketplaceOperationalDeliveryGroupAuthority.AreMarketplaceCompatible does not compare CheckoutId or Currency.
ExactDispatchIdentity compares nullable date/ticks/timezone, so two absent values can compare equal.
CompatibleTransport accepts any non-CarrierApi mode except LegacyIncomplete, including unknown values.
Project then sums FeeAmount and labels the result using seed.Currency.

Repair:

Require same non-empty checkout and authorized edition/owner context.
Exact currency identity; no mixed-currency sum or relabel.
Require authoritative chosen date, valid open < close bounded receiving ticks and non-empty timezone for cross-group merge; two missing values never establish compatibility.
Explicit transport modes from owning contract, unknown/legacy fail closed.
Keep exact service/option semantics, provider binding and common proven origin. Do not normalize distinct service identities into equality.
Do not use MinimumDeliveryDate as evidence that the customer selected a day. Separate historical display fallbacks from executable accepted identity.
Build one shared mapping of committed facts/history to authority input. Detail composer and mutation currently construct it independently with different fallbacks; avoid contradictory readiness.

B. operationalGroupKey is incomplete:
BuildOperationalGroup's key excludes checkout, currency, provider/account/config and destination. Separate non-Marketplace groups can also share a key if seller/method/date/origin match.
Give every projected group a deterministic collision-resistant identity scoped to checkout and complete compatibility/member identity. Incompatible groups must never share React keys or target another group. Stable order of inputs must not change identity.
No raw key/GUID on operator UI.

C. Readiness/permissions:
ConsolidateEligible becomes true for any cross-seller non-CarrierApi group, regardless of actual shipment state or actionable permissions.
Separate compatibility from current executable readiness. Use the actual eligible remaining packed quantities/created shipments/package locks/payment state and actor permissions.
Projection, offered operation and mutation must agree for the same facts. Stale state must be rechecked inside the mutation boundary.

D. Incomplete operational action:
AdminOrderOperationalDeliverySection renders a Create button per member, opening the old one-seller modal. It never uses ConsolidateEligible for a shared action. A common heading alone does not implement the requested operational path.
Provide one contextual group-level workflow for compatible Marketplace items across sellers: review selected quantities, create/reuse exact constituent parcels and consolidate them through the existing Fulfillment mechanism where appropriate.
An existing multi-step constituent-parcel model is acceptable internally; the user must have one coherent recoverable group workflow, not unrelated seller buttons with manual ID stitching.
Keep independent seller-owned/Store group create working. Do not break existing group/quantity selections.

E. Accepted method protection:
CreateConsolidatedPackageCoreAsync checks membership compatibility but still forwards request.ShippingMethodCode.
Derive method from accepted compatible commitments, reject conflicting client method, and verify the real Fulfillment consolidated-package boundary cannot override it.
Do not weaken CarrierApi dedicated execution. Cross-group carrier consignment remains explicitly unsupported; no fake combined provider dispatch.
Preserve 252-R2 Post trusted recipient metadata and R3 fail-closed origin/collection behavior.

3. Execution and UX acceptance

Reuse existing Order/Fulfillment Contracts and package lifecycle; no cross-module DbContext joins.
Validate all constituents before allocation; preserve exact original group/line/fulfillment links and quantities.
Use a stable operation identity and transactional/idempotent recovery so double-click, concurrency, response loss or intermediate failure cannot allocate duplicate parcels or hide partial work.
Never blindly rerun successful constituent work. Do not replace this requirement with a JavaScript loop of independent writes.
If schema support is genuinely necessary, additive migration only through existing scoped runner with private backup/readback; never rewrite historical accepted groups or fees.

UI:

Keep compact «ارسال سفارش» cards, Jalali day, LTR hour, seller attribution, remaining quantity and parcel status.
One contextual action on a compatible common group; independent group action elsewhere.
Expand a short fee breakdown only when useful; plain «جمع هزینه ارسال», not technical «جمع تعهدها».
Remove routine "why Store groups are separate" technical warnings from every normal card; show an actionable blocker only when the operator needs it.
Parcel status/tracking/action target must identify each actual parcel, not merely count + first tracking reference.
Immediate action-level pending, one submission, one inline error, retained quantities on refusal/unknown outcome.
No giant controls, duplicate buttons, overlays covering content or page overflow.
Preserve seller packing controls and financial breakdown without duplicating shipment creation sections.
Languages from configured registry/resources; no new hardcoded language authority. IRR no fraction glyph; exact accounting unchanged.
4. Proof required for closure — one focused run, no broad loops

Extend authority behavior tests:
different checkout/currency/provider binding, missing chosen date/open/close/timezone, invalid intervals/unknown mode, two same-seller distinct groups, stable unique keys and order permutation.
Existing positive Marketplace pair must still group; Seller/Store stay separate; missing origin fails closed.

Provision an OWNED disposable PostgreSQL target now.
Do not stop merely because an env variable is unset; 249-R4–R7 already established disposable provisioning patterns.
Use actual scoped Order+Shipping+Fulfillment migrations and command/orchestrator→Fulfillment persistence path, not just authority stubs or InMemory.
Required cases:

compatible Marketplace cross-seller group executes coherent workflow and links precise constituent parcels/package;
Seller/Store, cross-checkout, missing origin/date, different hour/service/currency rejected before allocation;
altered client method rejected; accepted method persists;
same operation retry/concurrent twin allocates once;
intermediate allocation/consolidation failure and lost response recover using saved identity without duplicates;
quantities, original accepted snapshots/fees and seller financials conserved.
Controlled provider only. Clean only task-owned disposable resources.
Report exact executed cases/counts; no declared-but-unrun matrix.

Mount real shared React workflow:
two-seller common action and quantities; separate seller/Store cards; double-click one call; typed refusal retains input; intermediate/unknown recovery; all parcel states/tracking correctly rendered; no technical leakage.
Not just callback/body/source-string assertions.

Canonical authorized read-only acceptance:
FE3000→Host5088, SingleStore/store-alpha/tooba_alpha.
Review order 01a1b92d-e97e-7000-999a-143350d0edaf; its actual Store groups remain separate. Do not edit ownership to force Marketplace proof.
Use a controlled Marketplace fixture for positive mounted/relational proof; do not switch canonical edition/DB or create a second final runtime.
DOM layout/focus at 1440/1024/390, no screenshots. Keep existing user windows/unsaved work untouched.
No real user order pack/create/dispatch/cancel/payment or business-data edits.

Run owning focused suites, typecheck/build and Host publish once after final changes; expand only for relevant failures.
Do not treat FE route 200 or an unauth 401 as authorized UI proof.

5. Canonical deployment and preservation

Deliver from D:\Users\User\source\repos\SarvNewVer, original FE3000/Host5088 SingleStore/store-alpha/tooba_alpha.
Preserve DataProtection keys and local CapturedPreview Enabled/SelectionEnabled/CheckoutEnabled/AvailableDateCount=7 across deployment; checked-in gates remain OFF.
No reset/reseed/manual DDL/business SQL, original data edits, alternate final port/database or test buyer replacement.
Preserve adjacent deposit confirmation/method normalization/dialog-wrap changes already in current main; do not attribute unrelated changes to this task.
Live Tipax account/network still deferred by user. No provider account request or new provider feature.
Product g/cm fields preserved; never sum product L/W/H into package dimensions.
TB185 paused/default-OFF/PendingAllocation; retained known issues unchanged.

6. Recovery and return

Persist this task VERBATIM as a readable multi-line TASK.md (not one escaped JSON line) in:
docs/ai/tasks/TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R1/
docs/architecture/evidence/TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R1/

Write RESULT.md + RECEIPTS.md with exact source/evidence SHAs, runtime, migration ledger, actual proofs, controlled-vs-live distinction and remaining limitations.
Append Architect correction to 253 evidence: relational proof absent AND common workflow/compatibility/readiness gaps, rather than suggesting only missing tests.
Update all three recovery TOPs:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Keep Marketplace-only rule explicit. No Store promotion, no historical fee rewrite.
Commit/push normally, verify local==origin/main and safe tree; no reset/stash/rebase/amend/force.
Return complete BEGIN_TOOBA_WORKER_RESULT envelope and usable canonical review URL.
PASS requires executable end-to-end behavior and executed relational proof; otherwise accurate PARTIAL/BLOCKED.
No user/visual/Architect ACCEPT claim. Record elapsed time honestly.
STOP_FOR_ARCHITECT.
automaticNextImplementationTask: NONE
END_TOOBA_TASK