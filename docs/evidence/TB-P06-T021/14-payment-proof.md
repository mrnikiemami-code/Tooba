# 14 — Payment proof (TB-P06-T021)

## Demo policy

Sandbox PSP is **acceptable** for `SELLABLE_DEMO`. Do **not** claim production bank / real-PSP readiness.

## Routes

| Surface | URL / API |
|---|---|
| Sandbox UI | `http://127.0.0.1:3000/fa/payment/sandbox` |
| Result UI | `http://127.0.0.1:3000/fa/payment/result` |
| Host | `POST /v1/storefront/checkout/{checkoutId}/payments` |
| Dev complete | `POST /v1/storefront/payments/{paymentId}/sandbox/complete` (or Host equivalent) |
| Webhook | `POST /v1/payments/webhooks/{providerCode}` |

Module: `src/backend/Modules/Payment/` · FE: `app/payment/sandbox`, `app/payment/result`

## Lifecycle required for sellable demo

```text
PendingPayment
→ provider / sandbox flow
→ callback / result page
→ Succeeded
→ Order Paid
→ Fulfillment handoff (auto-create on payment success)
```

| Step | Status |
|---|---|
| Create payment intent on checkout | LIVE |
| Sandbox complete path | LIVE |
| Persist Succeeded + Order Paid | LIVE |
| Fulfillment opened on payment success | LIVE (`FulfillmentPaymentSucceededHandler`) |
| Production real-bank PSP | **NOT** claimed |

## Readiness label

```text
PAYMENT_PATH = LIVE (sandbox)
SELLABLE_DEMO = YES
PRODUCTION_GO_LIVE_READY = NO  (sandbox ≠ production bank)
```
