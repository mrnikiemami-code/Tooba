# Cross-module boundary audit — TB-TMAR-NEXT-MODULE-BATCH-003

## Notification.Infrastructure references
- Order.Application (recipient reader — allowed; not in remove list)
- Payment.Contracts / Fulfillment.Contracts / Returns.Contracts (events)
- NO Payment/Fulfillment/Returns.Application/Domain

## Support.Infrastructure references
- Notification.Contracts only
- NO Notification.Application/Domain/Infrastructure

## Event extraction (smallest)
- `Tooba.Payment.Contracts/Events/{PaymentSucceeded,PaymentFailed}IntegrationEvent`
- `Tooba.Fulfillment.Contracts/Events/{FulfillmentCreated,ShipmentDispatched}IntegrationEvent`
- `Tooba.Returns.Contracts/Events/{ReturnRequested,ReturnApproved,RefundSucceeded}IntegrationEvent`
- Producers/consumers updated atomically; event type names/payloads preserved; no duplicates.

## Wallet
Remains Notification.Contracts-only (unchanged boundary).
