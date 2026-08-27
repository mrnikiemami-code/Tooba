# 19 — Visual fidelity (TB-P06-T023)

## Lock source

Shopeiva `src/components/dashboard/notifications/notifications.jsx`  
Tooba port: `src/frontend/app/customer-panel/notification-inbox.tsx`

## Preserved

| Aspect | Status |
|---|---|
| Card / filter / header geometry | Ported |
| `#E53935` accent, unread pulse dot | Preserved |
| Filter chips (all/unread/read/order/offer/ticket) | Preserved |
| lucide-react icons | Preserved |
| Mark-all / dismiss / mark-read actions | Preserved |
| Responsive panel shell | Existing customer/vendor shells |

## Authorized minor technical deviation

| Item | Detail |
|---|---|
| Toast | Inline flash instead of `react-toastify` (no dependency) |
| Data | Real Host API instead of Shopeiva mock arrays |

## Not claimed

```text
USER_VISUAL_ACCEPTED = NOT CLAIMED
```

No foreign dashboard redesign; no invented card language.
