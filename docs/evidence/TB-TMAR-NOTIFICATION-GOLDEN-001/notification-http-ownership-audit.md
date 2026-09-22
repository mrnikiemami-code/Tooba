# Notification HTTP Ownership Audit

## Before
- `Host/Tooba.Host/Notifications/NotificationEndpoints.cs` owned all customer/seller `/v1/*/notifications*` routes
- Host called `INotificationDirectory` directly, built `NotificationRecipientQuery`, projected list JSON, resolved customer actor, and mapped seller `PlatformHttpException` manually

## After
- `Tooba.Notification.Endpoints` owns routes via `NotificationEndpointModule.MapNotificationEndpoints()`
- Customer routes: `NotificationCustomerEndpoints` → `ISender` → Application CQRS
- Seller routes: `NotificationSellerEndpoints` → `ISender` → Application CQRS
- Host `Program.cs` only calls `MapNotificationEndpoints()`, `AddNotificationEndpointPresentation()`, and registers authorizer adapters
- `Host/Notifications/` deleted

## Verdict
Notification-HTTP-Ownership: MODULE_ENDPOINTS
