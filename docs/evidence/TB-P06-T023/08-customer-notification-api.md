# 08 — Customer notification API (TB-P06-T023)

Host: `src/backend/Host/Tooba.Host/Notifications/NotificationEndpoints.cs`  
Mapped from `Program.cs` → `MapNotificationEndpoints()`.

## Endpoints

| Method | Path |
|---|---|
| GET | `/v1/customer/notifications` |
| GET | `/v1/customer/notifications/unread-count` |
| POST | `/v1/customer/notifications/{id}/read` |
| POST | `/v1/customer/notifications/read-all` |
| DELETE | `/v1/customer/notifications/{id}` |

## Behavior

- Own notifications only (`RecipientKind.Customer` + actor UserId)
- Pagination: `skip` / `take` (take clamped 1–100); newest first
- Locale: `?locale=` default `fa`; copy resolved at read time
- Mark-one / mark-all / soft-delete scoped to recipient → foreign id → 404
- Unread count is real DB aggregate (not mocked)

## List DTO fields

`notificationId`, `type`, `category`, `title`, `body`, `targetRoute`, `isRead`, `createdAt` (+ page `totalCount`, `unreadCount`)

## Auth

- Authenticated session required in non-dev
- Development/Testing: `X-Tooba-Dev-Actor-User-Id` or storefront guest actor fallback
