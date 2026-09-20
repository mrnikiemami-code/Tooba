# Wallet.Contracts — TB-TMAR-CONTRACTS-W1

## Created

`src/backend/Modules/Wallet/Tooba.Wallet.Contracts/`

Contents:

- `WalletCurrency.Normalize(string)` — ledger currency normalization

## Live coupling removed

Payment.Infrastructure no longer references Wallet.Domain.

`WalletPaymentGateway` uses `WalletCurrency.Normalize` from Contracts.

`WalletAccount.NormalizeCurrency` remains as a Domain facade delegating to Contracts (preserves existing Wallet call sites).

## Still present (deferred)

Payment.Infrastructure → Wallet.Application (`IWalletDirectory`) — already Infra→foreign Application baselined; extraction of payment debit port is CONTRACTS-W2.
