# Tooba — Cart Foundation

Status:

```text
IN_PROGRESS — TB-P03-T005 awaiting Architect ACCEPT
```

Task:

```text
TB-P03-T005
```

## Purpose

Cart is a durable commercial basket. It is not Order, Payment, Inventory, or the pricing source of truth. Lines target `OfferId` (seller-specific), not Product-only.

Both future order models originate here: Request-to-Reserve and Online Purchase. Conversion is a seam only; no Order aggregate is created.

## Ownership and access

- Authenticated carts are bound to `UserId`.
- Guest carts use a high-entropy secret. Only a SHA-256 hash is stored. The raw secret is returned once at create.
- `CartId` is not bearer authority.

## Quantity and commercial context

Quantity is a positive integer (max 99). Float quantity is rejected.

Cart carries Market, Currency, and SalesChannel. Currency is not Locale.

## Pricing snapshot

Quoted amount comes from `IPriceLookupGateway` for Offer + Market + Channel + Currency + Quantity + At. The snapshot is UX only and is not checkout truth. Price is not stored on Product or Offer.

## Inventory holds

Cart never opens Inventory DbContext. It reserves through `IInventoryDirectory` with opaque external reference `cart:{cartId}` and line/quantity idempotency keys.

Default hold TTL is 30 minutes UTC on the cart and on the reservation. Expired carts and expired inventory holds are released by server-side UTC, not client timers.

Cart does not hold more reserved quantity than the current line quantity.

## Failure policy (no distributed transaction)

1. Reserve inventory first, then persist the cart line.
2. If cart persist fails, the reservation remains keyed by idempotency; retry reuses it.
3. If a quantity change releases then fails to re-reserve, Cart attempts to restore the previous hold and returns a clear failure.

## Marketplace

One cart may contain offers from multiple sellers. Seller-order split is out of scope.

## Events

- `cart.created.v1`
- `cart.line_added.v1`
- `cart.line_changed.v1`
- `cart.line_removed.v1`
- `cart.expired.v1`
- `cart.converted.v1`

## Out of scope

Checkout, Order, Payment, shipping, promotions, tax calculation, commercial UI, TB-P03-T006, P03 Gate.
