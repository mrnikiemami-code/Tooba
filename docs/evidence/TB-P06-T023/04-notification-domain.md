# 04 — Notification domain (TB-P06-T023)

Companion: `04-backend-structure.md` (module layout / host wiring).

## Owner

```text
src/backend/Modules/Notification/
  Tooba.Notification.Domain/        UserNotification, NotificationRecipientKind
  Tooba.Notification.Application/   INotificationDirectory, DTOs, NotificationCopy, NotificationTargetRoutes
  Tooba.Notification.Infrastructure/ DbContext, Directory, Projector, handlers, instrumentation
```

Schema: `notification` (`NotificationDbContext.Schema`)  
Table: `notification.user_notifications`

## Persistent model (`UserNotification`)

| Field | Notes |
|---|---|
| `NotificationId` | UuidV7 |
| `RecipientKind` | Customer \| Seller |
| `RecipientPartyId` | Buyer actor (customer) or seller PartyId |
| `RecipientActorUserId` | Optional; used for customer panel filter |
| `Type` | Semantic type (e.g. `payment.succeeded`) |
| `PayloadJson` | Safe structured JSON — no HTML |
| `TargetRoute` | Relative allowlisted path |
| `IsRead` / `ReadAt` | Read semantics |
| `CreatedAt` | UTC |
| `SourceEventId` | Idempotency key (integration EventId) |
| `SourceType` | Integration contract name |
| Soft delete | `IsDeleted` / `DeletedAt` |

**No `TenantId` column** — aligns with Reviews/Settlement (commerce connection isolates tenant). Story is the known exception elsewhere.

## Rules enforced

- Not a technical log / audit / analytics store
- Unique index `(RecipientKind, RecipientPartyId, SourceEventId)` prevents duplicate rows
- `Create` requires relative `TargetRoute` starting with `/`
- `MarkRead` / `SoftDelete` are idempotent

## Claims

```text
NOTIFICATION_BACKEND = LIVE
FAKE_NOTIFICATIONS = FORBIDDEN
```
