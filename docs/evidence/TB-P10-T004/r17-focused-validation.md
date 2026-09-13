# R17 focused validation

## Host

`dotnet test Tooba.Host.Tests --filter AdminReservationCycleAuditTests|OrderSupplyUxTests|ReservationCycleFoundationTests|InvoiceHeaderSemanticsTests|AdminDbNativeGridQueryTests|StorefrontPendingPaymentTests`

Passed: 39 / 39

- AdminReservationCycleAuditTests: Active #1, Expired, Cycle #2 + stored policy, CommittedPaid, ReleasedByCancel, ReacquireFailed + 2 shortage lines, retry limit, compact labels, batch/no N+1, locks
- OrderSupplyUxTests: R8 supply batch + new cycle batch
- ReservationCycleFoundationTests: R15
- StorefrontPendingPaymentTests: R16 (10)
- InvoiceHeaderSemanticsTests / AdminDbNativeGridQueryTests: list map + no in-memory Execute

## Frontend

`node --test admin-reservation-cycle.test.ts admin-order-supply.test.ts admin-api.test.ts`

Passed: 16 / 16

`node --test storefront-pending-payment-api.test.ts` — 4 / 4

## Recovery

`node --test docs/ai/recovery-staleness.guard.test.mjs` — 4 / 4 (`CURRENT_TASK_ID=TB-P10-T004-R17`)

## git diff --check

clean (CRLF warnings only)

No unrelated full suites. No TB-P10-T005. Home/PDP not touched.
