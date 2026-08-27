# 24 — Commercial readiness (TB-P06-T023)

Honest recheck after transactional notifications.  
**Do not claim** `PRODUCTION_GO_LIVE_READY`, `USER_VISUAL_ACCEPTED`, or `REALTIME_NOTIFICATIONS_LIVE`.

## Capability flags

```text
TRANSACTIONAL_NOTIFICATIONS_LIVE = YES
NOTIFICATION_BACKEND = LIVE
CUSTOMER_NOTIFICATION_UI = LIVE
SELLER_NOTIFICATION_UI = LIVE
NOTIFICATION_UNREAD = LIVE
NOTIFICATION_DEEP_LINKS = LIVE
REALTIME_NOTIFICATIONS = DEFERRED
FAKE_NOTIFICATIONS = FORBIDDEN
VISUAL_CONTRACT = SHOPEIVA_LOCKED
SELLABLE_DEMO = YES
PAYMENT_PRODUCTION_FOUNDATION = LIVE
REAL_PSP_PROVIDER_CONFIGURATION_REQUIRED = YES
PRODUCTION_GO_LIVE_READY = NO
```

## Surface estimates (honest)

| Surface | Est. % | Notes |
|---|---|---|
| Storefront | ~high (unchanged lock) | Not re-accepted visually here |
| Customer | ↑ | Notifications nav + inbox live |
| Seller | ↑ | Notifications nav + inbox live |
| Admin | unchanged | No admin notification inbox |
| Blog / Story | Story notify **DEFERRED** | No invented story events |
| Notifications readiness | ~foundation LIVE | Realtime deferred; empty until events |
| Product / marketplace sale | unchanged demo | Sandbox pay still |
| Overall PRODUCTION_GO_LIVE | **NO** | External PSP + visual accept + other gaps |

## Production blockers (unchanged primary)

1. Real PSP provider configuration + authorized bank proof  
2. Critical storefront `USER_VISUAL_ACCEPTED`  
3. Remaining deferred commercial domains (wallet, tickets, …)  
4. Realtime notifications (optional; deferred by design)

## Remaining notification gaps

- Story review notifications (events missing / Translate null)
- Storefront header badge unbound (prefer empty over fake)
- Full browser capture pack for Architect visual review
- `react-toastify` parity (inline flash deviation)
