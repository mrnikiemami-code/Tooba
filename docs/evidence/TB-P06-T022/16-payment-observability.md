# 16 — Payment observability

**Task:** TB-P06-T022

## Structured signals (safe)

Recordable / metric tags may include:

- PaymentId / Order (Checkout) Id
- Provider code
- Operation (initiate / verify / webhook / reconcile)
- Result classification / outcome
- Attempt number (VerifyMaxAttempts loop)
- Correlation / trace via OTel spans where instrumented
- Reconciliation processed count

## Metrics (`PaymentGatewayInstrumentation`)

| Metric | Tag |
|---|---|
| `tooba.payment.gateway.initiate` | `outcome` |
| `tooba.payment.gateway.verify` | `outcome` |
| `tooba.payment.webhook.received` | `outcome` |
| `tooba.payment.reconcile.processed` | count |

Outcomes observed in adapter: succeeded, failed, pending, timeout, unavailable, rate_limited, misconfigured, ssrf_blocked, invalid_response, etc.

## Never log

```text
card / PAN / CVV
WebhookSigningSecret / API keys
reusable sensitive callback tokens
raw provider credentials
```

## OTel spans (intended operations)

initiate · verify · callback · reconcile
