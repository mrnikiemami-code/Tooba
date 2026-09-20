# Currency ownership — TB-TMAR-CONTRACTS-W1

## Who owns the invariant?

**Wallet** owns ledger/account currency string normalization used when composing wallet payment references and mutating wallet ledger rows.

## Wallet-specific vs shared financial primitive?

**Wallet-specific.** Pricing already has a stricter `CurrencyCode` (different rejection rules, e.g. TOMAN). BuildingBlocks would wrongly unify incompatible rules.

## Why Contracts location?

- Behavior is part of Wallet’s public payment-facing boundary (Payment gateway must match Wallet spend normalization).
- Must not expose `WalletAccount` entity across modules.
- Domain keeps a thin facade; Contracts holds the stable shared primitive for cross-module Infrastructure consumers.
- Future Wallet microservice can ship Contracts without Domain types.
