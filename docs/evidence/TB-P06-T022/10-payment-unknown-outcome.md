# 10 — Payment unknown outcome

**Task:** TB-P06-T022  
**Rule:** Indeterminate codes leave Payment **Pending** (do not force Failed).

## Indeterminate codes (`PaymentGatewayOutcomes.IsIndeterminate`)

```text
GATEWAY_TIMEOUT
GATEWAY_UNAVAILABLE
GATEWAY_RATE_LIMITED
GATEWAY_PENDING
GATEWAY_UNKNOWN
```

## Implementation

In `PaymentDirectory.VerifyAsync`:

- If Verify not successful and FailureCode is indeterminate → return current status (**Pending**), **no** `ApplyVerifiedFailure`.
- Only definitive failure codes call `ApplyVerifiedFailure`.

## Bounded retry

`WebhookPaymentGateway.VerifyAsync` clamps `VerifyMaxAttempts` to 1–5.  
Retries only while FailureCode remains indeterminate; then returns last result (still Pending at directory layer if indeterminate).

## Scenario coverage (intended safe handling)

| Scenario | Expected Payment state |
|---|---|
| Initiation timeout / lost redirect response | Pending / recoverable via reconcile |
| Callback missing | Pending until StatusQuery/reconcile |
| Late callback | Idempotent verify when it arrives |
| StatusQuery timeout / 5xx / 429 | Indeterminate → Pending |
| Provider status pending/unknown/processing | `GATEWAY_PENDING` → Pending |
| App restart mid-payment | Pending preserved; worker/admin reconcile |

## Must not happen

Marking Failed solely because a callback did not arrive immediately.
