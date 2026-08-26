# Tooba — Payments Operations

Production payment provider boundary: PSP-agnostic gateway, signed webhooks, idempotent verify, reconciliation.

## Architecture locks

```text
Order != Payment != Payment Provider
callback success text != verified payment success
VerifyAsync is source of truth (StatusQuery or gateway evidence)
Paid Order state = Outbox payment.succeeded.v1 consumer only
No PAN/CVV storage
```

## Configuration

### Development (`appsettings.json`)

```json
"Payment": {
  "Gateway": {
    "Mode": "Sandbox",
    "DefaultProvider": "fake",
    "WebhookSigningSecret": "",
    "StatusQueryBaseUrl": "",
    "StatusQueryApiKey": "",
    "TimeoutSeconds": 15
  }
}
```

- Registers `FakePaymentGateway` + `FakeFailingPaymentGateway`
- Storefront sandbox complete route: `POST /v1/storefront/payments/{id}/sandbox/complete` (Development/Testing only)

### Production (`appsettings.Production.json`)

```json
"Payment": {
  "Gateway": {
    "Mode": "Disabled",
    "DefaultProvider": "webhook",
    "WebhookSigningSecret": "",
    "StatusQueryBaseUrl": "",
    "StatusQueryApiKey": "",
    "TimeoutSeconds": 15
  }
}
```

| Mode | Provider | Behavior |
|---|---|---|
| `Disabled` (default) | `FailClosedPaymentGateway` | Initiate/Verify throw `payment.gateway.unconfigured` |
| `Webhook` | `WebhookPaymentGateway` | Requires `WebhookSigningSecret` + `StatusQueryBaseUrl`; Verify queries PSP status API |

Env-inject secrets before enabling Production checkout payments.

### Reconciliation (`Tooba:PaymentReconciliation`)

```json
"PaymentReconciliation": {
  "Enabled": true,
  "PollIntervalSeconds": 60,
  "PendingAgeMinutes": 5,
  "BatchSize": 20
}
```

Background worker re-Verify stale `Pending` payments (lost/delayed callbacks).

## Webhook endpoint

```http
POST /v1/payments/webhooks/{providerCode}
X-Tooba-Payment-Signature: sha256=<hmac-sha256-hex-of-raw-body>
Content-Type: application/json
```

Body (`PaymentWebhookNotification`):

```json
{
  "providerEventId": "evt-unique-from-psp",
  "paymentId": "guid",
  "attemptId": "guid",
  "providerRequestReference": "wh-...",
  "amount": 109000.0000,
  "currency": "IRR",
  "status": "succeeded"
}
```

Processing:

1. HMAC signature validation (fail-closed if secret missing)
2. Inbox dedup on `(providerCode, providerEventId)`
3. Amount/currency tamper check against Payment aggregate
4. `IPaymentDirectory.VerifyAsync` — gateway ignores callback text; StatusQuery decides

## Error codes

| Code | HTTP | Meaning |
|---|---|---|
| `payment.gateway.unconfigured` | 400 | Production gateway not configured |
| `payment.webhook.invalid_signature` | 401 | HMAC mismatch |
| `payment.webhook.invalid_payload` | 400 | Malformed webhook body |
| `payment.webhook.amount_mismatch` | 409 | Tampered amount/currency |
| `payment.webhook.attempt_mismatch` | 409 | Wrong attempt/reference |
| `payment.webhook.provider_mismatch` | 409 | Provider code mismatch |
| `payment.missing` | 404 | Payment not found |

## Observability

Metrics (no secrets, no card data):

- `tooba.payment.gateway.initiate` — tag `outcome`
- `tooba.payment.gateway.verify` — tag `outcome`
- `tooba.payment.webhook.received` — tag `outcome`
- `tooba.payment.reconcile.processed` — count

## Storefront integration

- Initiate: `POST /v1/storefront/checkout/{checkoutId}/payments` — provider from `Payment:Gateway:DefaultProvider`
- Poll: `GET /v1/storefront/payments/{paymentId}` + order `paymentState === Paid` on result page
- No checkout UI changes in this task

## Migration

Apply `20260827000000_PaymentWebhookInbox` — table `payment.webhook_inbox` for provider event dedup.
