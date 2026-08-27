# 07 — Callback authenticity

**Task:** TB-P06-T022

## Endpoint

```http
POST /v1/payments/webhooks/{providerCode}
X-Tooba-Payment-Signature: sha256=<hmac-sha256-hex-of-raw-body>
```

## Processing order

1. Validate HMAC with `WebhookSigningSecret` (`PaymentWebhookSignatureValidator`).
2. Fail closed if secret missing or signature invalid → `payment.webhook.invalid_signature`.
3. Inbox dedup on `(providerCode, providerEventId)`.
4. Correlate to internal Payment / Attempt / providerRequestReference.
5. Amount/currency tamper checks against aggregate.
6. Call `VerifyAsync` — gateway **ignores** callback success claim; StatusQuery (or test override) decides.

## Browser return

Return URL / query params may drive UX only (`/payment/result`).  
They are **not** proof of payment.

## Production readiness note

Without an injected signing secret, Production webhooks cannot authenticate.  
That is intentional fail-closed behavior under `REAL_PSP_PROVIDER_CONFIGURATION_REQUIRED`.

## Tests covering authenticity

- `PaymentProductionPolicyTests.Signature_validator_*`
- `PaymentProviderContractTests.Invalid_authenticity_rejected_by_signature_validator`
- Foundation forged-callback scenarios in payment host tests
