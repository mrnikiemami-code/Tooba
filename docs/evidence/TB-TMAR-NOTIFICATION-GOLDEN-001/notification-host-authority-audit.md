# Notification Host Authority Audit

## Removed from Host
- `Host/Notifications/NotificationEndpoints.cs` (directory deleted)
- Direct `INotificationDirectory` HTTP usage
- `NotificationRecipientQuery` construction in Host HTTP
- List projection (`MapListResponse`)
- Manual `{ title, errorCode }` customer/seller error JSON

## Remaining Host role
- `MapNotificationEndpoints()` composition call
- `AddNotificationEndpointPresentation()` catalog registration
- `HostNotificationCustomerAuthorizer` / `HostNotificationSellerAuthorizer`
- Dev bootstrap migrate-only mentions of `NotificationDbContext` (allowlisted; no business/query authority)

## Scan
No Host HTTP ownership of Notification business/query paths after task.

## Verdict
- Notification-Host-Endpoints: REMOVED
- Notification-Host-Business-Authority: NONE
- Notification-Host-DbAuthority: NONE
