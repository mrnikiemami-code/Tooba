# Tooba — Returns / RMA Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock; not a Gate
```

Task:

```text
TB-P00-T026
```

Documentation only. No RMA APIs, schemas, refund code, reverse-shipping providers, warehouse inspection, UI, Tax answers, T027, or P00 Gate.

```text
Return != Cancellation
Return != Refund
Return != Fulfillment
Return != Inventory
Return != Order
Backend/module boundary != UI boundary
Modular monolith; no cross-module DB joins
```

## A. Core Separation

**Order** owns commercial order truth (snapshots). Returns consume Order evidence via contract; they do not rewrite Order history as a return flag.

**Fulfillment** owns outbound delivery/shipment execution. Returns may request reverse logistics; they do not mutate Fulfillment tables.

**Payment** owns refund/payment execution. Return approved ≠ refund completed.

**Inventory** owns stock/restock authority. Return received ≠ automatically available stock.

**Returns/RMA** owns return request, eligibility, case lifecycle, inspection/disposition **coordination**, and refund/restock **instructions**.

**Pricing/Promotion** are not re-run as current rules on return; historical Order allocations are consumed.

**Tax** is a policy hook. This task does not invent tax law.

## B. Scope

Conceptual (not a schema): Return Request; Return Case / RMA; Return Item / Quantity; Return Reason; Eligibility; Approval; Reverse Logistics; Received/Inspected; Disposition; Refund Request/Instruction; Restock/Disposition Instruction; Resolution.

## C. Cancellation vs Return

```text
Cancellation = before/without completed delivery lifecycle (Order-owned)
Return = after fulfillment/delivery or after dispatch according to policy
```

Do not represent both with one boolean/status. Exact cutover by product policy may vary; concepts remain distinct.

## D. Return Eligibility

May depend on: order item; delivery date; product/category policy; seller policy; market/legal policy; return window; quantity; condition; reason; final-sale/non-returnable; digital/service later; B2B contract.

Do not hardcode legal windows.

Exact commercial/legal eligibility:

```text
NEEDS_USER_PRODUCT_DECISION / NEEDS_LATER_P00_DETAIL
```

## E. Order Evidence

Returns consume immutable Order snapshots/references: item, quantity, price paid, promotion allocation, seller, buyer, market, currency, delivery address/context.

No direct Order table joins. Do not recalculate historical line price from current Pricing.

## F. Fulfillment Evidence

Via Fulfillment contract/projection: ShipmentId, DeliveredAt, delivered quantity, seller fulfillment responsibility, carrier/tracking, proof of delivery.

Returns does not mutate Fulfillment tables.

## G. Partial Return

Must support: some items; partial quantity of one line; multiple RMAs over time against remaining returnable quantity.

Do not assume 1 Order = 1 Return or 1 OrderLine = all-or-nothing.

## H. Returnable Quantity

Authoritative projection: ordered; cancelled; already returned; currently in active RMA; remaining eligible.

Do not derive from UI. Later implementation must accept concurrency-safely.

## I. Marketplace Seller Responsibility

Preserve: SellerId; seller fulfillment responsibility; platform policy; seller approval where allowed; platform override/escalation; funding/refund responsibility.

No cross-seller case leakage.

## J. Single-Store

Same Returns architecture. Seller may be implicit. No second return engine.

## K. Return Reason

Typed reasons (examples, not locked enum): damaged; wrong item; not as described; changed mind; defective; late delivery; other.

Reason may affect shipping refund, restocking fee, seller vs customer fault, inspection depth. Reason is not Order status.

## L. Customer Evidence

Future: photos/media refs, comments, serial/unboxing. Media via Media pipeline refs, not blobs in RMA tables. Evidence is not Catalog/Content.

## M. RMA Lifecycle

Concepts (enum not locked): Requested; Pending Approval; Approved; Rejected; Awaiting Shipment; In Transit; Received; Inspecting; Disposition Decided; Refund Instructed; Restock Instructed; Resolved; Cancelled (request withdrawn, not Order cancellation).

Transitions are case-owned. Payment/Inventory/Fulfillment states remain their own.

## N. Automatic vs Manual Approval

Architecture allows auto-approve by policy (window, reason, value, seller) and manual queue. First-release mix: `NEEDS_LATER_P00_DETAIL`. Auto-approve still emits audit and may still require inspection before restock.

## O. Reverse Logistics

Outbound Fulfillment ≠ reverse logistics. Returns coordinates reverse movement via Fulfillment **return-shipment commands/contracts** or a dedicated reverse adapter consumed by Fulfillment.

Provider behind adapter. No carrier SDK in Returns.

## P. Return Shipment

Label, tracking, carrier, customer drop-off vs pickup are operational details behind adapter. Returns stores shipment **refs**, not carrier payloads as domain truth.

If `return_not_required`, no shipment is invented.

## Q. Inspection

Received ≠ inspected. Inspection records condition vs claimed reason. Inspectors are SpiceDB-authorized. Inspection does not itself refund or restock.

## R. Disposition

Typed outcomes: restock sellable; restock as damaged; scrap; return to seller; hold; other.

Disposition **instructs** Inventory (and possibly Seller). Returns does not increment stock.

## S. Inventory Boundary

```text
Return received != automatically available stock
```

Restock requires inspection/disposition **and** Inventory contract. Returns must not write inventory tables.

## T. Payment / Refund Boundary

```text
Return approved != Refund completed
```

Returns may request/authorize a refund. Payment owns refund transaction lifecycle.

Correlation: RmaId, OrderId, Payment/RefundId, Amount, Currency.

## U. Refund Amount

May depend on: item snapshot; promotion allocation; shipping; tax; restocking fee if allowed; seller/platform funding; partial quantity; previous refunds.

Returns must not reinvent Pricing. Dedicated commercial refund calculation/policy contract coordinated with Order/Pricing/Promotion/Tax.

Exact algorithm:

```text
NEEDS_LATER_P00_DETAIL
```

## V. Promotion Interaction

Partial returns complicate discounts. Use historical applied allocation from Order.

Do not blindly re-run current Promotion rules (threshold, buy-X-get-Y, free shipping, seller-funded).

## W. Shipping Refund

Do not assume full original shipping refund. Policy inputs: reason, seller fault, customer preference, partial/full, market/legal.

## X. Tax Boundary

Refund tax treatment depends on Tax policy. Do not invent.

Preserve: tax snapshot/reference; refundable tax amount; jurisdiction policy hook.

Tax decision remains pending (see P00 Tax Decision Inputs).

## Y. Exchange

Related, not identical. Future: return + replacement order/fulfillment. Do not model as a return status.

Full exchange orchestration:

```text
DEFERRED
```

## Z. Replacement

Preserve resolution types: Refund; Replacement; Store Credit **only if USER confirms later**; Repair later.

Do not promote Store Credit because a template has Wallet.

## AA. No-Return Refund

Allow policy outcome `return_not_required` without pretending a shipment occurred. Exact policy later.

## AB. Seller Dispute / Escalation

Marketplace: seller may dispute customer claim/disposition/funding. Platform escalation is a case overlay, not Order status. Isolation and audit required.

## AC. B2B Returns

Same module; contract/PO/approval windows may differ. Do not fork `B2BReturn` schema. Exact B2B policy: `NEEDS_LATER_P00_DETAIL`.

## AD. Authorization

SpiceDB: customer request/view own; seller view/approve own partition; staff moderate/override; warehouse inspect/disposition; finance instruct refund.

UI visibility is not the security boundary.

## AE. Customer Eligibility Security

Do not trust client-supplied OrderId. Own-order via Identity/Party + SpiceDB. Guest return tokens later fail closed.

## AF. Idempotency

Request create, approval, refund instruction, restock instruction keyed by command/RMA ids. Duplicate submit ≠ duplicate refund/restock.

## AG. Concurrency

Returnable quantity races: remaining eligible must be reserved/accepted atomically later. Fail closed on over-return.

## AH. Audit

Durable audit: request, approve/reject, shipment, receive, inspect, disposition, refund instruct, restock instruct, escalation.

Actor, reason, target, amounts, correlation. Technical logs are not sufficient.

## AI. Notifications

Returns emits intents: requested, approved, rejected, label ready, received, refund issued (from Payment fact), resolved.

Notification capability owns channel/provider. No SMS SDK in Returns.

## AJ. Analytics

May observe: request started, approved, completed, reason distribution, cycle time. Analytics does not own RMA state.

## AK. Reviews Boundary

Returns is not Reviews. Defective-item returns may later inform eligibility; they do not write ratings. See `docs/architecture/24-reviews-ratings.md`.

## AL. Fraud / Risk

May signal serial return abuse, brigading, fake damage claims. Returns still owns case state. Fraud ≠ Authorization ≠ moderation.

## AM. Search / SEO

Private RMA flows are **not** public Search/SEO. Do not index return cases. No structured data for customer RMAs.

## AN. Customer UX

Return Case workflow: eligible items, quantities, reason, evidence, status timeline, label/instructions, refund progress in human language.

Not a grid of RMA rows. Not Order-status collapse.

## AO. Admin UX

Returns Workspace: queue, filters, seller/order context, evidence, inspection, disposition, refund/restock instruction status, bulk where safe, audit.

Not raw RMA CRUD.

## AP. Seller UX

Marketplace seller: own cases only, approval where allowed, dispute, return-to-seller disposition visibility. Cannot see other sellers.

## AQ. Customer Timeline

Compose Order + Fulfillment + Payment + Return projections. Each module remains owner. Timeline is UX composition.

## AR. Mobile UX

Request and status must be usable on mobile, RTL/LTR, accessible. Label/QR later is operational, not a desktop-only afterthought.

## AS. Loading / Empty / Error

Professional empty (nothing eligible), loading, error, partial (refund pending, inspection pending). Do not look broken when Payment lags.

## AT. Read Models

Customer/seller/admin read models from Returns + contracts. No mega-joins across Order/Payment/Inventory tables.

## AU. Operations Search

Staff search by RMA id, Order id, customer (permissioned), tracking, seller. Not public product Search.

## AV. Reconciliation

First-class: RMA instructed refund vs Payment refund; restock instruction vs Inventory; return shipment vs Fulfillment receive; returnable quantity vs completed RMAs.

Corrections via module commands, not spreadsheet edits.

## AW. Observability

Correlate RmaId, OrderId, Payment/RefundId, ShipmentId. No PAN/PII dumps. Business audit ≠ traces.

## AX. Cache

Private. Invalidate on lifecycle/instruction changes. No public shared cache of RMAs. State-changing actions read authority.

## AY. Data Ownership Matrix

| Fact | Returns | Order | Payment | Inventory | Fulfillment | Pricing | Promotion | Tax | Seller | Party | Media | Notifications | Analytics | Authorization | Audit |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| return request | OWNER | REFERENCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | REFERENCE | REFERENCE | REFERENCE | CONSUMER | CONSUMER | CONSUMER | CONSUMER |
| returnable quantity | OWNER (projection) | SOURCE | NOT_OWNER | NOT_OWNER | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | REFERENCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER |
| order price snapshot | CONSUMER | OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | SOURCE (historical) | SOURCE (allocation) | SOURCE (snapshot) | REFERENCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER |
| refund transaction | INSTRUCTION | REFERENCE | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | REFERENCE | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | CONSUMER |
| restock | INSTRUCTION | NOT_OWNER | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | REFERENCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER |
| return shipment | INSTRUCTION / REFERENCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | REFERENCE | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | CONSUMER |
| promotion allocation | CONSUMER | OWNER (snapshot) | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | SOURCE | NOT_OWNER | REFERENCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER |
| tax refund | INSTRUCTION / HOOK | SOURCE (snapshot) | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER (policy later) | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER |
| seller responsibility | REFERENCE | SOURCE | CONSUMER | CONSUMER | SOURCE | NOT_OWNER | CONSUMER | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | CONSUMER |
| customer evidence | OWNER (refs) | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | REFERENCE | OWNER (assets) | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER |
| notification delivery | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | REFERENCE | NOT_OWNER | OWNER | NOT_OWNER | CONSUMER | CONSUMER |
| return analytics | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | CONSUMER | NOT_OWNER |
| permission | CONSUMER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | OWNER | CONSUMER |
| audit | SOURCE | SOURCE | SOURCE | SOURCE | SOURCE | NOT_OWNER | NOT_OWNER | SOURCE | SOURCE | REFERENCE | NOT_OWNER | SOURCE | NOT_OWNER | CONSUMER | OWNER |

## AZ. Failure Matrix

| Failure | fail closed? | retry? | manual? | degrade? | compensate? | alert? | customer-visible |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Order evidence unavailable | Yes for new request | Yes | If prolonged | No fake eligibility | N/A | Yes | Cannot start verified return |
| Fulfillment evidence unavailable | Yes if delivery-gated | Yes | Yes | Delay eligibility | N/A | Optional | Eligible-later |
| Return quantity race | Yes | No | No | Idempotent winner | N/A | No | Over-return denied |
| Return shipment provider unavailable | For ship-required | Yes | Label later | Queue | N/A | Yes | Approved, label pending |
| Received but inspection pending | No | N/A | Inspect queue | Hold restock/refund-if-policy | N/A | SLA | Received / inspecting |
| Refund service unavailable | Do not pretend paid | Yes | Finance queue | Case stays refund-instructed | N/A | Yes | Refund pending |
| Refund duplicate | Yes | No | Investigate | N/A | Reverse extra via Payment | Yes | First refund only |
| Inventory restock failure | Keep instruction | Yes | Warehouse | Stock not sellable | Retry command | Yes | Case may still resolve commercially |
| Tax policy unavailable | For tax-dependent refund | No invent | Hold | Refund net-of-tax only if policy | N/A | Yes | Pending tax |
| Promotion allocation missing | For allocation-sensitive | Rebuild from Order snapshot | Yes | Conservative refund | N/A | Yes | Pending review |
| Cross-seller data leak | Yes deny | No | Security | Empty | N/A | Yes | Deny |
| Unauthorized return request | Yes deny | No | N/A | N/A | N/A | Optional | Denied |
| Media evidence unavailable | Optional by policy | Retry upload | Continue without | Degraded inspect | N/A | No | Upload failed |

## BA. Testing Strategy — Architecture Level

Future tests: full/partial/multiple RMAs; Marketplace seller scope; Single-Store; quantity concurrency; delivered eligibility; non-eligible; reverse shipment; inspect accept/reject; refund/restock correlation; promotion allocation; tax hook; customer/seller authz; reconciliation; mobile/RTL.

No tests in this task.

## BB. Decision Summary

| # | Decision | Classification |
| --- | --- | --- |
| 1 | Returns/RMA separate from Order, Fulfillment, Payment, Inventory | RECOMMENDED_FOR_ADR |
| 2 | Cancellation ≠ Return | RECOMMENDED_FOR_ADR |
| 3 | Multiple partial RMAs per Order | RECOMMENDED_FOR_ADR |
| 4 | Returnable quantity concurrency-safe and quantity-aware | RECOMMENDED_FOR_ADR |
| 5 | Marketplace seller responsibility isolated | RECOMMENDED_FOR_ADR |
| 6 | Order/Fulfillment evidence via contracts, never joins | RECOMMENDED_FOR_ADR |
| 7 | Received ≠ restock | RECOMMENDED_FOR_ADR |
| 8 | Refund execution belongs to Payment | RECOMMENDED_FOR_ADR |
| 9 | Approval ≠ refund completed | RECOMMENDED_FOR_ADR |
| 10 | Historical promotion/price allocations; do not re-run current rules blindly | RECOMMENDED_FOR_ADR |
| 11 | Tax refund is an explicit Tax policy hook | RECOMMENDED_FOR_ADR |
| 12 | Reverse logistics provider behind adapter | RECOMMENDED_FOR_ADR |
| 13 | Inspection/disposition separate from refund state | RECOMMENDED_FOR_ADR |
| 14 | Exchange not collapsed into Return | RECOMMENDED_FOR_ADR |
| 15 | SpiceDB for customer/seller/admin/warehouse/finance | RECOMMENDED_FOR_ADR |
| 16 | Critical actions idempotent and audited | RECOMMENDED_FOR_ADR |
| 17 | Reconciliation first-class | RECOMMENDED_FOR_ADR |
| 18 | UX is case/workflow/timeline, not CRUD | RECOMMENDED_FOR_ADR |
| 19 | Private RMA excluded from public SEO/Search | RECOMMENDED_FOR_ADR |
| 20 | Analytics/Fraud/Notifications consume facts, do not own RMA | RECOMMENDED_FOR_ADR |
| 21 | Backend/module ≠ UI | RECOMMENDED_FOR_ADR |
| 22 | UI requires visual evidence and Architect visual ACCEPT | RECOMMENDED_FOR_ADR |
| — | Legal/commercial eligibility windows | NEEDS_USER_PRODUCT_DECISION / NEEDS_LATER_P00_DETAIL |
| — | Refund amount algorithm | NEEDS_LATER_P00_DETAIL |
| — | Auto vs manual approval mix | NEEDS_LATER_P00_DETAIL |
| — | B2B return windows | NEEDS_LATER_P00_DETAIL |
| — | Exchange orchestration | DEFERRED |
| — | Store credit / wallet as resolution | DEFERRED |
| — | Repair | DEFERRED |

Do not create a final ADR in this task.

## P00 Tax Decision Inputs

Minimum USER/Architect decisions **before** Tax architecture can close. Cursor does **not** answer these. Cursor does **not** issue a Tax task.

```text
1. First commercial launch market / jurisdiction?
2. Are displayed/catalog prices tax-inclusive, tax-exclusive, or market-configurable?
3. Is tax calculation required in Tooba first release, or will first-release prices be treated as final tax-inclusive commercial amounts supplied by merchant/admin?
4. Is B2B tax invoice / VAT-number handling required in first sale or later?
```

## P00 Gap Status After Returns

```text
Reviews / Ratings = COMPLETE (TB-P00-T025 Architect-accepted)
Returns / RMA = current task pending Architect acceptance
Tax = USER decision required before Gate
Notifications = boundary sufficient for P00
Fraud / Risk = boundary sufficient for P00
Support = deferred post-P00 unless USER changes scope
```

Cursor does not issue T027, Tax, or P00-GATE.
