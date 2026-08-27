# 14 — Edition isolation SingleStore (TB-P06-T012)

## Marketplace-only settlement

| Component | SingleStore | Marketplace |
|---|---|---|
| Settlement event handlers | Not registered | Registered |
| Settlement schema migration (dev bootstrap) | Skipped | Applied |
| Seller/admin settlement HTTP | Edition-gated at data layer (empty/no account) | Live ledger |

## Mechanism

`SettlementModule.IsMarketplaceEdition` reads `Tooba:Edition` configuration.

Handler registration guard verified by `Settlement_handlers_are_marketplace_gated_in_module_registration`.

## Test baseline

`dotnet test` with `appsettings.Development` SingleStore:

- Settlement tests use isolated Testcontainers DB with explicit `ToobaEdition.Marketplace` commerce context
- No regression to SingleStore integration tests from settlement handler registration

## Rationale

Settlement accrual requires multi-seller payment allocations — SingleStore edition has no marketplace seller ledger use case. Fail-safe: handlers absent, no orphan consumers.

## Frontend

Vendor wallet and admin settlement screens render live API data; on SingleStore host they show empty/error states from real API responses, not fabricated balances.
