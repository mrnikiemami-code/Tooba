# 18 — Real provider adapter

**Task:** TB-P06-T022  
**Discovery:** `NO_PRODUCTION_PROVIDER_TARGET`

## Decision

Do **not** select or invent a commercial PSP brand.

```text
REAL_PSP_PROVIDER_CONFIGURATION_REQUIRED = YES
PRODUCTION_PAYMENT_FOUNDATION_READY = YES
REAL_BANK_PAYMENT_PROVEN = NO
```

## What was completed instead

`WebhookPaymentGateway` production adapter **boundary**:

| Capability | Status |
|---|---|
| Initiate via `InitiateBaseUrl` | Ready (config-gated) |
| Verify via `StatusQueryBaseUrl` | Ready (config-gated) |
| Ignore callback text as truth | Ready |
| HMAC webhook authenticity (host) | Ready |
| SSRF allowlist / private-host block | Ready |
| Bounded VerifyMaxAttempts | Ready |
| Fail-closed when misconfigured | Ready |
| Factory / DI registration | Ready (`Mode=Webhook` vs `Disabled`) |
| Contract test harness | Ready |
| Operator reconcile | Ready |

## What remains external

- Real PSP endpoint URLs
- Signing secret / API credentials
- Merchant account identifiers
- Authorized test or production bank transaction

## Mode

```text
adapter_mode = READY_FOR_PROVIDER (WebhookPaymentGateway)
default_production_mode = Disabled (FailClosedPaymentGateway)
sandbox_dev = FakePaymentGateway
```
