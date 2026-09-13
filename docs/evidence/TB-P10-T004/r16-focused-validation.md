# R16 focused validation

## Host

`dotnet test Tooba.Host.Tests --filter StorefrontPendingPaymentTests|ReservationCycleFoundationTests|StorefrontCheckoutAccessTests|StorefrontPaymentSucceededGuardTests`

Passed: 24 / 24

- StorefrontPendingPaymentTests: active pending; Succeeded excluded; Manual AwaitingAdmin informational; multi-order scoped; failed payment same countdown; expired retry same Order; max cycles no CTA; cancelled/refunded hidden; batch/ownership source; locks/error copy
- ReservationCycleFoundationTests: R15 semantics + batched `GetProjectionsAsync` (540/240s)
- StorefrontCheckoutAccessTests: R13
- StorefrontPaymentSucceededGuardTests: R14

## Frontend

`node --test storefront-pending-payment-api.test.ts storefront-cart-ui.guard.test.ts storefront-cart-api.test.ts storefront-payment-api.test.ts`

Passed: 30 / 30

## Recovery

`node --test docs/ai/recovery-staleness.guard.test.mjs` — 4 / 4

## git diff --check

clean (CRLF warnings only)

No unrelated full suites. No TB-P10-T005. Home/PDP shared cards not changed; critical-storefront not required.
