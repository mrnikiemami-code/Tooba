# Offer production readiness — R10

## ADDED Requirements

### Requirement: R10-01 — Channel
The R10 workspace and its applicable owning contracts SHALL satisfy the following: Offer-owned allowed-channel query based on existing edition/role/seller policies. Enum Direct/Marketplace/Agency/Corporate/Affiliate/Api is vocabulary, not permission. Same policy in query, creation and duplicate lookup; carry explicit channel through DTO, intent and reread; no fixed Marketplace. Preserve immutable channel after creation. SingleStore hides marketplace-only fields. Undecided channel policy is a scoped decision, never invent grants.

#### Scenario: R10-01-P — positive
- **GIVEN** eligible identity
- **WHEN** select allowed channel and save
- **THEN** one Draft with exact selected channel
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-01-N — negative
- **GIVEN** unauthorized channel
- **WHEN** bypass UI with API
- **THEN** typed denial, zero writes
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-01-B — boundary
- **GIVEN** same variant in two independently allowed channels; changed channel with same intent
- **WHEN** submit separate and conflicting requests
- **THEN** distinct authorized offers; intent conflict on changed payload
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

### Requirement: R10-02 — Create workspace
The R10 workspace and its applicable owning contracts SHALL satisfy the following: Enable supported editable tabs once valid identity selected, before persistence. Hold price, inventory, shipping and conditions locally; explicit save only. Initial Offer-owned fields use existing CreateOfferCommand contract. Never auto-activate incomplete listing. History honest empty until persisted; discounts and Stage3 remain roadmap gated.

#### Scenario: R10-02-P — positive
- **GIVEN** valid identity and populated draft
- **WHEN** switch tabs and save once
- **THEN** one durable Draft with requested fields
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-02-N — negative
- **GIVEN** invalid draft
- **WHEN** attempt save
- **THEN** validation before writes
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-02-B — boundary
- **GIVEN** double-click and concurrent create
- **WHEN** submit same intent
- **THEN** one offer; deterministic replay
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

### Requirement: R10-03 — Inventory provisioning
The R10 workspace and its applicable owning contracts SHALL satisfy the following: Inventory-owned eligible warehouse read and idempotent position provision for persisted offer; pre-create selects actual permitted locations and drafts quantities; no fake StockItemId. Provision after Offer created, then set physical by location. Respect owner/active/edition, reserve floor, unit policy; reserved and available read-only; no total-stock mutation or implicit warehouse creation.

#### Scenario: R10-03-P — positive
- **GIVEN** two eligible locations
- **WHEN** enter both stock counts before create and save
- **THEN** two real positions, independent physical/reserved/available reads
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-03-N — negative
- **GIVEN** foreign/inactive location or below reserved
- **WHEN** direct API attempt
- **THEN** denial and unchanged positions
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-03-B — boundary
- **GIVEN** second write fails
- **WHEN** retry known failed remainder
- **THEN** first success preserved, no duplicate position, reason retained
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

### Requirement: R10-04 — Shared persistence
The R10 workspace and its applicable owning contracts SHALL satisfy the following: One shared admin/seller create/apply and edit use case with typed per-domain outcomes. Full reread matches exact seller, offer ID, variant, channel, status, SKU, amount/currency, return, min/max, fulfillment and positions. Partial create must recover persisted ID and retain unapplied edits; no blind duplicate. Unknown response requires reconciliation before retry. Actor/tenant/owner scoped draft storage; clear committed/discarded draft.

#### Scenario: R10-04-P — positive
- **GIVEN** valid draft
- **WHEN** create/edit then reread
- **THEN** all requested fields match and committed draft clears
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-04-N — negative
- **GIVEN** wrong seller/value in response or lost response
- **WHEN** controlled negative injection
- **THEN** no false success; recover unapplied changes
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-04-B — boundary
- **GIVEN** same key same payload, changed payload, different key conflict
- **WHEN** replay all cases
- **THEN** one durable object, correct typed replay/conflict
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

### Requirement: R10-05 — Atomic concurrency
The R10 workspace and its applicable owning contracts SHALL satisfy the following: Real owning-module compare-and-swap or atomic transaction protection for Offer/Pricing/Inventory and affected Fulfillment. Pre-read is not atomic. Independent domain transactions and honest partial outcomes; no foreign DbContext or global transaction. Investigate existing persistence primitives; if schema change required prepare module-owned migration plan and obtain required authorization before application; disposable DB only if authorized. Never run live migration.

#### Scenario: R10-05-P — positive
- **GIVEN** two sessions with same version
- **WHEN** first saves then second attempts stale write
- **THEN** typed conflict, first value retained
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-05-N — negative
- **GIVEN** stale API request
- **WHEN** attempt commercial/price/stock write
- **THEN** zero overwrite
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-05-B — boundary
- **GIVEN** concurrent reservation and stock adjustment
- **WHEN** execute real concurrency test
- **THEN** reserve-floor invariant maintained
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

### Requirement: R10-06 — History
The R10 workspace and its applicable owning contracts SHALL satisfy the following: Inspect actual Offer events/audit/outbox and Pricing/Inventory/Fulfillment contracts; authorized read projection of recorded changes, timestamps, actor where recorded and old/new when reliable. No fabricated historical records. Record new changes in owning module with reliable transaction/event pattern if needed. Honest empty/unknown actor, pagination; no generic Host audit authority.

#### Scenario: R10-06-P — positive
- **GIVEN** recorded mutation
- **WHEN** view actual history
- **THEN** correct event/time/scope
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-06-N — negative
- **GIVEN** foreign seller
- **WHEN** read history via API
- **THEN** denial, no disclosure
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-06-B — boundary
- **GIVEN** empty history or unrecorded actor
- **WHEN** load page
- **THEN** honest empty/unknown with stable paging
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

### Requirement: R10-07 — Unit and conditions
The R10 workspace and its applicable owning contracts SHALL satisfy the following: Catalog authoritative unit, precision, step and rounding; create/edit and inventory client/server validation; integer/fractional quantities, min/max, reserved floor. Existing return policy and days, clear limits, lifecycle. No placeholder or raw technical label.

#### Scenario: R10-07-P — positive
- **GIVEN** integer and fractional fixtures
- **WHEN** save valid limits and stock
- **THEN** exact values and policy reread
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-07-N — negative
- **GIVEN** off-step, wrong precision, min>max or forbidden return
- **WHEN** UI/API submit
- **THEN** localized validation, unchanged state
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-07-B — boundary
- **GIVEN** clear limits and boundary days
- **WHEN** save and reread
- **THEN** null limits and valid policy precisely retained
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

### Requirement: R10-08 — Price and shipping
The R10 workspace and its applicable owning contracts SHALL satisfy the following: Actual base price/currency and permitted fulfillment responsibility/type; no blank-to-zero; IRR precision; locale independent of currency; respect existing market/channel. Seller cannot mutate admin-only settings. Unsupported Stage3 service, funding discounts and multi-currency remain gated; no fake control. Preserve other shipping flows.

#### Scenario: R10-08-P — positive
- **GIVEN** valid base and authorized shipping
- **WHEN** save both
- **THEN** exact independent rereads
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-08-N — negative
- **GIVEN** blank price or unauthorized seller fulfillment
- **WHEN** submit
- **THEN** typed error, no unintended write
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-08-B — boundary
- **GIVEN** zero price or clear shipping
- **WHEN** exercise only policy-approved cases
- **THEN** actual owner policy enforced and exact result
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

### Requirement: R10-09 — Central locale
The R10 workspace and its applicable owning contracts SHALL satisfy the following: Remove hardcoded ru prefix and unsafe cast; use actual registry UrlPrefix/code/culture/direction/calendar/active/default through canonical shared provider/resolver and public Localization CQRS. Minimal shared integration for offer routes and controls; no feature-local locale authority, query hack or whole-app translation rewrite. Explicit resource fallback. No live registry mutation.

#### Scenario: R10-09-P — positive
- **GIVEN** FA/EN and configured third locale; distinct prefix fixture if needed
- **WHEN** navigate actual create/edit/list and switch
- **THEN** registry metadata and directions respected, draft preserved
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-09-N — negative
- **GIVEN** disabled/invalid prefix
- **WHEN** request route
- **THEN** canonical fallback or 404 per policy
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-09-B — boundary
- **GIVEN** registry default changes or code differs from prefix
- **WHEN** resolve isolated registry fixture through actual shared path
- **THEN** metadata-driven routing, no hardcoded language
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

### Requirement: R10-10 — Frontend architecture
The R10 workspace and its applicable owning contracts SHALL satisfy the following: Fix Offer-owned flat admin API/intent and seller transport boundaries in canonical feature structure; avoid cycles/deep imports and dumping-ground exports. Routes compose, typed adapters transport, use cases orchestrate, hooks own state, components render. Reuse AppDataGrid, Toast, MessageBox. Existing <=800 new file and shrink-only legacy guard, cohesion not cosmetic split. Preserve R8 visual design. No guard/baseline changes.

#### Scenario: R10-10-P — positive
- **GIVEN** R9-R1 baseline
- **WHEN** inspect changed source and run guards
- **THEN** zero in-scope violations, no new debt
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-10-N — negative
- **GIVEN** disposable forbidden import or oversized file
- **WHEN** run applicable guard
- **THEN** guard rejects and fixture restored
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-10-B — boundary
- **GIVEN** unrelated historic debt
- **WHEN** compare baseline/candidate violation sets
- **THEN** exact external debt reported; no waiver for offer-owned files
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

### Requirement: R10-11 — Backend and Host
The R10 workspace and its applicable owning contracts SHALL satisfy the following: Apply all eight AQ gates: owning Contracts, CQRS MediatR, Result, validators, resources, namespaces, folder and DI, permissions and transactions. Thin endpoints, no foreign DbContext or Host business logic. Preserve public language minimal DTO. No unrelated AMSC.

#### Scenario: R10-11-P — positive
- **GIVEN** changed owning modules
- **WHEN** inspect references and run relevant architecture/endpoint/validation tests
- **THEN** no new violations in touched paths
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-11-N — negative
- **GIVEN** unauthorized command/query
- **WHEN** direct API attempt
- **THEN** fail closed
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-11-B — boundary
- **GIVEN** shared contract change
- **WHEN** compile and test adjacent consumers
- **THEN** compatibility retained
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

### Requirement: R10-12 — UX and feedback
The R10 workspace and its applicable owning contracts SHALL satisfy the following: Shared Toast 3000ms success, shared MessageBox operational failure/duplicate/unknown/dirty exit, inline field errors; no duplicated banners or raw IDs/errors. Preserve R8 layout, both grids, correct RTL/LTR and accessibility; clear obsolete errors upon correction.

#### Scenario: R10-12-P — positive
- **GIVEN** successful admin/seller save
- **WHEN** observe feedback
- **THEN** single shared toast closes; no permanent banner
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-12-N — negative
- **GIVEN** blank inventory reason then correct; controlled operation failure
- **WHEN** save then retry
- **THEN** inline correction, real persisted success; operation error modal
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-12-B — boundary
- **GIVEN** dirty exit cancel/confirm and duplicate link
- **WHEN** exercise both actions
- **THEN** safe navigation, no data loss or duplicate
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

### Requirement: R10-13 — Performance and regression
The R10 workspace and its applicable owning contracts SHALL satisfy the following: Comparable actual served-build form and picker measurements versus R8 reported 1598/103 ms, define warm/cold, multiple samples median/p95 and evidence generator. Diagnose unexplained regressions. Preserve orders/products/campaigns, Stage2 cart, grid sort/filter/paging/export/saved views; real assertions, not count-only. Responsive 360/768/1440; no screenshot ceremony.

#### Scenario: R10-13-P — positive
- **GIVEN** comparable fixture and browser
- **WHEN** measure and run regression flows
- **THEN** no unexplained material regression; actual expected results
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-13-N — negative
- **GIVEN** network failure
- **WHEN** controlled retry path
- **THEN** recoverable, no blind writes
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-13-B — boundary
- **GIVEN** 360/768/1440 with long labels
- **WHEN** exercise actual form and grids
- **THEN** usable controls and no unintended overflow
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

### Requirement: R10-14 — Evidence and closure
The R10 workspace and its applicable owning contracts SHALL satisfy the following: Read source, OpenSpec and AQ; architecture plan and delivery manifest before code. Full frontend architecture, build/typecheck/lint, focused owner backend and mounted tests, real Chromium→Host→isolated PostgreSQL→GET/SQL for persistence. Record identity, actors, request counts and before/after; separate R10 evidence, no overwrite R9/R9-R1. Finally cleanup and verify restore. Checker rejects missing test/wrong fact/hash tamper. STOP_FOR_ARCHITECT; no TB-297.

#### Scenario: R10-14-P — positive
- **GIVEN** isolated fixtures and issued matrix
- **WHEN** execute required tests
- **THEN** observed facts, hashes and provider identities
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-14-N — negative
- **GIVEN** missing test, changed fact, tampered evidence
- **WHEN** run checker negative controls
- **THEN** all rejected
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

#### Scenario: R10-14-B — boundary
- **GIVEN** mid-test failure
- **WHEN** finally cleanup and reread
- **THEN** baseline restored, no unrelated writes
- **AND** No unauthorized writes, fabricated results, weakened guards or violation of the named requirement; negative cases must prove unchanged relevant state.

