# Tooba — Inventory, Availability & Reservation Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T010
```

Documentation only. Inventory != Catalog, != Offer, != Price, != Fulfillment shipment.

## A. Core Separation

Inventory owns **availability and reservation write-model** for sellable identities (typically Offer/SKU at a location).

Must not own: product titles, prices, seller legal identity, payment capture, shipment tracking as SoT.

## B. Inventory Scope

Scoped to Offer (and/or Variant+Seller) **within tenant/deployment**. Marketplace: per seller offer. Single-Store: same seams with implicit seller/location.

## C. Stock Identity

Stock is identified by sellable id (OfferId/VariantId+Seller) + location id — not by Product title, not by seller SKU as the only key (seller SKU is Offer-local; inventory keys off Offer/canonical sellable id).

## D. Warehouse / Location Model

Candidate locations: warehouse, store, virtual/dropship. Multi-warehouse **readiness** without requiring multi-WH UX on day one. Exact location taxonomy: `NEEDS_LATER_P00_DETAIL`.

## E. Quantity Semantics

Separate conceptually: on-hand, reserved, available-to-sell, incoming (optional). Available is derived, not three independent truths that can drift without a rule.

Do not lock schema.

## F. Reservation

Reservation is a time-bounded hold against available quantity for a cart/checkout/order line, with id, quantity, sellable, location, owner (cart/checkout/order), expiry, status.

Not a stock adjustment that silently loses audit.

## G. Reservation Timing

Candidate: reserve at add-to-cart vs at checkout start vs at payment. Tradeoffs: oversell vs abandoned-cart lock. Exact timing: `NEEDS_LATER_P00_DETAIL`. Architecture must support expiry and release.

## H. Oversell Protection

Fail closed when available < requested (unless explicit backorder policy). No silent negative available as success. Concurrency must not double-spend the last unit (U).

## I. Inventory Adjustment

Receipts, counts, damage, returns-to-stock are Inventory commands with reason/audit. Not Catalog edits. Not “set Product.Stock = n” from Admin as unofficial path.

## J. Stock Movement Ledger Direction

Recommend append-only movement facts (or equivalent) so on-hand is reconcilable. Exact ledger vs snapshot: later. Not optional for professional ops.

## K. Seller / Marketplace Inventory

Each seller’s offer stock is isolated. Seller A cannot consume Seller B. Catalog Product has no stock field.

## L. Single-Store Inventory

Same module. One implicit location/seller allowed. Do not merge Inventory into Product.

## M. Inventory vs Fulfillment

Inventory: can we sell / hold. Fulfillment: pick/pack/ship of a **confirmed** order unit. Shipping a unit decrements/commits reservation via contract, not by Fulfillment writing Inventory tables.

## N. Market / Channel Availability

Inventory quantity ≠ market eligibility. An item may have stock but be unsellable in a Market (Offer/Market policy). Both checks needed at quote/cart.

## O. Backorder / Preorder / Made-to-Order

Preserve future policies as **explicit** offer/inventory modes, not negative stock hacks. Not implemented now.

## P. Inventory Quote / Availability Check

Synchronous **query contract** at PDP/cart/checkout: subject sellable + qty + location/policy → available/deny. Not a Search index as SoT.

## Q. Cart Interaction

Cart holds lines and may request reservation per policy. Cart does not own stock counts.

## R. Checkout Interaction

Checkout extends/creates reservations; expiry must cover payment window or re-reserve. Failure releases holds.

## S. Order Interaction

On confirm: convert reservation to committed allocation / decrement per policy. Order snapshots qty; live stock remains Inventory.

## T. Payment Interaction

Payment failure / timeout must **release** reservations (event/reaction). Do not leave orphan holds. Payment does not own stock.

## U. Concurrency / Idempotency

Reserve/commit/release must be idempotent (reservation id / command id). Concurrent last-item: one winner, one fail-closed. No cross-module DB lock as the architecture.

## V. Event Model

Facts: reserved, expired, released, committed, adjusted. Consumers: Cart, Checkout, Order, Search projection, Notifications. Outbox-ready (T004).

## W. Search / PDP / PLP Projection

In-stock badges are **projections**. Search must not update Inventory. Stale badge ≠ sell permission; checkout rechecks.

## X. Cache

Short TTL / versioned availability cache optional. No Redis required. Never cache across tenants/sellers. Security-insensitive but commercially sensitive (oversell).

## Y. First-Party Analytics

Stockout/reservation events may emit analytics facts (ids, not dumps). Analytics is not inventory SoT.

## Z. Admin / Seller UX Implications

Seller/Admin adjust via Inventory commands; lists from projections. SpiceDB gates who may adjust which seller/location.

## AA. External Inventory / ERP Integration

ERP/WMS behind adapter. External system is not an excuse for Catalog to hold stock. Sync direction later; fail closed if sync uncertain on sell path if policy requires.

## AB. Reconciliation

Periodic compare ledger vs physical/ERP. Corrections are adjustments with audit, not silent Search edits.

## AC. Tenant Isolation

Single-Store: stock never leaks across stores. Marketplace: never across sellers. Tenant context from platform, not Host parse in Inventory.

## AD. Data Ownership Matrix

| Concept | Owner |
| --- | --- |
| On-hand / reserved / ATS | Inventory |
| Reservation lifecycle | Inventory |
| Offer bind | Offer |
| Price | Pricing |
| Cart lines | Cart |
| Shipment | Fulfillment |
| In-stock search field | Search projection |

## AE. Failure Matrix

| Case | Direction | Fail closed? |
| --- | --- | --- |
| Insufficient ATS | Deny reserve/sell | Yes |
| Expired reservation | Re-check; no assume hold | Yes |
| Duplicate reserve command | Idempotent same hold | N/A |
| Inventory service down | Do not sell on guess | Yes for sell path |
| Tenant/seller mismatch | Deny | Yes |
| Payment fail | Release hold | Yes (release) |

## AF. Testing Strategy — Architecture Level

Future: last-item race, expiry, payment-fail release, seller isolation, tenant isolation, idempotent commit, projection lag vs checkout recheck. No tests now.

## AG. Decision Summary

### RECOMMENDED_FOR_ADR

1. Inventory owns availability/reservation, not Catalog/Offer/Price.
2. Marketplace stock is seller/offer-scoped.
3. Single-Store keeps Inventory as its own write model.
4. Reservation as first-class hold with expiry.
5. Oversell: fail closed unless explicit backorder mode.
6. Ledger/movement direction for reconcilable stock.
7. Fulfillment does not own ATS write model.
8. Checkout/payment release reservations on failure.
9. Idempotent reserve/commit/release.
10. Search in-stock is projection only.
11. Tenant/seller isolation on all inventory operations.
12. External WMS/ERP behind adapter.

### NEEDS_LATER_P00_DETAIL

- Reserve-at-cart vs checkout
- Multi-warehouse topology
- Quantity field formulas
- Backorder/preorder modes
- ERP sync direction

### DEFERRED

- Implementation, schemas, WMS vendor, reservation algorithm code, UI, ADR, Shopeiva
