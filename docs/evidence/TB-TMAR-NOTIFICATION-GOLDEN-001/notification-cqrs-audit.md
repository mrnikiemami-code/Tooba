# Notification CQRS Audit

## Use cases (MediatR 12.5.0 via BuildingBlocks)

### Queries
- `ListCustomerNotifications`
- `GetCustomerUnreadNotificationCount`
- `ListSellerNotifications`
- `GetSellerUnreadNotificationCount`

### Commands
- `MarkCustomerNotificationRead`
- `MarkAllCustomerNotificationsRead`
- `DismissCustomerNotification`
- `MarkSellerNotificationRead`
- `MarkAllSellerNotificationsRead`
- `DismissSellerNotification`

## Layout
Each use case lives in `Application/Commands/<UseCase>/` or `Application/Queries/<UseCase>/` with request + handler colocated.

## Registration
`Program.cs` `AddToobaCqrsFoundation(... typeof(MarkCustomerNotificationReadCommand).Assembly)`.

## Endpoints
Every Notification HTTP route injects `ISender` and sends a typed request. No giant handler files. No direct `INotificationDirectory` in Endpoints.

## Verdict
Notification-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
