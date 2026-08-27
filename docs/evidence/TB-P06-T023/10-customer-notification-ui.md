# 10 — Customer notification UI (TB-P06-T023)

## Routes / sources

| Piece | Path |
|---|---|
| Page | `src/frontend/app/customer-panel/notifications/page.tsx` |
| Inbox | `src/frontend/app/customer-panel/notification-inbox.tsx` |
| API client | `src/frontend/app/customer-panel/notification-api.ts` |
| Shopeiva lock | `notifications.jsx` (see `02-shopeiva-notification-source-map.md`) |

## Binding

- Loads `/v1/customer/notifications` — **no mock rows**
- Unread / read visual states; mark one; mark all; delete (soft via API)
- Filters: all / unread / read / order / offer / ticket (offer/ticket empty honestly until those event types exist)
- Brand accent `#E53935` preserved from Shopeiva inbox geometry
- Empty hint: honest — “پس از رویدادهای واقعی پرداخت، ارسال یا مرجوعی…”

## Toast deviation (minor)

Shopeiva uses `react-toastify`. Tooba uses **inline flash** (`data-testid="notifications-flash"`) — no `react-toastify` dependency. Geometry/actions otherwise ported.

## Claims

```text
CUSTOMER_NOTIFICATION_UI = LIVE
NOTIFICATION_UNREAD = LIVE (API + UI)
```

No fake push / no fake badge inventing unread.
