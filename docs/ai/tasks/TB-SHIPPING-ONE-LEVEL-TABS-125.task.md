PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-125
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-124
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ORDER_SHIPPING_GROUP_PERSISTENCE_AND_SETTLEMENT_DESIGN_REVIEW
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: f75d9c0001f92c159f8536415c69b9ff6af1f418
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD; target completion <=30 minutes. Stop investigation/design by minute 35, reserve remainder for concise evidence and handoff. No loops, repeated test runs or broad sweeps.

ARCHITECT RULING
TB-124 proved shipping is CART-WIDE, one ShippingMethodCode/amount/date/window per CartShippingDraft; checkout and Fulfillment cannot preserve multiple item-selected methods. The mandatory architecture source is docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md, including its durable multi-seller / multi-method order grouping and separate per-group shipping-fee / seller-share / marketplace-share / carrier cost / tax / discount / subsidy / settlement clauses. Do NOT implement a frontend-only grouping. This task is DESIGN/READ-ONLY ONLY. No source, migration, appsettings, feature flag, schema, API or production runtime edits. No test execution unless an essential narrowly focused check is required for an otherwise unresolved design fact.

PREFLIGHT — FAIL CLOSED

Claim one Pending Bridge row on tooba-main; Working heartbeat; preserve this task receipt UNSTAGED. Read AGENTS.md, pipeline rules, TB-124 evidence, and the FULL mandatory checkout architecture contract.
Fetch origin/main; expected base must be ancestor. Distinguish concurrent docs-only changes from source overlaps. Any overlapping source/contract change or ambiguous drift => RECOVERY_CONFLICT/STOP. Never reset, stash, rebase or force push. Leave pre-existing dirty admin frontend screens and evidence untouched.
Shared :5088/:3000, tooba_alpha and containers strictly untouched. No process launch/restart/kill, DDL/DML, API mutation, seed or scratch startup.

MISSION A — MAP REAL AUTHORITIES (MAX 15 MIN)
Read only the necessary implementations and existing persistence mapping/migration surfaces for:

Cart/Order: CartShippingDraft, CheckoutGroup, OrderLine, SellerOrder, checkout selection requests/projections, payment draft and order conversion; identify current keys, columns, FK/tenant guards and conversion boundaries.
Fulfillment: FulfillmentUnit, Shipment, OrderFulfillmentBridge, method binding, seller/origin/eligibility boundaries, shipment/item membership and state machines. Establish whether multiple units/shipments per SellerOrder are legal TODAY, not guessed.
Money: shipping quote/rate/fee authority; Pricing/Tax; Settlement, Wallet and Payment contracts, ledger or payable/receivable mappings, refund and idempotency. Explicitly distinguish shipping CHARGE to customer from actual CARRIER cost, SELLER share and MARKETPLACE share.
Current storefront components/CSS ownership for Shipping/Payment and existing responsive RTL tokens. Record references; no UI edits.
Stop discovery as soon as authoritative paths sufficient for a smallest coherent design are known. If more than one owner can plausibly own the same persisted state, mark a decision point for Architect instead of assuming.

MISSION B — PRODUCE ONE IMPLEMENTABLE ARCHITECTURE DESIGN (MAX 20 MIN)
Write an explicit minimal, owner-correct, migration-aware design, NOT code:

Define lifecycle and stable identity of shipping groups for draft, accepted order, fulfillment and settlement; tenant + order + seller + fulfillment-origin constraints; group membership by OrderLineId and quantity (handle partial quantity or mark a bounded decision).
Propose exact aggregate ownership, DTO/Contracts transition, command/query flow and immutable order snapshot. Describe how per-item customer choice produces groups and how per-seller/warehouse/capability restrictions prevent invalid merges. Show invariants for single-seller multi-method, multi-seller same-method, incompatible origins, missing method and idempotent conversion.
Detail MINIMAL tables/columns/indexes/unique/FK/constraints and module-owned migration sequence needed, differentiating confirmed existing schema from proposed schema. Backward compatibility for existing cart-wide and historical single-method orders; no destructive overwrite, no cross-tenant relation, no Host-owned persistence.
Financial breakdown per shipping group: customer shipping charge, carrier payable, seller allocation, marketplace allocation/subsidy, discount funder, shipping tax, currency, quote/policy version, final charge vs actual cost adjustment, immutable snapshots, reversals/refunds and ledger linkage. Propose a precise accounting reconciliation equation or clearly typed balanced journal entries. No invented commission percentages; Settlement owns allocations/ledger, Order owns quote/accepted snapshot, Fulfillment owns execution cost updates.
UI design requirement: reuse ACTUAL existing storefront components/CSS/tokens and RTL/responsive behavior; only minimal scoped group wrapper styling. No global CSS duplication/redesign. Per-group item/fee/ETA and reconciled total display.
Split a delivery plan into at most 3-5 bounded implementation slices, each <=45 minutes with only necessary focused tests and stop-for-architect. Prioritize a vertical end-to-end usable path; do not create a multi-week analysis backlog.
Decide independently on TB-123 tax scratch blocker: the sanctioned workspace-live-shirt (standard category with IR-NAT/IR tax rule) is a SOURCE-BACKED suggested fixture, not runtime-proven. Do not change tax categorization or create rules now. Record whether this fixture is sufficient for a later isolated browser acceptance.

MISSION C — DECISION RECORD AND STOP
Create docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-125.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-125/ (small files: authority-map, schema-contract-proposal, money-reconciliation, bounded-slices, result). Cross-reference the locked architecture contract. Do not modify the locked contract without separate approval. Clearly label VERIFIED EXISTING versus PROPOSED. Name genuine unresolved decisions and smallest first implementation task after architect review.
Commit and push ONLY task-owned docs/evidence on main, verify HEAD == origin/main. Bridge task receipt remains UNSTAGED. If proper data and schema authority cannot be established in budget, return PARTIAL/BLOCKED with narrow missing facts and STOP.

FORBIDDEN
No design-by-UI shortcut; no one-method-per-order assumption; no seller-share folded into product commission; no marketplace share inferred from customer fee alone; no direct SQL write; no mixing Order and Settlement persistence authority; no generic Host DbContext; no fake identity; no migration coding; no shared process/database operations; no broad test suite; no iterative test/fix loops; no auto-next-task.

ACCEPTANCE
PASS means the proposal is grounded in existing entities/DB mappings, explicitly specifies per-item/group persistent data and settlement allocation, preserves modular owners and certified rules, includes compatible migration and storefront CSS continuity, and is ready for Architect approval. PASS does NOT mean implementation or commercial readiness is completed.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Bridge-Task-Row, Status PASS/PARTIAL/BLOCKED, Stop-State STOP_FOR_ARCHITECT, Expected-Base-SHA, actual Base-SHA/drift, Code-SHA NONE, Evidence-SHA, measured minutes, verified current limitations, exact proposed schema/ownership, allocation and reconciliation rule, unresolved decisions, documentation changed paths, tests (ideally zero), shared non-interference, rollback and first proposed implementation slice. automaticNextImplementationTask NONE.
STOP FOR ARCHITECT.
END_TOOBA_TASK
