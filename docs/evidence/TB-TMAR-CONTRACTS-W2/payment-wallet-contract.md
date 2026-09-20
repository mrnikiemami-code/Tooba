# Payment → Wallet Contracts — TB-TMAR-CONTRACTS-W2

## Before
Payment.Infrastructure → Wallet.Application (`IWalletDirectory`)

## After
Payment.Infrastructure → Wallet.Contracts (`IWalletOrderPaymentPort`)

## Moved to Contracts
- `IWalletOrderPaymentPort` (Spend + Quote only)
- `WalletCheckoutQuoteDto`
- `WalletOrderPaymentDebitResultDto` (no Ledger Entry leak)

## Implementation
- `WalletDirectory` implements `IWalletOrderPaymentPort` (explicit Spend mapping)
- DI: `IWalletOrderPaymentPort` resolves via `IWalletDirectory` cast
- `WalletPaymentGateway` constructor uses `IWalletOrderPaymentPort`

## Baseline shrink
Removed: `Tooba.Payment.Infrastructure -> Tooba.Wallet.Application`
