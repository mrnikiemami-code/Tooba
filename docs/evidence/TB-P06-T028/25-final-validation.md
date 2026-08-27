# 25 — Final validation (TB-P06-T028)

## Backend

| Check | Result |
| --- | --- |
| `dotnet build` Host (artifacts/tb-p06-t028-rebuild) | **0 warnings / 0 errors** |
| `dotnet test` Host.Tests | **Passed 298 / Failed 0 / Skipped 0** |
| `dotnet test` MigrationRunner.Tests | **Passed 4 / Failed 0 / Skipped 0** |
| Migration apply `store-alpha` | Returns `AddRefundDestination` + Settlement `InitialSettlement` applied |
| Live API E2E | `docs/evidence/TB-P06-T028/15-wallet-checkout-e2e.json` → **ALL_OK** |

Backend total for Result contract: **Passed 302 / Failed 0 / Skipped 0**

## Frontend

| Check | Result |
| --- | --- |
| `npm run typecheck` | PASS |
| `npm run lint` | PASS (0 warnings/errors) |
| `npm run test` | PASS (all suites fail 0) |
| `npm run build` | PASS |

## Contract fixes (integration)

- Host accepts `providerCode=wallet` **or** `useWallet=true` (`WantsWallet`)
- Returns accept `destination` **or** `refundDestination`
- FE sends both fields for payment/returns

## Locks honored

- `ATOMIC_DEBIT_AT_PAID`
- `WALLET_MIXED_TENDER = DEFERRED`
- Immutable wallet ledger reused
- Order Paid only via payment.succeeded path
- Refund authority remains Returns module
