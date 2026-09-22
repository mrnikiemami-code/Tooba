# Notification Response Contract Audit

## Preserved wire JSON (camelCase)
List:
- `items[].notificationId/type/category/title/body/targetRoute/isRead/createdAt`
- `skip/take/totalCount/unreadCount`

Unread: `{ unreadCount }`
Mark-all: `{ markedCount }`
Mark-one / dismiss success: HTTP 204
Missing mark-one / dismiss: HTTP 404 via `notification.missing`

## Ownership
Projection moved to Application `NotificationHttpMapper` / typed response models.
Endpoints only parse transport, resolve actor, send MediatR request, map `Result` through `ApiResponseFactory`.

## Verdict
Response contract preserved; projection owned by Application
