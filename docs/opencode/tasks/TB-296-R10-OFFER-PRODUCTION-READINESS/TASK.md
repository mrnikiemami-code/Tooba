# TB-296-R10-OFFER-PRODUCTION-READINESS
Base: 295d8241d277f4320f90ee9e5e75e907dfa5905f
Branch: opencode/iso-agents-002
Mode: IMPLEMENT + VERIFY — STOP_FOR_ARCHITECT
Goal: production-ready existing single-offer workspace, ready for owner UAT and subsequent financial phase. Preserve R8 approved design and R9/R9-R1 working behavior. New bounded R10 replaces proposed R9-R2.
Authority: docs/architecture/TOOBA-OFFER-COMPLETION-MASTER-PLAN.md; docs/opencode/ARCHITECT-QUALITY-GATE.md; docs/opencode/ARCHITECT-DELIVERY-METHOD.md §§0A,1A,5; existing locks.

## Connected waves within one task
A. Read actual source, map owning contracts and policy gaps; establish exact baseline violations, architecture plan, active OpenSpec and delivery manifest before edits.
B. Implement authorized channel, inventory provisioning, history, atomic concurrency and unit contracts; no invented commercial policy. Continue independent safe slices under §0A.
C. Integrate and refactor existing single-offer workspace and minimal shared localization; no redesign.
D. Execute focused tests, then full connected matrix; one consolidated repair and final rerun maximum. No automatic next task.

## R10-01 — Channel
Offer-owned allowed-channel query based on existing edition/role/seller policies. Enum Direct/Marketplace/Agency/Corporate/Affiliate/Api is vocabulary, not permission. Same policy in query, creation and duplicate lookup; carry explicit channel through DTO, intent and reread; no fixed Marketplace. Preserve immutable channel after creation. SingleStore hides marketplace-only fields. Undecided channel policy is a scoped decision, never invent grants.
Mandatory cases R10-01-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## R10-02 — Create workspace
Enable supported editable tabs once valid identity selected, before persistence. Hold price, inventory, shipping and conditions locally; explicit save only. Initial Offer-owned fields use existing CreateOfferCommand contract. Never auto-activate incomplete listing. History honest empty until persisted; discounts and Stage3 remain roadmap gated.
Mandatory cases R10-02-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## R10-03 — Inventory provisioning
Inventory-owned eligible warehouse read and idempotent position provision for persisted offer; pre-create selects actual permitted locations and drafts quantities; no fake StockItemId. Provision after Offer created, then set physical by location. Respect owner/active/edition, reserve floor, unit policy; reserved and available read-only; no total-stock mutation or implicit warehouse creation.
Mandatory cases R10-03-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## R10-04 — Shared persistence
One shared admin/seller create/apply and edit use case with typed per-domain outcomes. Full reread matches exact seller, offer ID, variant, channel, status, SKU, amount/currency, return, min/max, fulfillment and positions. Partial create must recover persisted ID and retain unapplied edits; no blind duplicate. Unknown response requires reconciliation before retry. Actor/tenant/owner scoped draft storage; clear committed/discarded draft.
Mandatory cases R10-04-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## R10-05 — Atomic concurrency
Real owning-module compare-and-swap or atomic transaction protection for Offer/Pricing/Inventory and affected Fulfillment. Pre-read is not atomic. Independent domain transactions and honest partial outcomes; no foreign DbContext or global transaction. Investigate existing persistence primitives; if schema change required prepare module-owned migration plan and obtain required authorization before application; disposable DB only if authorized. Never run live migration.
Mandatory cases R10-05-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## R10-06 — History
Inspect actual Offer events/audit/outbox and Pricing/Inventory/Fulfillment contracts; authorized read projection of recorded changes, timestamps, actor where recorded and old/new when reliable. No fabricated historical records. Record new changes in owning module with reliable transaction/event pattern if needed. Honest empty/unknown actor, pagination; no generic Host audit authority.
Mandatory cases R10-06-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## R10-07 — Unit and conditions
Catalog authoritative unit, precision, step and rounding; create/edit and inventory client/server validation; integer/fractional quantities, min/max, reserved floor. Existing return policy and days, clear limits, lifecycle. No placeholder or raw technical label.
Mandatory cases R10-07-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## R10-08 — Price and shipping
Actual base price/currency and permitted fulfillment responsibility/type; no blank-to-zero; IRR precision; locale independent of currency; respect existing market/channel. Seller cannot mutate admin-only settings. Unsupported Stage3 service, funding discounts and multi-currency remain gated; no fake control. Preserve other shipping flows.
Mandatory cases R10-08-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## R10-09 — Central locale
Remove hardcoded ru prefix and unsafe cast; use actual registry UrlPrefix/code/culture/direction/calendar/active/default through canonical shared provider/resolver and public Localization CQRS. Minimal shared integration for offer routes and controls; no feature-local locale authority, query hack or whole-app translation rewrite. Explicit resource fallback. No live registry mutation.
Mandatory cases R10-09-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## R10-10 — Frontend architecture
Fix Offer-owned flat admin API/intent and seller transport boundaries in canonical feature structure; avoid cycles/deep imports and dumping-ground exports. Routes compose, typed adapters transport, use cases orchestrate, hooks own state, components render. Reuse AppDataGrid, Toast, MessageBox. Existing <=800 new file and shrink-only legacy guard, cohesion not cosmetic split. Preserve R8 visual design. No guard/baseline changes.
Mandatory cases R10-10-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## R10-11 — Backend and Host
Apply all eight AQ gates: owning Contracts, CQRS MediatR, Result, validators, resources, namespaces, folder and DI, permissions and transactions. Thin endpoints, no foreign DbContext or Host business logic. Preserve public language minimal DTO. No unrelated AMSC.
Mandatory cases R10-11-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## R10-12 — UX and feedback
Shared Toast 3000ms success, shared MessageBox operational failure/duplicate/unknown/dirty exit, inline field errors; no duplicated banners or raw IDs/errors. Preserve R8 layout, both grids, correct RTL/LTR and accessibility; clear obsolete errors upon correction.
Mandatory cases R10-12-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## R10-13 — Performance and regression
Comparable actual served-build form and picker measurements versus R8 reported 1598/103 ms, define warm/cold, multiple samples median/p95 and evidence generator. Diagnose unexplained regressions. Preserve orders/products/campaigns, Stage2 cart, grid sort/filter/paging/export/saved views; real assertions, not count-only. Responsive 360/768/1440; no screenshot ceremony.
Mandatory cases R10-13-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## R10-14 — Evidence and closure
Read source, OpenSpec and AQ; architecture plan and delivery manifest before code. Full frontend architecture, build/typecheck/lint, focused owner backend and mounted tests, real Chromium→Host→isolated PostgreSQL→GET/SQL for persistence. Record identity, actors, request counts and before/after; separate R10 evidence, no overwrite R9/R9-R1. Finally cleanup and verify restore. Checker rejects missing test/wrong fact/hash tamper. STOP_FOR_ARCHITECT; no TB-297.
Mandatory cases R10-14-P/-N/-B in ACCEPTANCE-MATRIX.json; each compound case must evidence every stated assertion, not just one. Additional material source branches must be covered, never silently waived.

## Scope boundaries
Only Offer-owned defects and minimum owning/shared dependencies needed for Offer. No whole-project refactor; unrelated old guard failures report exact baseline/candidate sets. No financial funding, commission/settlement redesign, bulk workspace, design pilot, invented discount or Stage3 policy. No unauthorized schema or live data mutation. Missing endpoint with decided behavior is implementation work. Genuine undecided policy or unapproved migration is scoped blocker; record exact authority and affected tests; continue independent work. Never label full readiness if mandatory material requirement remains blocked or unrun.
## Final gate
Evidence only under R10, source/build/DB identities, all AQ results, actual timestamps and duration. Worker PASS is not architect acceptance or owner UAT. STOP_FOR_ARCHITECT; TB-297 not started.
