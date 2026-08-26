# Tooba — Fulfillment Operations

Fulfillment orchestrates post-payment shipping: one unit per Paid SellerOrder, multi-shipment support, inventory consume on dispatch.

## Architecture locks

```text
Order != Fulfillment != Shipment
Fulfillment status != SellerOrder status
Address snapshot immutable at creation
Inventory consume via IInventoryDirectory (not direct DB)
No cross-module SQL joins (IOrderFulfillmentReader handoff)
```

## States

### FulfillmentUnit

| Status | Meaning |
|---|---|
| `ReadyToFulfill` | Created from Paid order |
| `Processing` | Warehouse started |
| `Packed` | Ready to ship |
| `Dispatched` | Shipment(s) dispatched; not fully delivered |
| `InTransit` | Partial delivery |
| `Delivered` | All lines shipped and delivered |
| `Failed` / `Cancelled` | Terminal (reserved) |

### Shipment

| Status | Meaning |
|---|---|
| `Created` | Awaiting tracking + dispatch |
| `Dispatched` | Left warehouse |
| `InTransit` | (via fulfillment unit when partial) |
| `Delivered` | Confirmed delivery |
| `Failed` / `Cancelled` | Terminal (reserved) |

## Handoff (Paid → Fulfillment)

```text
payment.succeeded.v1
  → FulfillmentPaymentSucceededHandler
  → FulfillmentDirectory.CreateFromPaidSellerOrdersAsync
      • dedup: fulfillment.payment_inbox (event_id)
      • dedup: unique ix_fulfillments_seller_order_id
      • read: IOrderFulfillmentReader.GetHandoffAsync
      • create: FulfillmentUnit + items (ReservationId from order lines)
      • outbox: fulfillment.created.v1
```

Requires `SellerOrderStatus.Paid`. Unpaid handoff throws.

## Seller workflow

1. **List** — `GET /v1/seller/fulfillments`
2. **Detail** — `GET /v1/seller/fulfillments/{id}`
3. **Processing** — `POST .../processing`
4. **Packed** — `POST .../packed`
5. **Create shipment** — `POST .../shipments` with `{ carrierDisplayName, items: [{ orderLineId, quantity }] }`
6. **Assign tracking** — `POST .../shipments/{shipmentId}/tracking` with `{ trackingReference }`
7. **Dispatch** — `POST .../shipments/{shipmentId}/dispatch` (consumes inventory reservation when line fully shipped)
8. **Deliver** — `POST .../shipments/{shipmentId}/deliver`

All seller routes require `SellerPanelAccess` authorization and `SellerPartyId` ownership.

## Admin workflow

Read-only in foundation release:

- `GET /v1/admin/fulfillments`
- `GET /v1/admin/fulfillments/{id}`

Requires `AdminPanelAccess`.

## Customer visibility

- `GET /v1/customer/orders/{checkoutId}/fulfillments`
- Returns all seller fulfillments for checkout (marketplace: multiple units).
- Ownership: checkout must belong to authenticated customer.

No customer panel UI in this release; API only.

## Tracking

- Tracking reference required before dispatch.
- Immutable after first assignment (same value re-post is idempotent).
- Unique DB constraint on non-null tracking references.

## Partial shipments

- Multiple shipments per fulfillment allowed.
- Each shipment specifies per-line quantities.
- Total shipped per line cannot exceed `QuantityOrdered`.
- Fulfillment reaches `Delivered` only when every line is fully shipped and all shipments delivered.

## Inventory

- Reservation ID copied from order line at fulfillment creation.
- Consumed on dispatch via `IFulfillmentInventoryGateway` → `IInventoryDirectory.ConsumeAsync`.
- Consume when `QuantityShipped >= QuantityOrdered` for that line.
- Fulfillment never opens Inventory DbContext.

## Authorization

| Actor | Guard | Scope |
|---|---|---|
| Seller | `SellerPanelAccess` | Own `SellerPartyId` fulfillments only |
| Admin | `AdminPanelAccess` | All fulfillments (read) |
| Customer | Session + checkout ownership | Own checkout fulfillments (read) |

## Schema

PostgreSQL schema: `fulfillment`

| Table | Purpose |
|---|---|
| `fulfillments` | FulfillmentUnit |
| `items` | FulfillmentItem |
| `shipments` | Shipment |
| `shipment_items` | ShipmentItem |
| `payment_inbox` | payment.succeeded dedup |
| `outbox_messages` | Integration event outbox |

Migration: `20260827010000_InitialFulfillment`

## Observability

Metrics (no PII):

- `tooba.fulfillment.created`
- `tooba.fulfillment.transition` (tag: `outcome`)
- `tooba.fulfillment.shipment.created`
- `tooba.fulfillment.tracking.assigned`
- `tooba.fulfillment.dispatched`
- `tooba.fulfillment.delivered`

Outbox events: `fulfillment.created.v1`, `shipment.dispatched.v1`, `shipment.delivered.v1`

## Background processing

No fulfillment-specific background workers. Creation is event-driven; outbox dispatch uses shared host worker.

## Deferred

- Returns / RMA workflow
- Carrier API integration and automatic InTransit updates
- Seller/admin/customer panel UI for fulfillment
- Failed/Cancelled operational flows
- Background reconciliation for stuck fulfillments
