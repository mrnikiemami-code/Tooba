# 04 — Backend notification structure (TB-P06-T023)

## Module

```
src/backend/Modules/Notification/
  Tooba.Notification.Domain/          UserNotification, RecipientKind
  Tooba.Notification.Application/     INotificationDirectory, DTOs, copy, target allowlist
  Tooba.Notification.Infrastructure/  DbContext schema=notification, Directory, Projector, handlers, OutboxRegistration
```

## Host wiring

- `ToobaModuleComposition` → `NotificationModule`
- `Program.cs` → `MapNotificationEndpoints()`
- `ModuleMigrationRegistry` + Marketplace/ProductWorkspace bootstrap migrate `NotificationDbContext`
- MassTransit: existing Host SQL transport consumer dispatches `IIntegrationEventHandler<>` — no RabbitMQ

## APIs

| Method | Path |
|---|---|
| GET | `/v1/customer/notifications` |
| GET | `/v1/customer/notifications/unread-count` |
| POST | `/v1/customer/notifications/{id}/read` |
| POST | `/v1/customer/notifications/read-all` |
| DELETE | `/v1/customer/notifications/{id}` |
| same | `/v1/seller/notifications*` |

DTO: `notificationId`, `type`, `category`, `title`, `body`, `targetRoute`, `isRead`, `createdAt`

## Consumers

- `payment.succeeded.v1` → customer + sellers
- `payment.failed.v1` → customer
- `fulfillment.created.v1` / `shipment.dispatched.v1` → customer + seller
- `return.requested.v1` / `return.approved.v1` / `refund.succeeded.v1` → customer + seller
- Story: skipped (Translate null)

## Idempotency

Unique index `(recipient_kind, recipient_party_id, source_event_id)`
