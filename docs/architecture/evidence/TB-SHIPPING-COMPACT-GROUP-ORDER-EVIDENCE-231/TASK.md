PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231
Parent-Task: TB-SHIPPING-STOREFRONT-READ-FAILURE-INTEGRITY-230-R1
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Mode: EXISTING_ORDER_GROUP_COMPACT_CAPTURE_EVIDENCE
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
HARD35min / source stop22 / final13 proof+recovery+commit. One correction.
Do not turn this into full checkout/parity, UI redesign or another quote/calendar helper.

FIRST USER HANDOFF (bounded, no new setup)
230-R1 accepted FOR READ-ONLY user retest; user Visual ACCEPT pending.
Reverify current local preview runtime and normal test cart/account, no rebuild/restart/migration.
If feasible within first3min open ONE VISIBLE installed Chrome window using ONLY the existing task-owned
tb229r2-chrome profile/session on /fa/shipping, so user can actually inspect the real configured card.
Use existing normal logged-in session and current task cart; no password/token in message/git.
If the profile is already open by a headless task process, stop ONLY that verified profile's own process
before launching it visibly; never a user browser/kill-by-name. Never enumerate/select user tabs.
Clearly mark this as the dedicated REVIEW window. Once handed to the user, stop automating/reloading it.
Do not change their destination/address/cart/rates while they test. If visible handoff unavailable, report
normal URL and exact limitation, do not spend time writing another browser driver.
All subsequent231 proof uses an isolated test DB/context, not their review window/runtime.
Keep local Host/FE and previewON/count7 as-is. DefaultOFF/not_ready/TB185paused/PendingAllocation preserved.

READ TOP / INVARIANT
Read230-R1 ARCHITECT-REVIEW, section29 and
docs/architecture/shipping/SHIPPING-PUBLICATION-AND-COMPACT-ORDER-EVIDENCE-DECISION-2026-10-26.md.
Owner's requirement: historical shipping amounts must not change after admin edits, while DB volume stays small.
One shared immutable Shipping capture perowner+draft+revision. Each order GROUP stores only compact refs and
accepted monetary/receiving facts, NOT all authored rows/regions/translations/week/JSON for every order/line.
Current server not_ready guard stays;231 does NOT accept browser quote payloads or enable captured checkout.

CONCRETE CHANGE — EXISTING ORDER GROUPS, NO PARALLEL HISTORY TABLE FRAMEWORK
Inspect/reuse:
Order.Domain/Checkout/CartShippingDraftGroup.cs;
Order.Domain/Checkout/OrderShippingGroup.cs and OrderShippingGroupMaterializer.cs;
OrderShippingGroupPlan and existing StorefrontShippingService.BuildOrderGroupPlans;
existing OrderDbContext group configuration/migrations/persistence boundary.
Add ONE cohesive Order-owned typed compact evidence value, optional for legacy groups, carried through the
EXISTING draft-group -> plan -> immutable order-group materialization path. No Shipping.Domain/Infrastructure
reference, no new quote authority, no broad branch fork.

Minimum contents (reuse existing group fields instead of duplicating them):
- full logical shipping owner(identity, not user), stable method service/option (existing fields);
- exact selected SnapshotId, DraftId, SourceRevision, FormatVersion and SelectionVersion;
- actual matched Shipping areaGroupId and RateRowId; optional assigned type id if needed for provenance;
- accepted freight and packaging separately, exact currency, payable-at-checkout vs carrier-collect amounts;
- accepted receiving date/start/end and method timezone (reuse existing selected date/window where coherent).
Preserve actual accepted minimum/maximum CUSTOMER delivery-day metadata if needed, never map to LeadDays/
seller preparation. Existing scalar ShippingChargeSnapshot must NOT silently be reinterpreted as total cost
vs checkout-payable: document current semantic and keep compatibility until consumer parity selects the
correct fields. Do not change Payment/Settlement payer math in this slice.
No raw payload, entireweek, geographic trail, all rate conditions or translations copied into evidence.
No amount/duration recomputation or supplier/funder guessing. Free/CarrierApi are still unavailable.
Accepted values originate from a future trusted server quote/revalidation; make mutation/constructors scoped
accordingly. DO NOT add HTTP request accepting an arbitrary accepted-evidence object from the browser.

VALIDATION / PERSISTENCE
All-or-none optional evidence: legacy absence valid; partial/mismatched owner, empty ids, nonpositive
revision/token/version, invalid enum/currency, negative money or inverted receiving interval refused before
mutation. Do not silently clamp or generate ids/default owner/timezone. Only supported capture format as
defined by existing source; unknown format refuses, no latest fallback.
Add minimal nullable owned columns in EXISTING Order group storage (draft and confirmed only where needed),
one additive migration. No cross-module FK/join, no snapshot copy, no generic event-sourced history subsystem.
Small appropriate snapshot-id index for logical reference audit if needed, no redundant broad indexes.
No existing migration edit/baseline regeneration, no legacy backfill; all legacy evidence null.
Confirmed OrderShippingGroup evidence is immutable through public API; draft replacement explicitly replaces
only its own evidence along with selected group. Existing materializer must transfer it exactly while
preserving group/line identity. Reuse existing implicit Order persistence transaction, no foreign/multi-module
transaction or new ad-hoc BeginTransaction exemption.

MEANINGFUL FOCUSED PROOF (not implementation-mirroring/source regex)
Use real existing materializer: two groups share ONE SnapshotId but have different area/row ids and accepted
amounts/date; values stay distinct after materialization and persist/readback. Membership identity unchanged.
A later draft replacement/repricing changes draft evidence only; previously materialized/DB-stored order
retains exact old snapshot reference and accepted money/date. No request to mutable Shipping draft or current
pointer is needed to display history; do not claim live historical reader integration if not implemented.
Bad/partial evidence fails without leaking partial mutation; legacy groups still materialize with no evidence.
No full authored JSON persisted by schema/mapping and evidence size is constant per GROUP, not per cart line.
Relational proof on ONE task-owned disposable PostgreSQL using the existing local image only, with fixed
expected test identity checked BEFORE DDL. Respect existing reviewed target helpers; do not relax them or use
shared5432/tooba_alpha for tests. If a new isolated Order test identity is essential keep it task-specific.
Normal Order migration chain only there, cleanup own container/port. No InMemory transaction-proof claim.
Build changed Order projects + exact new fact class; no whole119/188/194/Shipping/solution/guard wildcard loops.
If relational proof blocked, report exact gap and do not substitute or claim storage acceptance.
No Host/FE restart, shared migration apply, business write, checkout/order/payment/reservation in live runtime.

FINISH
Evidence folder docs/architecture/evidence/TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231/.
Record exact added columns/reused fields (so DB-volume cost is reviewable), nullable legacy behavior,
actual tests/provider facts, migration WRITTEN vs sharedNOTAPPLIED. Never promise byte counts without measuring.
Update ALL THREE TOP recovery files (AGENTS, START-HERE-IF-CHATGPT-IS-LOST, canonical shipping recovery):
230-R1 accepted read-only user trial, review URL/windowhandedoff, actual231 storage result, runtime unchanged,
NEXT selected-revision/cart/Offer/type/quote/date revalidation across selection/commit, still not_ready.
Keep known empty-key refresh failure-retry gap, cross-ownerlabel-map/decoder limits and OfferAPI proof gaps
named for later parity readiness; do NOT reopen them or pretend fixed by order evidence.
Preserve227mobile residual and225-R1 unrecovered userLOCALtabeditloss; no user Visual ACCEPT claim.
Exact-path scoped commit/push/fetch parity; no add-A/history rewrite/Bridge/post-result scripts.
Stop on time with truthful PARTIAL if necessary, no extra architecture audit. STOP_FOR_ARCHITECT;
Automatic-Next-Task NONE. No232envelope.
