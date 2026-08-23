# Tooba — Inventory Foundation

Status:

```text
COMPLETE — Architect accepted TB-P03-T004
```

Task:

```text
TB-P03-T004
```

## Purpose

Inventory owns on-hand, reserved, and derived available quantity for an Offer at a location. Product and Offer have no stock columns. Price does not imply availability.

## Target identity

Primary commercial key is `OfferId` (seller-specific Marketplace stock). `CatalogVariantId` is copied via `IOfferLookupGateway` / `ICatalogLookupGateway` for descriptive lookup. No FK to offer or catalog schemas.

## Location

`InventoryLocation` is a minimal site (code, name, status). One Offer may have positions in multiple locations. Aggregation must not erase location identity.

## Stock math

```text
Available = OnHand - Reserved
```

Available is not stored as an independent column. Integers only. Illegal: OnHand &lt; 0, Reserved &lt; 0, Reserved &gt; OnHand.

## Adjustments

Increase / Decrease / Set with an operational reason. External callers do not assign OnHand directly.

## Reservations

Hold / Release / Consume with durable `ReservationId`, optional external/idempotency keys, and optional UTC `ExpiresAt`. Released or consumed holds clear the idempotency key so a later hold can reuse the same client key. Not a Cart or Order module.

## Concurrency

Reservation uses a single PostgreSQL `UPDATE ... WHERE OnHand - Reserved >= quantity` (`ExecuteUpdateAsync`). Two concurrent claims of the last unit cannot both succeed.

## Marketplace vs Single-Store

Same abstraction. Marketplace data lives on the marketplace database; Single-Store data lives on the tenant database. Tenant A stock never resolves in Tenant B.

## Events

- `inventory.adjusted.v1`
- `inventory.reserved.v1`
- `inventory.released.v1`
- `inventory.reservation_consumed.v1`
- `inventory.availability_changed.v1`

## Out of scope

Cart, checkout, order, fulfillment, procurement, warehouse UI, seller portal, Buy Box, tax, payment, TB-P03-T005, P03 Gate.
