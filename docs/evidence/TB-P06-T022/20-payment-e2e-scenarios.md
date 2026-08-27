# 20 — Payment E2E scenarios

**Task:** TB-P06-T022  
**Constraint:** No direct DB mutation for payment truth; no fake real-bank success.

## Scenario map

| # | Scenario | How proven | Result |
|---|---|---|---|
| 1 | Successful verified payment | Sandbox / fake gateway + foundation tests | Expected PASS (demo) |
| 2 | Forged callback rejected | HMAC + foundation tests | Expected PASS |
| 3 | Duplicate callback → no duplicate Order Paid | Inbox + Succeeded idempotency | Expected PASS |
| 4 | Timeout/unknown → Pending → reconcile → success | Indeterminate leave Pending + TestStatusOverrides / reconcile | Expected PASS (harness) |
| 5 | Amount mismatch → not Succeeded | Webhook amount check | Expected PASS |
| 6 | Production config absent → fail closed | Mode Disabled / unconfigured Webhook | Expected PASS |

## Real provider test-mode

```text
actual_provider_test_credentials = ABSENT
real_provider_e2e = NOT_RUN
REAL_BANK_PAYMENT_PROVEN = NO
```

## Execution note

Worker should attach concrete test command results in `24-final-validation.md`.  
This file maps required scenarios; it does not invent a commercial bank transaction log.
