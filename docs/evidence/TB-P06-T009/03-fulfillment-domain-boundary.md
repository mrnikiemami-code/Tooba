# 03 — Fulfillment domain boundary (TB-P06-T009)

## Architecture lock

```text
Order != Fulfillment != Shipment
Checkout/SellerOrder (order schema) ≠ FulfillmentUnit (fulfillment schema)
```

## Module layout

| Layer | Project | Contents |
|---|---|---|
| Domain | `Tooba.Fulfillment.Domain` | `FulfillmentUnit`, `FulfillmentItem`, `Shipment`, `ShipmentItem`, status enums, domain events |
| Application | `Tooba.Fulfillment.Application` | `IFulfillmentDirectory`, `IFulfillmentInventoryGateway`, snapshots, integration event contracts |
| Infrastructure | `Tooba.Fulfillment.Infrastructure` | `FulfillmentDirectory`, `FulfillmentDbContext`, payment handler, inventory gateway |
| Host | `Tooba.Host/Fulfillment/` | HTTP endpoints, panel composer |

## Cross-module rules

- Fulfillment.Infrastructure references `Tooba.Order.Application` only (not Order.Infrastructure).
- Order.Infrastructure references `Tooba.Order.Application`; implements `IOrderFulfillmentReader` via `OrderFulfillmentBridge`.
- No cross-module SQL joins; handoff via `OrderFulfillmentHandoffSnapshot`.
- Domain/Application csproj: no MassTransit, no Authzed.

## Schema

- PostgreSQL schema: `fulfillment`
- Tables: `fulfillments`, `items`, `shipments`, `shipment_items`, `payment_inbox`, `outbox_messages`
