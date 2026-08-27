# 13 — Payment refund compatibility

**Task:** TB-P06-T022

## Scope

Do **not** redesign Returns/Refunds. Verify routing compatibility only.

## Contract

`IPaymentRefundGateway.RefundAsync(...)` → `GatewayRefundResult`.

## Registration (`PaymentModule`)

| Environment | Refund gateway |
|---|---|
| Non-Production | `FakePaymentRefundGateway` (test/demo only) |
| Production | `FailClosedPaymentRefundGateway` |

## Rules

1. Real provider refund execution remains fail-closed without configured PSP.
2. Do not fake provider refund success in Production.
3. Existing refund domain/foundation can call the interface when a future provider refund adapter is configured.
4. `NO_PRODUCTION_PROVIDER_TARGET` ⇒ `REFUND_PROVIDER_READY = NO` (contract present; execution not live).

## Honest matrix

```text
REFUND_CONTRACT = PRESENT
REFUND_PROVIDER_READY = NO
FAKE_PROVIDER_REFUND_SUCCESS_IN_PRODUCTION = NO
```
