# Tooba — Cart, Checkout & Order Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T011
```

Documentation only.

```text
Cart != Checkout != Order
```

## A. Core Separation

**Cart** = pre-order commercial session (lines, selected context).  
**Checkout** = capture process (revalidation, reservation, payment initiation, placement attempt).  
**Order** = confirmed commercial record with snapshots.

No module may treat these as one table with a status flag as the whole design.

## B. Cart Ownership

Cart owns cart lines and session commercial context: sellable/offer refs, qty, selected market/currency/channel, optional identity/party, applied promotion **refs**. Does not own live price books, stock, or catalog copy.

## C. Guest Cart

Guest cart is allowed (T007). Identified by opaque cart/session id, not login. Merge policy after login: `NEEDS_LATER_P00_DETAIL` (must not invent silent overwrite).

## D. Authenticated Cart

Bound to Identity; may link Buyer Party. Actor != Buyer still applies if B2B later.

## E. Marketplace Multi-Seller Cart

One cart may contain lines from multiple sellers. Partition by seller/offer for reservation, fulfillment grouping, and payment policy later. Do not force one-seller carts unless product later decides.

## F. Single-Store Cart

Same Cart module. Implicit seller. UX need not show seller.

## G. Cart Line Validity

Lines reference Offer/Variant ids. Stale catalog/offer (ended, unpublished) fails at revalidation, not by joining Catalog tables from Cart.

## H. Cart Pricing

Cart **displays** quotes via Pricing contract. Cart does not author prices. Stale display ≠ charge amount.

## I. Cart Inventory

Optional reservation per T010 timing policy. Cart does not store ATS as truth.

## J. Checkout Responsibility

Checkout owns the process: revalidate price/availability/eligibility, ensure reservations, collect addresses, start payment, place order **or** fail closed and compensate.

## K. Checkout Session

Distinct from Cart. Created from a cart snapshot/version. Concurrent checkouts: idempotency keys. Cart may remain until success policy.

## L. Checkout Revalidation Pipeline

```text
resolve tenant
lock/version cart
re-quote (Pricing)
re-check availability (Inventory)
re-check offer/market eligibility
re-check authz (SpiceDB) where needed
apply promotions
compute payable
```

Any fail → no Order (or no capture) per failure matrix.

## M. Order Placement Boundary

Order is created only after Checkout decides confirm policy (see U). Command: PlaceOrder. Event: OrderPlaced. Cart/Checkout do not remain the long-term commercial SoT.

## N. Order Ownership

Order owns confirmed header/lines **snapshots**, buyer/actor/payer refs, market/channel/currency, totals, quote/version refs, payment/fulfillment **refs** (not PSP internals, not live stock).

## O. Order Snapshot Policy

Snapshot: product title/sku as sold, offer id, unit/extended amounts, tax/fee as quoted, promotions, FX if any, addresses. Later catalog/price/membership changes do not rewrite history.

## P. Order Line

Line = sellable + qty + commercial snapshot + seller/offer partition. Not a live Catalog row.

## Q. Order Status vs Payment/Fulfillment Status

Separate: Order commercial state, Payment state, Fulfillment state. Do not collapse to one `Status` that means paid-and-shipped.

## R. Marketplace Sub-Orders / Seller Partitioning

Candidate: one customer Order with seller partitions / sub-orders. Exact split: `NEEDS_LATER_P00_DETAIL`. Must not merge seller money/stock.

## S. Fulfillment Grouping

Fulfillment units derived from partitions/locations via contract after Order exists. Order does not ship. See `docs/architecture/21-fulfillment.md`.

## T. Payment Initiation

Checkout/Order **commands** Payment module. Payment owns attempts/PSP refs (T004).

## U. Payment Before vs After Order Creation

Both patterns exist. Direction: prefer **Order (or OrderIntent) id before capture** so payment is not orphaned; exact sequence `NEEDS_LATER_P00_DETAIL`. Must be idempotent and compensatable.

## V. Failure / Compensation

On fail: release inventory holds, void/cancel payment attempt, do not leave a sellable Order if policy is fail-closed. Process manager/outbox later (T004).

## W. Checkout Expiry

Checkout and reservations expire. Expired checkout cannot place. User restarts from cart/revalidation.

## X. Promotion Interaction

Promotions via Pricing quote step; Order snapshots applied promotions. Cart stores selected codes/refs only. See `docs/architecture/22-promotion-discount.md`.

## Y. Address Model

Checkout collects shipping/billing; Order **snapshots** addresses. Party address book is current facts, not historical Order truth (T007).

## Z. Market / Locale / Currency

Cart/Checkout carry Market and Currency from context. Locale is presentation. Host/tenant already resolved.

## AA. Sales Channel

Channel on cart/checkout/order snapshot (T007/T009). Not Identity.

## AB. B2B Approval Readiness

Checkout may later pause in Request-to-Buy before PlaceOrder. Actor vs Buyer Party preserved on Order.

## AC. Request-to-Buy vs Online Purchase

Same Order module; different process. Do not fork Order schema into “B2BOrder”.

## AD. Cancellation

Order-owned cancellation policy + inventory release + payment void/refund commands. Eligibility is business rule after SpiceDB check.

## AE. Returns / Refund Readiness

Preserve later return units referencing Order lines. Not implemented. Payment refunds via Payment module.

## AF. Customer / Seller / Admin Views

All via projections/composition. Seller sees own partition. No mega-joins.

## AG. Events

CartUpdated (optional), CheckoutStarted, OrderPlaced, OrderCancelled, plus payment/inventory events consumed. Versioned, outbox-ready.

## AH. Idempotency

PlaceOrder, pay, reserve keyed by checkout/command ids. Duplicate submit ≠ duplicate orders.

## AI. Security / Authorization

SpiceDB: who may view/cancel/manage. “Own order” is relationship, not client-supplied OrderId trust. Guest access via secret/token later — fail closed if uncertain.

## AJ. Observability / Audit

Correlate checkout/order/payment ids. No PAN/secrets in logs. Business audit ≠ technical logs.

## AK. Reconciliation

Payment vs Order amounts; inventory commits vs lines. Corrections via module commands.

## AL. Data Ownership Matrix

| Concept | Owner |
| --- | --- |
| Cart lines / session | Cart |
| Checkout process | Checkout |
| Confirmed order + snapshots | Order |
| Quote | Pricing |
| Reservation | Inventory |
| Payment attempt | Payment |
| Shipment | Fulfillment |

## AM. Failure Matrix

| Case | Direction | Fail closed? |
| --- | --- | --- |
| Price changed | Requote or abort | Yes for charge mismatch |
| Insufficient stock | Abort; release other holds per policy | Yes |
| Payment fail | No successful Order (or unpaid intent only per U) | Yes |
| Checkout expired | Restart | Yes |
| Duplicate place | Return original | Idempotent |
| Tenant mismatch | Deny | Yes |
| Unauthorized | Deny | Yes |

## AN. Testing Strategy — Architecture Level

Future: guest cart, multi-seller partition, revalidation races, idempotent place, payment fail compensation, snapshot immutability, B2B actor/buyer. No tests now.

## AO. Decision Summary

### RECOMMENDED_FOR_ADR

1. Cart != Checkout != Order.
2. Guest cart allowed.
3. Multi-seller cart partitionable.
4. Single-Store same modules, implicit seller.
5. Checkout revalidates price and inventory.
6. Order owns historical snapshots.
7. Payment and fulfillment statuses distinct from order commercial state.
8. Idempotent placement.
9. Compensation releases reservations and payment attempts.
10. Addresses snapshotted on Order.
11. B2B approval is process overlay, not a second Order product.
12. Views via projections; no cross-module joins.

### NEEDS_LATER_P00_DETAIL

- Guest-to-auth cart merge
- Payment vs Order creation sequence
- Sub-order vs partition model
- Reservation timing (align T010)

### DEFERRED

- Implementation, schemas, PSP, UI, return product, ADR, Shopeiva

Checkout/Order emit server-confirmed facts Analytics may observe. Analytics does not own cart/order state. See `docs/architecture/16-first-party-analytics.md`.
