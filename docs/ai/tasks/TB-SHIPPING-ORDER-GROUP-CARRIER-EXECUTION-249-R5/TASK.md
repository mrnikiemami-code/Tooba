PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R5
Parent-Task: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R4
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — durable dispatch completion and product physical specifications
Mode: CLOSE_REPLAY_AND_PACKAGE_VALUE_DEFECTS_THEN_ADD_PRODUCT_MEASUREMENTS
Expected-Base-SHA: a9af3f400e968113c5f09a7edee1c70f2a51f1a6
Status: ISSUED
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1. Continue same delivery with explicit added user requirement

User authorizes sequential correction/review until usable handoff, one active task. Preserve existing UI quality. No Tipax account, real provider HTTP, screenshots, real order fulfillment/payment or changing user's cart.

R4's PostgreSQL 17/17 is useful executed evidence, but independent review finds unclosed defects. Do not label overall software accepted yet. Read all local/module instructions and R4 evidence.

NEW binding requirement: every product must support weight in GRAMS and length/width/height in CENTIMETRES through real create/update commands, queries/read DTOs, persistence and existing product editor. Dimensions of different products MUST NOT be summed. Physical parcel dimensions remain separate actual package facts. Implement in Phase B after Phase A without abandoning current shipping delivery.

Canonical: SarvNewVer FE3000→Host5088 SingleStore/store-alpha/tooba_alpha; Marketplace ownership retained. No alternative final runtime.

2. Phase A — precise defects found in R4 source

ShippingCarrierShipmentDirectory:

packageDispatchId=UuidV7.New() per Create; operation_key includes that NEW ID. Two requests passing initial openCreate read can insert different operations. Unique operation_key does not identify the same intent.
open Pending replay resumes allocation/provider without durable exclusive execution claim. Concurrent resumptions can both call external create.
allocation succeeds before FulfillmentShipmentId is saved in Shipping. A crash between those points can allocate again on resume. Exact CreatedShipmentId helps identity but does not close idempotent allocation.
provider call has no persisted "external may have started" claim distinct from safe pre-dispatch pending. Crash/cancellation/exception after provider accepts but before Succeed can cause ResumePendingCreate to SEND AGAIN.
Resume uses current account context without verifying snapshot configuration against execution account/config; accepted execution semantics can drift.
ScalePackageForSelection divides value/collect by selectedQty/GroupPackedQuantityTotal. Mixed prices, partially packed orders and currency rounding make that invalid. Example one item worth100 and one worth900 cannot each get500 merely because quantities match.
weight/dims remain one group completion reused for different dispatches; partial package must have its OWN physical facts.
Inspect current code before changing; do not merely add comments/tests around these behaviors.

Required repair:
A. Explicit stable dispatch intent keyed by exact owner/group/physical-package identity. Server-persisted draft/dispatch ID may be an untrusted client locator; same intent + same payload replays, different payload typed conflict.
B. Fulfillment allocation idempotent on same durable intent; reuse exact recorded allocation after link-write failure. Same owner module enforces quantity concurrency for manual/carrier actions.
C. Durable exclusive dispatch claim BEFORE provider HTTP, outside long DB transaction. At most one caller invokes provider for the claimed intent. Crash after possible send => UNKNOWN/reconciliation, never blind resend. A lease expiring does not prove carrier did not create a shipment.
D. Snapshot exact account/config/service/routing/package/currency/sender/recipient/lines before external call; rotation/disable policy must not silently substitute fresh configuration.
E. Package declared value from its exact selected Order line values/quantities and explicit provider semantics; collect allocation from actual accepted collectible obligations, with currency precision and deterministic remainder accounting. Do not allocate by raw item count. Total across packages cannot exceed accepted totals.
F. Store actual per-dispatch weight/dimensions; no repeating full-group physical facts on partial dispatches or inferred packing algorithm.

3. Phase B — product weight/dimensions (user requested, no dimension aggregation)

Source baseline:
CatalogShippingWeightGrams already derives canonical grams from active Number weight/*_weight attributes (g/kg); ICatalogCheckoutLookup.GetShippingWeightGramsByVariantIdsAsync feeds Order.
Product length/width/height have no explicit shipping read/command contract. ShippingCapturedRevisionQuoteLookup currently uses method binding DefaultLength/Width/HeightCentimeters.

Implement one Catalog-owned physical specification authority, not a second conflicting weight:

Explicit stable semantic fields WeightGrams, LengthCentimeters, WidthCentimeters, HeightCentimeters in write/read contracts.
Inspect existing Catalog attribute storage and variant override semantics; use existing owned mechanism or a cohesive typed physical-spec representation, with an explicit compatibility policy for existing weight attributes. No competing authorities or silent preference guessing; conflicting legacy data must be resolved explicitly/fail closed.
Product-level values plus lawful variant override/inheritance when variants differ. Invalid present override must not silently inherit.
Wire ACTUAL product create/update command paths, endpoint body/validator/handler, directory persistence and product/editor queries. No unused contract-only overload.
Existing product editor gets one compact "مشخصات فیزیکی" block with واضح units: وزن (گرم)، طول/عرض/ارتفاع (سانتی‌متر). Reuse design components/skill; preserve all existing tabs and layout.
Null means unspecified; never0/default fabricated. Validate positive provided values, bounded precision/overflow, invalid types and partial-state semantics. Existing products remain editable; missing shipping measurements only block flows that actually require them.
Additive migration if required, no mass guessed backfill/seed; legacy values preserved with provenance.
Expose neutral physical facts through Catalog Contracts to Order/Shipping, no foreign persistence.
Existing method/default package dimensions must not masquerade as dimensions of each product. Actual package L/W/H are explicit packing facts; no sum(length), max-only pretend packing, silent multiplication or invented box layout.
Unit/item weights may contribute to physical package contents under explicit packaging policy; final packed weight may include packaging. Do not conflate product measurements with provider-declared parcel facts.
Existing accepted Order/shipment history remains immutable after Catalog measurements change.
4. Targeted proof that catches the real defects

Reuse R4 disposable PostgreSQL fixture. Provision identified task-only DB, normal migrations, private secret, no canonical tests/reset. External provider controlled only. Strengthen assertions, not just counts.

Barrier-controlled concurrent same dispatch: both callers pass initial lookup; one allocation and ONE provider invocation. Test allocation/link/pre-send recovery in separate contexts/process-like reentry.
Provider accepts then local cancellation/crash before result save: later replay does NOT send again; UNKNOWN and lawful reconciliation.
Failure AFTER allocation BEFORE Shipping link: resume same allocation, no stranded/double quantities.
Different physical packages same group have own line qty and own weight/dims; mixed unit values100/900; declared values correct; collect/rounding residual totals exact.
Snapshot account rotation/disable/config changes refused/reconciled without silent substitution.
Real Catalog create/update→PostgreSQL→query returns exact g/cm; actual endpoint validator exercised, not DTO construction only.
Legacy g/kg compatibility, variant overrides, conflicting/negative/overflow/null/partial cases. Catalog change does not rewrite accepted Order.
Shared UI interaction (actual rendering/events, not regex/source-string "interaction"): physical-spec edit/save/reload, dispatch completion/actions/error/retry, locks and stale response. No layout regression.
PendingPayment Manual original order remains correct/no raw carrier form; foreign owner replay denied.
Run focused tests + affected builds/typecheck once after fixes. Keep controlled vs live disclosure.
Do not count unit methods merely named Concurrent as deterministic concurrency proof. Report exact barriers, failure boundaries and persisted row/request counters.
5. Canonical deliverable and recovery

Use existing authorized normal BFF test session for READ-ONLY order/product form checks, independent of user tabs. Host Bearer proof is useful but not browser-component acceptance. Do not grant roles or fake credentials/session. No actual external/provider writes.
Deploy final corrected source on3000/5088; private backup before official additive migrations, exact history readback. Preserve DataProtection keys, actual accounts/cart/rates/orders, unsaved edits and local working gates.
No second runtime or alternate DB handoff, screenshots or global engine restarts.

First10 focused minutes: exact PhaseA/PhaseB file plan and realistic estimate, do not pretend whole scope is15 minutes. Checkpoint25 and every30 minutes: completed working paths + remaining scope, one cumulative ledger; do not publish partial as final.
If genuinely blocked report exact technical error and concrete measured boundary, not missing env or account. No weakening tests for green.

Preserve task verbatim:
docs/ai/tasks/TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R5/TASK.md
Evidence:
docs/architecture/evidence/TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R5/{TASK.md,RESULT.md,RECEIPTS.md}
Update AGENTS.md, both recovery TOPs and carrier plan: R4 source review findings, product-measurement requirement and actual completion state. Historical PASS remains worker-only, not Architect acceptance.
Normal push no force/reset/amend/discard. Full BRIDGE-WAKE-V1 result with actual Base/Code/Evidence SHA, per-phase delivered status, exact canonical stage, migration ledger, deterministic proof, elapsed and remaining gaps.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
END_TOOBA_TASK
