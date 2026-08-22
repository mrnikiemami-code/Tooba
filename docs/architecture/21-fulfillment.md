# Tooba — Fulfillment Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T022
```

Documentation only. No carrier APIs, labels, warehouse code, tracking jobs, Returns, UI, schemas, or provider integrations.

```text
Order != Fulfillment
Payment != Fulfillment
Inventory != Fulfillment
Shipment != Order
Carrier != Fulfillment Domain
```

```text
Locale != Market != Currency
Backend/module boundary != UI boundary
Modular monolith; no cross-module DB joins
```

## A. Core Separation

**Order** owns commercial commitment and order truth (lines, snapshots, buyer, commercial status).  
**Inventory** owns stock, availability, and reservation/allocation authority.  
**Payment** owns financial transaction truth (attempts, captures, COD settlement records).  
**Fulfillment** owns post-order physical/service delivery **execution** state.

A shipment is not an order. A carrier/3PL SDK is not the Fulfillment domain model.

```text
Order = what was sold
Inventory = what can be held/committed
Payment = what was paid / is owed
Fulfillment = how execution of delivery proceeds
Carrier = external logistics operator behind an adapter
```

## B. Fulfillment Scope

Fulfillment conceptually covers (not a schema):

```text
Fulfillment Request
Fulfillment Unit
Shipment
Delivery Method
Carrier/Provider
Tracking
Packing/Dispatch lifecycle
Delivery status
Pickup readiness
Delivery exception
```

Out of this module: catalog copy, live prices, PSP internals, SpiceDB graph, notification providers, analytics warehouses, address-book mutation.

## C. Fulfillment Unit

Stable grouping for execution. Candidate dimensions: Order, Seller, ship-from location, delivery method, destination, fulfillment provider, availability/timing, item constraints.

One Order may produce **multiple** fulfillment units. One unit may later produce **one or multiple** Shipments.

Do not assume:

```text
1 Order = 1 Shipment
```

## D. Marketplace Split

One customer Order may have multiple **seller-owned** fulfillment responsibilities:

```text
Order
├── Seller A Fulfillment
└── Seller B Fulfillment
```

Do not force all sellers into one logistics lifecycle. Order remains the customer-facing commercial aggregate/read experience. Seller money, stock, and dispatch stay separable.

## E. Single-Store

Same Fulfillment capability. Seller may be implicit. UX need not show seller. Do **not** fork a second fulfillment architecture.

Future multi-location warehouses remain possible on the same model (location ids via contract).

## F. Inventory Boundary

Inventory owns: stock, availability, reservation, allocation authority where defined.

Fulfillment may consume confirmed/reserved **allocation references**. Fulfillment must **not** directly mutate Inventory tables. Decrement/release happens through approved contracts/events after dispatch/cancel policy.

## G. Order Boundary

Order owns: ordered items, commercial snapshots, buyer, delivery-choice snapshot where commercially relevant, order status.

Fulfillment owns execution after fulfillment is authorized/created. Fulfillment events may influence Order’s customer-facing **derived** status via contract/event handling. Fulfillment does **not** rewrite Order tables.

## H. Payment Boundary

Fulfillment initiation depends on **payment policy**, not a single flag.

Examples: prepaid, COD, authorized-not-captured, B2B terms, bank transfer pending.

Do not hardcode:

```text
Paid == ready to ship
```

for every method. Policy/command orchestration decides eligibility.

## I. Fulfillment Eligibility

Explicit decision before creating/releasing work. Candidate inputs: Order state, Payment state/policy, Inventory reservation/allocation, future fraud/risk, seller acceptance, delivery method, address validity, future B2B approval.

Exact orchestration: `NEEDS_LATER_P00_DETAIL`.

## J. Shipping Method

Separate **Shipping/Delivery Method** (customer/commercial service level) from **Carrier** (operator).

Examples of methods: Standard Delivery, Express Delivery, Same Day, Store Pickup, Seller Delivery, future Digital/No Physical Fulfillment.

Carrier/provider is implementation. Do not expose carrier SDK concepts into Order.

## K. Shipping Quote Boundary

Checkout may need methods/price/ETA/restrictions **before** Order. Quote may depend on destination, seller/location, items, weight/dimensions, market, service level, cart/order value.

Fulfillment/shipping capability provides **service/rate quote inputs**. It does not own Order or Pricing write models. Shipping-charge composition sits with Pricing/Checkout (see L).

## L. Shipping Price Ownership

Direction:

```text
Fulfillment/Shipping capability provides service/rate quote inputs
Pricing/Checkout composes customer-facing charge
Order snapshots accepted shipping charge
```

Carrier raw rate ≠ customer price. Support: free shipping, subsidized shipping, seller-funded shipping, campaign shipping discount, B2B contract shipping.

Exact charge authority: `NEEDS_LATER_P00_DETAIL`.

## M. Delivery Promise

**Estimated delivery promise** ≠ **actual tracking/delivery state**.

Promise may use: inventory location, cutoff time, carrier SLA, seller handling time, destination, holiday calendar. Do not promise exact delivery from static copy alone. Preserve a future promise engine.

## N. Address Boundary

Party/Customer owns address-book **current** semantics. Order snapshots the delivery address used for the transaction. Fulfillment consumes that Order destination snapshot/reference.

Do not use the mutable current customer address as shipment truth after placement.

## O. Seller Fulfillment Model

Preserve a fulfillment-**responsibility** model. Candidate modes (not locked): Seller Fulfilled, future Platform Fulfilled, future 3PL Fulfilled, Hybrid.

## P. Fulfillment Location

Preserve future: Warehouse, Store, Seller Location, 3PL Location, Pickup Point.

Inventory and Fulfillment may reference location IDs through **contracts**. No cross-module joins.

## Q. Partial Shipment

Some lines/quantities ship now; remainder later. Quantity-level fulfillment tracking is required. A line is not all-or-nothing.

## R. Split Shipment

One fulfillment responsibility may split due to: multiple locations, availability, package constraints, carrier/service, seller operation.

Order UI still presents one coherent customer experience (see AR–AS).

## S. Shipment

Conceptual (no schema): ShipmentId, FulfillmentUnitId, carrier/service reference, tracking number/reference, items/quantities, DispatchedAt, DeliveredAt, Status, package references.

```text
Shipment != Order
```

## T. Shipment State

Do not lock the enum. Analyze: Pending, Preparing, Packed, ReadyForDispatch, Dispatched, InTransit, OutForDelivery, Delivered, DeliveryFailed, Cancelled, Exception.

Transitions are **guarded**. Provider status is **normalized** behind an adapter. No silent regression (see Y).

## U. Carrier / 3PL Adapter

External logistics behind internal interfaces. Illustrative names:

```text
IFulfillmentProvider
ICarrierGateway
ITrackingProvider
```

```text
Carrier != Fulfillment Domain
```

Provider SDK/data types must not leak into Order or Fulfillment **domain contracts**.

## V. Label / Booking Readiness

Future: shipment booking, label creation, pickup request, manifest, cancel shipment, tracking. Not implemented now.

Provider calls require **idempotency**. Duplicate retry must not duplicate bookings.

## W. Tracking

Sources: provider push/webhook, polling, manual update, internal delivery team. Normalize into the Fulfillment tracking model.

Do not trust provider callback payloads without verification where signed callbacks exist.

## X. Provider Callback Safety

Same principles as Payment where relevant: authenticate/verify callback, correlate provider shipment, idempotent processing, ignore duplicate/stale updates, prevent state regression, audit changes.

Exact signature capability is provider-specific.

## Y. Out-of-Order Events

Tracking can arrive out of order. Architecture must avoid regressions such as:

```text
Delivered -> InTransit
```

because a stale callback arrived later. Preserve provider event time and received time where useful.

## Z. Cancellation

Order cancellation may interact with: unfulfilled items, packed items, dispatched shipment, carrier cancellation, inventory release, refund/payment.

Cancellation is **not** one boolean. Need orchestration/compensation. Exact policies: `NEEDS_LATER_P00_DETAIL`.

## AA. Return Readiness

Fulfillment keeps identity/history for reverse logistics: return shipment, delivered quantity, delivery date, original shipment, seller, carrier, tracking.

Returns/RMA owns the case (`docs/architecture/25-returns-rma.md`). Fulfillment executes reverse-shipment commands; it does not own RMA eligibility, refund, or restock. Cancellation ≠ Return.

## AB. Failed Delivery

Handle: recipient unavailable, address problem, carrier exception, damaged package, refused delivery, lost shipment.

Fulfillment records the exception and orchestrates **approved** next steps. Do not automatically refund/cancel without policy.

## AC. COD

COD couples Fulfillment, Payment, and Order at the **policy** layer. Delivery may produce collection **evidence**. Payment remains owner of payment/financial status.

Fulfillment reports delivery/COD collection through contract/event. Fulfillment does **not** mark Payment succeeded by writing Payment tables.

## AD. B2B Delivery

Future extension points (not implemented): delivery windows, PO references, site/location delivery, contact person, partial fulfillment rules, scheduled delivery, proof of delivery.

Same Fulfillment module; process overlay, not a `B2BFulfillment` product fork.

## AE. Proof of Delivery

Future: signature, photo, recipient, timestamp, delivery code.

Media may own binary/photo assets. Fulfillment owns the semantic proof relationship. Authorization and privacy apply.

## AF. Fulfillment Events

Candidate facts (names not locked): FulfillmentCreated, FulfillmentReady, ShipmentCreated, ShipmentDispatched, ShipmentDelivered, ShipmentExceptionRaised, FulfillmentCancelled.

Consumers may include: Order, Notifications, Analytics, Audit, Support, Payment (COD evidence). Outbox/event ready. Versioned at the module edge.

## AG. Notifications Boundary

Fulfillment emits facts. Notification capability decides: customer/seller message, channel, template, retry.

Fulfillment does not own SMS/email provider logic.

## AH. Analytics Boundary

Analytics may observe: dispatch time, delivery time, failure rate, carrier performance, seller handling time.

Analytics does not own Fulfillment state. Observation ≠ operational authority. See `docs/architecture/16-first-party-analytics.md`.

## AI. Audit

High-impact manual/operational changes need durable business audit: manual dispatch, tracking override, cancel shipment, change carrier, mark delivered manually, exception resolution.

Technical logs are not sufficient. See `docs/architecture/18-observability-logging-audit.md`.

## AJ. Authorization

SpiceDB should govern: view fulfillment, prepare shipment, dispatch, edit tracking, cancel shipment, resolve exception, view proof of delivery.

Seller users act only on authorized seller fulfillment. Admin may have broader scoped permissions. “Own shipment” is a relationship, not client-supplied id trust.

## AK. Tenant Isolation

Single-Store fulfillment data is tenant-scoped. Jobs, provider callbacks, and reconciliation must carry tenant context.

Never resolve tenant by guessing from shipment identifier alone if global uniqueness/security is not guaranteed.

## AL. Idempotency

Required for: create fulfillment, create shipment, book carrier, cancel shipment, provider callback, manual retry, tracking ingestion.

Retries must not create duplicate shipments or provider bookings.

## AM. Concurrency

Guard: two workers dispatch the same shipment; seller and admin both update tracking; cancel while dispatching; duplicate provider callback; concurrent split of quantities.

State-transition / concurrency control later. No implementation now.

## AN. Reconciliation

First-class operational requirement. Examples: provider dispatched vs Tooba pending; provider delivered vs Tooba in transit; tracking missing; duplicate booking; cancelled internally but provider active; shipment with no provider record.

Need reconciliation jobs/reports. Corrections via module commands, not silent table edits across domains.

## AO. Manual Operations

Professional ops need safe intervention: assign carrier, enter tracking, retry booking, mark package ready, resolve exception, record manual delivery.

Every high-impact action requires: authorization, validation, audit, clear reason where appropriate.

## AP. Admin UX

Not raw CRUD. Workflow-oriented:

```text
Fulfillment Queue
Ready to Prepare
Packing
Ready to Dispatch
In Transit
Exceptions
Delivered
```

With: filters, seller/tenant scope, carrier, delivery method, date, priority, bulk actions where safe, tracking, customer/order context.

```text
Backend/module boundary != UI boundary
Build PASS != UI ACCEPT
Functional PASS != Visual ACCEPT
Desktop PASS != Mobile PASS
LTR PASS != RTL PASS
```

## AQ. Seller UX

Seller Panel: new fulfillment work, packing/dispatch queue, label/tracking, exceptions, delivery status, SLA/handling metrics.

Do not expose other sellers’ data. Do not clone Admin blindly.

## AR. Order Workspace Integration

Admin/Customer/Seller Order Workspace shows fulfillment coherently. Backend boundaries stay invisible.

Example customer timeline:

```text
Order confirmed
Preparing
Shipped
In transit
Delivered
```

without exposing internal packing/exception workflow complexity unless the role needs it.

## AS. Customer UX

Customer should see: shipment grouping, seller where Marketplace-relevant, tracking, delivery estimate, actual state, partial shipment, delivery exception.

UI must explain split shipments clearly.

## AT. Mobile UX

Seller/Admin operational flows may be used on mobile. Preserve patterns for: future scan/lookup, packing confirmation, tracking entry, dispatch, exception handling.

Do not require desktop-only workflows unnecessarily.

## AU. Search / Filter in Operations

Operational search: OrderId, ShipmentId, Tracking, Customer, Seller, Carrier, Status, Date.

Use operational read models / search contracts. Do **not** run cross-module ad-hoc joins.

## AV. Performance

Queues use purpose-built read models. Do not build high-volume screens by joining many module write tables per row. Pagination, filtering, sorting required.

## AW. Fulfillment Read Models

Potential projections: FulfillmentQueueItem, ShipmentTimeline, OrderFulfillmentSummary, SellerFulfillmentSummary, ExceptionQueue.

Projection data is **not** authority for state transitions.

## AX. Cache

Customer-facing tracking summaries may be cached briefly. State-changing operations must read authority.

Bounded freshness/invalidation to avoid stale “Delivered” vs “Not shipped”. See `docs/architecture/19-caching-infrastructure-abstractions.md`.

## AY. Observability

Need: fulfillment creation lag, packing duration, dispatch lag, carrier booking errors, tracking callback lag, delivery success rate, exception rate, reconciliation mismatch, seller SLA.

Integrate with OpenTelemetry and Analytics/Audit boundaries. Technical telemetry ≠ business audit.

## AZ. Failure Matrix

| Case | Retry? | Fail closed? | Manual intervention? | Compensate? | Alert? | Customer-visible state? |
| --- | --- | --- | --- | --- | --- | --- |
| Carrier unavailable | Yes, bounded | Do not fake dispatch | If retries exhaust | No auto-refund | Yes | Stay Preparing / Exception |
| Carrier booking timeout | Idempotent retry | No silent success | If unknown outcome | Reconcile booking | Yes | Unchanged until confirmed |
| Duplicate booking | Dedup / reconcile | N/A | Ops if two live bookings | Cancel extras per policy | Yes | One shipment shown |
| Tracking callback invalid | No | Ignore payload | If repeated | No | Yes | Unchanged |
| Tracking callback duplicate | Idempotent no-op | N/A | No | No | Low | Unchanged |
| Out-of-order status | No apply if stale | Guard transition | If conflict unresolved | No | Medium | Last **valid** state |
| Shipment cancel rejected | Retry cancel; then ops | Keep dispatched truth | Yes | Carrier + inventory policy | Yes | Dispatched / Exception |
| Inventory release failure | Retry command | Do not orphan stock | Yes | Saga retry | Yes | Internal until resolved |
| Order update event failure | Outbox retry | Fulfillment remains source of execution | If poison | Replay event | Yes | Derived Order status may lag |
| Missing address snapshot | No ship | Yes | Fix from Order snapshot/policy | No auto-use address book | Yes | Cannot dispatch |
| No eligible delivery method | No create unit | Checkout/eligibility fail | Merch/ops config | No | Medium | Checkout/order policy |
| Partial shipment inconsistency | No illegal qty | Yes | Ops + audit | Reverse invalid ship | Yes | Honest partial + remainder |
| Cross-seller data leak risk | N/A | Deny | Security review | N/A | Yes | Denied / scoped empty |

Default: fail closed on unknown tenant, unauthorized actor, unverified callback, and impossible quantity.

## BA. Testing Strategy — Architecture Level

Future implementation must cover: single shipment; multi-seller split; partial quantity; multi-location split; COD; prepaid; provider callback verification; duplicate/out-of-order events; cancellation race; tracking; tenant isolation; seller authorization; reconciliation; manual override audit; customer timeline; mobile/RTL UX.

No tests now.

## BB. Data Ownership Matrix

Marks: `OWNER` | `SOURCE` | `REFERENCE` | `CONSUMER` | `PROJECTION` | `NOT_OWNER`

| Fact | Order | Payment | Inventory | Fulfillment | Seller | Party | Pricing | Notifications | Analytics | Audit | Media | Authorization |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Ordered quantity | OWNER | NOT_OWNER | CONSUMER | CONSUMER | REFERENCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | NOT_OWNER | NOT_OWNER |
| Reserved quantity | NOT_OWNER | NOT_OWNER | OWNER | REFERENCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | NOT_OWNER | NOT_OWNER |
| Shipment quantity | NOT_OWNER | NOT_OWNER | CONSUMER | OWNER | REFERENCE | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | NOT_OWNER | NOT_OWNER |
| Delivery address snapshot | OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER | SOURCE (book) | NOT_OWNER | CONSUMER | NOT_OWNER | CONSUMER | NOT_OWNER | NOT_OWNER |
| Shipping charge | OWNER (accepted snapshot) | CONSUMER | NOT_OWNER | SOURCE (rate input) | REFERENCE | NOT_OWNER | OWNER (compose) | NOT_OWNER | CONSUMER | CONSUMER | NOT_OWNER | NOT_OWNER |
| Carrier rate | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER (provider quote) | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER | CONSUMER | CONSUMER | NOT_OWNER | NOT_OWNER |
| Tracking | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | NOT_OWNER | NOT_OWNER |
| Payment state | REFERENCE / PROJECTION | OWNER | NOT_OWNER | CONSUMER (eligibility) | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | NOT_OWNER | NOT_OWNER |
| COD collection | CONSUMER | OWNER | NOT_OWNER | SOURCE (evidence) | CONSUMER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | NOT_OWNER | NOT_OWNER |
| Seller responsibility | REFERENCE | NOT_OWNER | REFERENCE | OWNER (execution) | OWNER (profile) | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | NOT_OWNER | CONSUMER |
| Customer notification | NOT_OWNER | NOT_OWNER | NOT_OWNER | SOURCE (facts) | NOT_OWNER | REFERENCE | NOT_OWNER | OWNER | CONSUMER | CONSUMER | NOT_OWNER | NOT_OWNER |
| Proof-of-delivery asset | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER (semantic link) | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | OWNER (binary) | CONSUMER |

Address book remains Party current facts; shipment destination is the Order snapshot Fulfillment consumes.

## BC. Decision Summary

### RECOMMENDED_FOR_ADR

1. Fulfillment is separate from Order, Payment and Inventory.
2. One Order may create multiple fulfillment units/shipments.
3. Marketplace seller fulfillment responsibilities remain separable.
4. Single-Store reuses same fulfillment architecture.
5. Shipping Method is distinct from Carrier/Provider.
6. Carrier/provider integrations are behind internal adapters.
7. Order snapshots delivery destination; Fulfillment does not use mutable address-book truth.
8. Inventory remains stock/reservation authority.
9. Payment policy affects fulfillment eligibility but Payment state is not Fulfillment state.
10. Shipping raw carrier rate is not automatically customer shipping price.
11. Partial and split shipment are first-class.
12. Provider callbacks/tracking updates are idempotent and state-regression-safe.
13. Cancellation requires orchestration/compensation, not a boolean.
14. Full Returns/RMA remains a separate future capability.
15. COD evidence does not let Fulfillment directly mutate Payment.
16. Fulfillment is tenant- and seller-authorized through SpiceDB.
17. Reconciliation is first-class.
18. Manual operational actions are authorized and audited.
19. Fulfillment UI is queue/workflow/workspace oriented, not CRUD.
20. Order Workspace presents coherent shipment timeline regardless of backend module boundaries.
21. Operational read models support high-volume workflows without cross-module joins.
22. Fulfillment architecture is future 3PL/multi-location/microservice ready.

Do not create final ADR yet.

### NEEDS_LATER_P00_DETAIL

- Exact fulfillment eligibility orchestration
- Exact shipping-charge composition authority (Pricing vs Checkout vs Fulfillment quote inputs)
- Exact cancellation policies by shipment state
- Promise-engine inputs and SLA calendars
- Provider callback signature/verification matrix per carrier
- Sub-order vs seller-partition naming alignment with T011

### DEFERRED

- Implementation, schemas, carrier/3PL SDKs, labels, tracking jobs
- Full Returns/RMA architecture
- Full B2B delivery product
- Notifications implementation, Fulfillment UI, Shopeiva, P00 Gate, ADR
