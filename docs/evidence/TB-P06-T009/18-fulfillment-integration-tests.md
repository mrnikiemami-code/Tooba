# 18 — Fulfillment integration tests (TB-P06-T009)

## Test class

`src/backend/Host/Tooba.Host.Tests/FulfillmentFoundationTests.cs`

Collection: `PostgresSerial` (Testcontainers PostgreSQL 16)

## Test cases

| Test | Type | Coverage |
|---|---|---|
| `Fulfillment_is_not_order_and_modules_do_not_reference_each_other_infrastructure` | Unit (no Docker) | Domain types distinct; schema names; csproj reference boundaries; no MassTransit/Authzed in Domain/Application |
| `Paid_order_handoff_is_idempotent_and_shipment_lifecycle_preserves_boundaries` | Integration (SkippableFact) | Paid handoff; payment inbox dedup; unpaid rejection; address snapshot immutability; Processing→Packed→CreateShipment→Tracking→Dispatch→Deliver; inventory consume on dispatch; overflow shipment rejected; tracking idempotency/immutability; customer checkout list |
| `Multiple_shipments_and_seller_scoped_listing_work_on_postgres` | Integration (SkippableFact) | Multi-seller checkout → per-seller fulfillments; seller-scoped listing; two partial shipments for qty-2 line |

## Infrastructure under test

- `FulfillmentDirectory`, `OrderFulfillmentBridge`, `RecordingInventoryGateway`
- Migrations: `order` + `fulfillment` schemas on Testcontainers

## Expected build

- 0 warnings, 0 errors after tests added.
