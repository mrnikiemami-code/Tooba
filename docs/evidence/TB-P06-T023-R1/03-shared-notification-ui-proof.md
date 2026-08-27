# 03 — Shared notification UI proof

| Surface | Page | Component |
|---|---|---|
| Customer | `app/customer-panel/notifications/page.tsx` | `NotificationInbox kind="customer"` from `notification-inbox.tsx` |
| Seller | `app/vendor-panel/notifications/page.tsx` | same `NotificationInbox kind="seller"` |

No forked CSS/JS copies. One shared module + shared ToastContainer in AppProviders.
