# 11 — Seller notification UI (TB-P06-T023)

## Routes / sources

| Piece | Path |
|---|---|
| Page | `src/frontend/app/vendor-panel/notifications/page.tsx` |
| Shared inbox | reuses `customer-panel/notification-inbox.tsx` with `kind="seller"` |
| API | `notification-api.ts` → `/v1/seller/notifications*` |

## Shopeiva note

Vendor has **no** dedicated inbox in Shopeiva (settings toggles only). Task allows reuse of customer `notifications.jsx` geometry under `/vendor-panel/notifications`.

## Behavior

- Real seller rows only; empty until paid-order / fulfillment / return events for that seller
- Mark read / mark all / dismiss; safe deep links via `targetRoute`
- Same `#E53935` / filter chip / card geometry as customer inbox
- Inline flash toast (same minor deviation as customer)

## Claims

```text
SELLER_NOTIFICATION_UI = LIVE
```
