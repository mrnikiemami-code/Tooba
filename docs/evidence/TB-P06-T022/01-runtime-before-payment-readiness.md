# 01 — Runtime before payment readiness

**Task:** TB-P06-T022  
**Predecessor HEAD:** `f9c20bb8377f053050b64142d59e20cd53aed833`  
**Branch expectation:** `main`, `HEAD == origin/main`

## Triad (pre-change baseline)

| Surface | Expectation | Notes |
|---|---|---|
| PostgreSQL | Running | Shared Tooba host DB |
| Backend (`Tooba.Host`) | `/health/live` 200, `/health/ready` 200 | Payment module loaded |
| Tooba Frontend | Checkout + payment routes reachable | Shopeiva-locked UI |
| Original Shopeiva | Available where locally configured | Visual reference only |

## Routes to confirm before edits

```text
GET  /health/live
GET  /health/ready
GET  /fa/checkout
GET  /fa/payment/result
GET  /fa/payment/sandbox          (Development sandbox only)
Admin operational order/payment inspect
```

## Pre-change payment behavior (proven by T008/T021)

- Development: `FakePaymentGateway` + sandbox complete path works.
- Production config default: `Payment:Gateway:Mode = Disabled` → fail-closed.
- Pending → Succeeded → Order Paid path exists for sandbox.
- No real bank / commercial PSP transaction claimed.

## Honest status

Runtime triad health is a **worker execution prerequisite** for this Task.  
This file records the expected baseline; fill concrete HTTP timestamps/status codes when the worker captures them during BRIDGE-WAKE execution.

```text
SANDBOX_PAYMENT_BEFORE_CHANGES = EXPECTED_LIVE (Development)
REAL_BANK_BEFORE_CHANGES = NOT_PROVEN
PRODUCTION_GATEWAY_BEFORE_CHANGES = DISABLED_FAIL_CLOSED
```
