# 03 — Provider configuration discovery

**Task:** TB-P06-T022  
**Result:** `NO_PRODUCTION_PROVIDER_TARGET`

## Search scope (safe)

- `appsettings.json` / `appsettings.Production.json` → `Payment:Gateway`
- `docs/operations/payments.md`
- Payment Infrastructure registration (`PaymentModule`)
- No dumping of environment secrets into evidence

## What was found

| Item | Found? | Value / notes |
|---|---|---|
| Commercial PSP brand name | No | Explicitly not selected in code |
| Production Mode | Yes | `Disabled` (fail-closed default) |
| DefaultProvider (Production file) | Yes | `webhook` (generic code, not a bank brand) |
| InitiateBaseUrl | Empty | Requires external injection |
| StatusQueryBaseUrl | Empty | Requires external injection |
| WebhookSigningSecret | Empty | Requires secret source / env |
| StatusQueryApiKey | Empty | Optional bearer; empty in repo |
| AllowedStatusQueryHosts | Empty array | Private hosts blocked by default |
| Merchant / account IDs | No | Not present in repo config |
| Feature flag selecting a bank | No | — |

## Production snippet shape (secrets redacted)

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

## Decision

```text
PROVIDER_TARGET_FOUND = NO
NO_PRODUCTION_PROVIDER_TARGET = YES
REAL_PSP_PROVIDER_CONFIGURATION_REQUIRED = YES
```

Do **not** invent a commercial PSP. Adapter boundary + contract harness completed instead.
