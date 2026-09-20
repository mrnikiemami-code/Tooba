# Returns → Wallet Contracts — TB-TMAR-CONTRACTS-W3

## Before
Returns.Infrastructure → Wallet.Application (`IWalletDirectory.CreditRefundAsync`)

## After
Returns.Infrastructure → Wallet.Contracts (`IWalletRefundCreditPort`)

## Moved to Contracts
- `IWalletRefundCreditPort` (CreditRefund only)
- `WalletRefundCreditResultDto` (Balance + IdempotentReplay; no Ledger Entry leak)

## Implementation
- `WalletDirectory` implements `IWalletRefundCreditPort` (explicit Credit mapping)
- DI: `IWalletRefundCreditPort` resolves via `IWalletDirectory` cast
- `ReturnDirectory` constructor takes `IWalletRefundCreditPort`

## Baseline shrink
Removed: `Tooba.Returns.Infrastructure -> Tooba.Wallet.Application`
