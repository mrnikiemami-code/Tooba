# Recovery SoT — TB-TMAR-CONTRACTS-W1

Program: TMAR
Phase: Contracts Wave 1
Removed: Cart/Order/Pricing.Domain → Offer.Domain (replaced by Offer.Contracts for SalesChannel)
Removed: Payment.Infrastructure → Wallet.Domain
Created: Tooba.Offer.Contracts (SalesChannel), Tooba.Wallet.Contracts (WalletCurrency)
Baselines: Domain→foreign Domain empty; Infra→foreign Domain empty
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL
Next: TB-TMAR-CONTRACTS-W2 (Wallet payment port out of Application; Order hub ports)
Last Verified Task: TB-TMAR-CONTRACTS-W1
Branch: main
User work preserved: YES
