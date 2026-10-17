PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-126
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-125
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_ORDER_OWNED_MULTI_SHIPPING_GROUP_DRAFT_IMPLEMENTATION
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 94aa51bb27336bfabea01ad6b566bd52c81b567b
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD. Aim to finish implementation by minute 32, use remaining time for essential tests, evidence, commit and report. Do not enter repeated build/test/debug loops: one initial focused run, at most one targeted rerun after a demonstrable fix. Stop PARTIAL/BLOCKED on time or authority conflict.

ARCHITECT RULING
TB-125 design review PASS is ACCEPTED as a grounded proposal, not implementation acceptance. The mandatory authority is docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md, with delivery ordering in docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md. TB-124 proved existing shipping drafts are cart-wide. Implement the FIRST tangible Order-owned persistent multi-group draft slice (M1), with minimum additive APIs and essential proof. Do not silently move to UI, Fulfillment, Settlement or production cutover. Keep Track A open: payment-draft/tax-fixture acceptance remains unfinished, not forgotten.

ARCHITECT DECISIONS FOR THIS SLICE
(a) Fulfillment origin/context MUST be represented as a future-safe typed optional field on group identity, but if no trusted origin authority exists at draft selection, do NOT forge or hardcode a warehouse. A null/unspecified origin must not cause unsafe merging; refuse grouping requiring unverified origin separation. Flag any unavoidable gap as BLOCKED_ORIGIN_AUTHORITY.
(b) V1 assigns WHOLE cart lines to exactly one group. Partial quantity splits are deferred; do not add unused split execution behavior.
(c) Commercial allocation percentages, funders and real-carrier adjustment policies are UNDECIDED. Do not invent numbers, compute seller/platform shares, or create Settlement rows in this task. Store only existing authoritative shipping quote amount and currency per group.
(d) Preserve legacy single-method drafts/contracts as compatibility shorthand and legacy accepted-order columns unchanged. Avoid breaking current storefront.
(e) Later shipping allocation ledger belongs to Settlement as dedicated shipping entries rather than changing merchandise-only SettlementEntry semantics; no ledger implementation here.

PREFLIGHT — FAIL CLOSED
Claim exactly one Pending task on tooba-main, Working heartbeat; read AGENTS.md and relevant pipeline/skills, TB-124/125 evidence and both locked documents. Fetch origin/main; ensure Expected-Base-SHA is ancestor and examine new commits plus dirty/untracked files. Accept only non-overlapping documentation/bookkeeping drift. Any overlap with owned source or uncertain history -> RECOVERY_CONFLICT/STOP. Preserve the existing two dirty admin frontend screens and other unrelated evidence; no stash/reset/rebase/force-push. Do not overwrite concurrent architect docs.

MISSION A — IMPLEMENT ONE OWNED DRAFT CAPABILITY

Trace actual Cart IDs, seller ownership and Order EF mappings BEFORE editing. Make the smallest additive Order-owned M1 migration and configuration for cart_shipping_groups + cart_shipping_group_lines (or equivalent Order-owned names consistent with conventions). Stable group UUID, CartId, seller identity, canonical methodCode plus distinct serviceCode/optionCode, trusted optional origin, server-calculated quote/currency, delivery schedule/label snapshot, state and timestamps. Group lines reference actual CartLineId and contain entire-line membership. Respect tenant DB isolation and established FK conventions; do not invent cross-module relational FKs. Enforce uniqueness preventing a line from belonging to two draft groups and valid money/identity invariants.
Extend the Order draft domain/application and only the minimum existing public Contracts with additive Groups[] selection/projection while accepting old scalar selection as a compatibility path. Server-side validation must ensure every eligible current cart line is included exactly once, group seller agrees with authoritative Cart ownership, method identity is canonical, selection is eligible for that seller/destination, and no duplicate/missing membership. Explicitly reject inconsistent or unverified data; never trust frontend labels/totals as authority.
Preserve the existing single-method Shipping -> Payment behavior, draft readback, and NO automatic shipping commit/cart clear/order creation on navigation. If actual existing DTO/aggregate cannot safely support Group[] within this timebox, deliver a coherent DB+domain slice and report PARTIAL with precise next boundary; do NOT ship a fake Groups[] UI or partially exposed route.
Keep all business logic in Order and existing module Contracts/authoritative ports; no Host business rules, no direct SQL in handlers, no new second shipping price calculator, no Fulfillment or Settlement ownership leakage. Avoid broad refactors.

MISSION B — MINIMAL PROOF
Run ONLY focused tests that show: (1) one seller with two methods => two persisted groups; (2) two sellers with identical method => separate groups; (3) every cart line belongs to exactly one group, duplicate/omitted/wrong-seller lines refused; (4) legacy one-method draft roundtrip remains valid. Include a migration/EF mapping validation if available cheaply. Test with existing fixtures; do not start a shared Host or create shared DB writes. Build changed projects once. If no adequate authoritative seller/method eligibility seam exists, stop BLOCKED_AUTHORITY rather than bypass it. No broad Host architecture suite or endless retries.

MISSION C — DELIVERY RECORD
Commit only owned Order domain/application/infrastructure/contracts/migration and focused tests, plus concise docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-126.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-126/. Do not add unrelated source, frontend, CSS, appsettings, tax fixture, production shipping flags or new processes. Keep Bridge task receipt UNSTAGED. Push origin/main normally if base remains compatible; verify HEAD==origin/main. Preserve dirty shared workspace files. Report actual minutes, changed paths, Code-SHA/Evidence-SHA, test counts, exact limits, rollback and next technical blocker. STOP_FOR_ARCHITECT.

VISUAL NON-REGRESSION LOCK
DO NOT EDIT frontend, CSS, storefront component files, templates or theme. Current Shipping/Payment appearance and RTL must remain byte-for-byte unaffected in this slice. Later grouped UI must reuse current storefront CSS/components with high visual fidelity, not redesign.

ACCEPTANCE
PASS only for a coherent, persisted Order-owned multi-group cart draft capability with authoritative line membership, backward-compatible old selection and passing minimal tests/build. It does NOT imply accepted order conversion, Fulfillment shipment grouping, per-group Settlement ledger, tax acceptance, grouped Checkout UI or commercial readiness. Return PARTIAL/BLOCKED transparently if these criteria cannot be met without scope inflation.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Bridge-Task-Row, Status PASS/PARTIAL/BLOCKED, Stop-State STOP_FOR_ARCHITECT, Expected-Base-SHA, Base-SHA and drift, Code-SHA, Evidence-SHA, actual minutes, changed paths, migrations/contracts, focused test counts, shared-state non-interference, deviations, rollback and exact first remaining blocker. Automatic-Next-Implementation-Task: NONE. STOP FOR ARCHITECT.
END_TOOBA_TASK
