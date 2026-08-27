# 02 — Payment architecture audit

**Task:** TB-P06-T022

## Classification legend

`LIVE` | `PARTIAL` | `MISSING` | `EXTERNAL_CONFIG_REQUIRED`

## Component audit

| Area | Status | Notes |
|---|---|---|
| Payment aggregate / state machine | LIVE | `PaymentStatus`: Created, Pending, Succeeded, Failed, Cancelled, Expired |
| Provider abstraction (`IPaymentGateway`) | LIVE | Initiate + Verify; registry by `ProviderCode` |
| Sandbox adapter | LIVE | `FakePaymentGateway` / `FakeFailingPaymentGateway` (non-Production) |
| Production adapter boundary | LIVE | `WebhookPaymentGateway` (PSP-agnostic; no brand) |
| Production fail-closed default | LIVE | `Mode=Disabled` → `FailClosedPaymentGateway` |
| Initiation flow | LIVE | Creates attempt + redirect/reference |
| Callback / webhook flow | LIVE | `POST /v1/payments/webhooks/{providerCode}` + HMAC |
| Verification flow | LIVE | StatusQuery is truth; callback text ignored |
| Idempotency keys / inbox | LIVE | Provider event inbox + Succeeded terminal guard |
| Retry policy (indeterminate) | LIVE | `VerifyMaxAttempts` + leave Pending |
| Reconciliation | LIVE | Background stale Pending + admin `ReconcileAsync` |
| Payment ↔ Order coupling | LIVE | Order Paid only via `payment.succeeded.v1` consumer |
| Outbox / integration events | LIVE | MassTransit + PostgreSQL outbox |
| Tenant / market / provider resolution | PARTIAL | Provider from config/registry; no commercial target |
| Operator / admin visibility | LIVE | Admin payment inspect + order detail payment ops |
| Real PSP credentials / endpoints | EXTERNAL_CONFIG_REQUIRED | `NO_PRODUCTION_PROVIDER_TARGET` |
| Real bank proven path | MISSING | Intentionally not claimed |

## Architecture locks (unchanged)

```text
Order != Payment != Payment Provider
callback success text != verified payment success
VerifyAsync is source of truth
No PAN/CVV storage
No commercial PSP brand hardcoded
```

## Verdict

Foundation is production-hardening ready. Real provider selection remains external.
