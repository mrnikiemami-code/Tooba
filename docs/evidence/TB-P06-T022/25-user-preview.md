# 25 — User preview

**Task:** TB-P06-T022  
**Purpose:** Exact URLs for Architect / operator preview. No Visual ACCEPT implied.

## Customer / storefront

| Surface | URL |
|---|---|
| Checkout | `/fa/checkout` |
| Sandbox payment (Development) | `/fa/payment/sandbox` |
| Payment result | `/fa/payment/result` |
| Cart (pre-checkout) | `/fa/cart` |

## Admin

| Surface | URL / API |
|---|---|
| Orders list | `/admin/orders` (existing panel) |
| Order detail payment ops | `/admin/orders/{checkoutId}` — payment Info block |
| Payment inspect API | `GET /v1/admin/payments/{paymentId}` |
| Reconcile API | `POST /v1/admin/payments/{paymentId}/reconcile` |

## What to look for

1. Sandbox success still works in Development.
2. Pending stays Pending when truth unknown.
3. Admin payment fields appear without layout redesign.
4. Production Mode Disabled still fail-closes checkout payments without secrets.

## Claims

```text
SELLABLE_DEMO preview = YES (sandbox)
REAL_BANK preview = NO
USER_VISUAL_ACCEPTED = NO
```
