# Tooba — Checkout & Order Foundation

Status:

```text
IN_PROGRESS — TB-P03-T006 awaiting Architect ACCEPT
```

Task:

```text
TB-P03-T006
```

## Purpose

Checkout is the orchestration that turns an Active Cart into commercial Order history. Cart is not Order. Order is not Payment. Order is not Fulfillment. Order is not Inventory or Pricing source of truth.

Both locked commerce flows originate from Cart:

- `RequestToReserve` — a reservation/request. Payment is not required. This is not an unpaid Online Purchase.
- `OnlinePurchase` — a payment-capable lifecycle. Paid is not recorded here.

## Ownership

Module `Order` owns `OrderDbContext`, schema `order`, migrations, and the write model.

Cross-module work uses contracts only:

- Cart: `ICartQueryGateway`, `ICartDirectory.ConvertAsync`
- Offer: `IOfferLookupGateway`
- Pricing: `IPriceLookupGateway`
- Inventory: `IInventoryDirectory.ReleaseAsync` on cancel

No foreign DbContext, no cross-module FK, no Host parsing inside Order.

## Checkout orchestration

1. Load Cart through Cart contracts. `CartId` is not bearer authority; `CartAccess` is required.
2. Revalidate Offer (must be Active; seller must match the cart line).
3. Re-resolve current price. Cart quote is UX only.
4. If cart quote ≠ current Pricing: fail with `PRICE_CHANGED`. Do not silently charge a different amount.
5. Require each cart line to already carry a reservation id (Cart hold). Do not consume stock on Order insert.
6. Persist `CheckoutGroup` + per-seller `SellerOrder` + immutable line snapshots.
7. Convert the Cart with the matching `CartConversionIntent`.
8. No distributed transaction. `IdempotencyKey` uniqueness is the duplicate-submit seam.

Guest full checkout is deferred: `PlacedByUserId` is required in this foundation.

## Buyer vs acting user

- `BuyerPartyId` — economic/customer party (optional/future-ready).
- `PlacedByUserId` — authenticated principal performing checkout. Not the seller organization identity.

These fields are not collapsed. `OrderNumber` is a support-friendly reference, not an authorization credential. Read APIs require `OrderAccess` matching buyer or actor.

## Multi-seller structure

One customer checkout is `CheckoutGroup`. Each seller gets a `SellerOrder` with its own status and lines. Seller-specific cancellation/fulfillment remains possible. A single monolithic order row is not used.

## Status model (foundation)

Seller order:

- `OnlinePurchase` → `PendingPayment` (not Paid)
- `RequestToReserve` → `ReservationRequested`
- `Cancelled` — releases inventory reservations through Inventory contracts

Payment capture, reservation accept/reject by merchant, and fulfillment transitions are later modules.

## Totals

Line and order totals are historical snapshots (tax-exclusive base). Tax and discount engines are not implemented; placeholders are zero-by-policy and must not be read as calculated tax.

## Inventory handoff

Cart owns the temporary hold. Checkout copies the opaque `ReservationId` onto order lines. Inserting an Order row does not consume stock. Cancel calls `IInventoryDirectory.ReleaseAsync`. Duplicate submit with the same `IdempotencyKey` returns the existing checkout and does not create a second handoff.

## Events

- `order.checkout_submitted.v1`
- `order.seller_order_created.v1`

No payment-success events. No MassTransit types in Domain/Application.

## Tenant / edition

Marketplace orders live in the marketplace database. Single-store orders live in the tenant database via existing tenant-aware persistence. Tenant A cannot load Tenant B checkout rows.

## Out of scope

Payment gateway, paid state, fulfillment/shipment, returns, invoicing, tax/promo engines, commercial UI, TB-P03-T007, P03 Gate.
