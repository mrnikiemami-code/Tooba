PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-143
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-142
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_SETTLEMENT_SALES_CAPABILITY_ACTIVATION_AND_PENDING_LEDGER_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: f9bb7000853d681f35fc2e062287c95d73f44ea9
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Hard-Timebox: 45 minutes. Implementation by minute 28, focused verification and clean report by minute 45. On time expiry PARTIAL and STOP. No loops.

ARCHITECT RULING — narrow authorization
TB-142 demonstrated that an Edition=Marketplace Host migrates Settlement but lacks runnable commerce schema/fixture, whereas the SANCTIONED SingleStore scratch checkout has real Marketplace sales-channel offers, tax, Payment and Fulfillment but no Settlement handler or schema. Do NOT conflate edition and sales channel, or activate Settlement indiscriminately. Authorize a minimal capability-based handler activation ONLY for a deployment with verifiably applicable Marketplace-channel paid sale AND an explicitly available Settlement persistence schema. Retain Marketplace handler availability and SingleStore legacy semantics. No new commercial allocation policy, no seller/platform carrier split, and PendingAllocation always non-payable. Deployment-wide SingleStore registration is acceptable ONLY if handler fail-closes cheaply for non-Marketplace sales and schema is explicitly activated for a disposable test; it must not break ordinary SingleStore production payment events by throwing on missing Settlement tables. If this cannot be achieved safely inside the box, STOP PARTIAL and present a precise architect decision; do not force it by an Edition swap.

PREFLIGHT
Claim one Bridge task, read AGENTS.md and locked checkout grouping contract and delivery plan; fetch origin/main, check expected SHA and protected dirty files. No reset, stash, rebase, force-push; docs-only drift tolerable, source overlap => STOP. Inspect actual SettlementPaymentSucceededHandler, PaymentSucceededIntegrationEvent payload, Order Settlement group reader, Payment allocation reader, SettlementDbContext resolution, MarketplaceDevelopmentBootstrap explicit migration seam and exact feature/capability gates. Authorize no edits before identifying the narrow source-owned capability check.

MISSION A — smallest safe handler eligibility
Implement a typed, owner-correct eligibility decision for a paid checkout whose sale is genuinely Marketplace-channel; derive it from an authoritative Order/Payment Contracts read, NEVER from a frontend method label, seller name, SKU, tenant edition guess or client claim. Preserve tenant scope. If existing event cannot convey/resolve channel, extend the smallest Contracts-only read seam; no cross-module DbContext. Ensure handler can skip non-applicable paid events without blocking Order/Fulfillment fan-out and without querying a non-existent Settlement relation. Distinguish handler-registration eligibility (deployment capability) from per-event sale-channel eligibility. Existing Marketplace-edition behaviour must remain intact. If introducing SingleStore handler registration, ensure missing schema cannot poison payment success through a retrying integration transport; explicit schema readiness gate and fail-closed non-payout behaviour are REQUIRED. Do not change Host business authority or automatic migration registration and do not enable Settlement globally across shared Development tenants.

MISSION B — runtime proof in disposable SingleStore scratch
Reuse TB-140 sanction: scratch-only PostgreSQL with separate tenant + messaging databases and Development fake/no-charge sandbox Payment. Never touch shared :5088/:3000, tooba_alpha, production PSP, customer payments or actual carrier services. Apply Settlement module's own migrations through the proven TB-141 explicit module seam ONLY to task-owned scratch target (guard rejects shared references/port); demonstrate MigrateAsync idempotence as already proven if reused, no extra repeated launches. Real same-seller/two-method paid customer journey with one final-click + one sandbox verification, not event injection or direct handler/ledger call. Read back: ONE succeeded Payment, TWO StoreShippingGroup allocations, ONE paid SellerOrder, ONE FulfillmentUnit with TWO identical group IDs, and TWO settlement.shipping_group_fees charge rows with same group ids and 200000/180000 customer charges, allocation_state=PendingAllocation, unknown commercial fields NULL, ZERO shipping amount in seller merchandise accrual or payout. If module cannot safely activate for SingleStore, do not run a false-positive test; report exact missing contract/capability as PARTIAL.

MISSION C — tests/evidence
Targeted tests: channel eligibility incl SingleStore Marketplace-channel vs SingleStore normal retail, handler skipping absent/disabled Settlement schema, existing Marketplace edition handler registration, idempotent event replay, positive group charges not seller credit, existing Fulfillment unaffected. Test only changed seams once with maximum one corrective rerun. Minimal docs/evidence in docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-143.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-143/. Commit/push normally, verify origin/main, report actual minutes, SHA, before/after, exact remaining commercial policy blocker. Never misstate a unit-only test as runtime proof.

RELEASE/SAFETY NON-NEGOTIABLE
No percentage/discount/funder/carrier policy invented; never mark PendingAllocation as settled, revenue, credit or payout. No schema activation on shared DB. No changes to frontend, CSS, themes, typography, cards, spacing, RTL, checkout appearance, component tokens or admin. No automatic shipment booking. Preserve the already-proven paid->Fulfillment group handoff. STOP_FOR_ARCHITECT on PASS/PARTIAL/BLOCKED.
END_TOOBA_TASK