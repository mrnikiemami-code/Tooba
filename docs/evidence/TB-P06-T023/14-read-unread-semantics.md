# 14 — Read / unread semantics (TB-P06-T023)

## Rules (code)

| Behavior | Implementation |
|---|---|
| New row unread | `IsRead = false` on `Create` |
| Opening list | Does **not** auto mark-all (Shopeiva does not either) |
| Mark one | `MarkRead` — no-op if already read; returns false |
| Mark all | `MarkAllReadAsync` — only unread rows |
| Soft delete | Marks read if unread, then deletes |
| Unread count | Count `!IsRead && !IsDeleted` for recipient |
| Duplicate event | Unique `(kind, party, SourceEventId)` → no second unread row |

## UI

- Client calls mark-read on explicit action / deep-link flow, not on mere list fetch
- Filters compute unread from loaded items; server `unreadCount` also returned on list + dedicated endpoint

## Proof

`NotificationFoundationTests.Idempotent_create_mark_read_and_cross_seller_isolation` covers duplicate suppress + mark-read idempotency + isolation.
