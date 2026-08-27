# 02 — Audit

## Owners (no duplication)

| Concern | Owner |
|---|---|
| Ledger balance | Wallet (`IWalletDirectory`, immutable `WalletLedgerEntry`) |
| Payment initiate/verify | Payment (`IPaymentDirectory` + `IPaymentGateway`) |
| Order Paid projection | Order via `payment.succeeded` → `OrderPaymentBridge` |
| Refund authority | Returns (`IReturnDirectory`) |
| Notifications | Notification (`CreateIfAbsentAsync`) |

## Design locks applied

- `ATOMIC_DEBIT_AT_PAID` — debit only inside wallet gateway `Verify` / `SpendForOrderPaymentAsync`
- `WALLET_MIXED_TENDER = DEFERRED` — storefront rejects remainder > 0
- No mutable wallet balance column
- Refund destination typed: `OriginalPayment | Wallet`
