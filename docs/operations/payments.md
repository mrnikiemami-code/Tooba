# Tooba — Payments Operations

Production payment provider boundary: PSP-agnostic gateway, signed webhooks, idempotent verify, reconciliation.

## Architecture locks

```text
Order != Payment != Payment Provider
callback success text != verified payment success
VerifyAsync is source of truth (StatusQuery or gateway evidence)
Paid Order state = Outbox payment.succeeded.v1 consumer only
No PAN/CVV storage
No commercial PSP brand hardcoded in application code
```

## Configuration

### Development (`appsettings.json`)

```json
"Payment": {
  "Gateway": {
    "Mode": "Sandbox",
    "DefaultProvider": "fake",
    "WebhookSigningSecret": "",
    "InitiateBaseUrl": "",
    "StatusQueryBaseUrl": "",
    "StatusQueryApiKey": "",
    "AllowedStatusQueryHosts": [],
    "TimeoutSeconds": 15,
    "VerifyMaxAttempts": 3
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
    "InitiateBaseUrl": "",
    "StatusQueryBaseUrl": "",
    "StatusQueryApiKey": "",
    "AllowedStatusQueryHosts": [],
    "TimeoutSeconds": 15,
    "VerifyMaxAttempts": 3
  }
}
```

| Mode | Provider | Behavior |
|---|---|---|
| `Disabled` (default) | `FailClosedPaymentGateway` | Initiate/Verify throw `payment.gateway.unconfigured` |
| `Webhook` | `WebhookPaymentGateway` | Requires `WebhookSigningSecret` + `InitiateBaseUrl` + `StatusQueryBaseUrl`; Initiate redirects to configured PSP URL; Verify queries StatusQuery |

Inject secrets/URLs via environment before enabling Production checkout payments.

**REAL_PSP_PROVIDER_CONFIGURATION_REQUIRED** until a real provider target + credentials are supplied outside the repo.

### SSRF / outbound safety

- StatusQuery/Initiate URLs must be absolute http(s).
- Without `AllowedStatusQueryHosts`, loopback/private hosts are rejected and https is required.
- With allowlist, only listed hosts are accepted (test harness / approved endpoints).

### Reconciliation (`Tooba:PaymentReconciliation`)

```json
"PaymentReconciliation": {
  "Enabled": true,
  "PollIntervalSeconds": 60,
  "PendingAgeMinutes": 5,
  "BatchSize": 20
}
```

Background worker re-Verify stale `Pending` payments. Indeterminate gateway outcomes (`GATEWAY_TIMEOUT`, `GATEWAY_UNAVAILABLE`, `GATEWAY_RATE_LIMITED`, `GATEWAY_PENDING`) leave Payment **Pending** (do not force Failed).

## Admin operator APIs

```http
GET  /v1/admin/payments/{paymentId}
POST /v1/admin/payments/{paymentId}/reconcile
```

Also embedded on `GET /v1/admin/orders/{checkoutId}` as `payment` (no secrets).

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
| `admin.payment.missing` | 404 | Admin payment inspect miss |

## Observability

Metrics (no secrets, no card data):

- `tooba.payment.gateway.initiate` — tag `outcome`
- `tooba.payment.gateway.verify` — tag `outcome`
- `tooba.payment.webhook.received` — tag `outcome`
- `tooba.payment.reconcile.processed` — count

## Storefront integration

Checkout and Payment Result UI remain Shopeiva-locked. Browser return URL is UX only; backend Verify is truth.
