# TB-TMAR-BOUNDARY-V1 Source Coupling

## Cart.Domain → Offer.Domain
- Foreign types used: **none** (unused `using` + unused ProjectReference)
- Classification: technical/shared primitive? **No** — unused project edge
- Removal path: delete ProjectReference + unused using (Contracts not required)
- Risk: very low if compile proves unused

## Order.Domain → Offer.Domain
- Foreign types used: **none**
- Classification: unused project edge
- Removal path: delete ProjectReference + unused using
- Risk: very low

## Pricing.Domain → Offer.Domain
- Foreign types used: **none** (local Money/CurrencyCode; OfferId is Guid)
- Classification: unused project edge / conceptual comment only
- Removal path: delete ProjectReference + unused using
- Risk: very low

## Payment.Infrastructure → Wallet.Domain
- Foreign types used: `WalletAccount.NormalizeCurrency` (static helper on Wallet Domain type)
- Also references `IWalletDirectory` from Wallet.Application (sync orchestration adapter)
- Classification: domain helper leakage + application orchestration dependency
- Removal path:
  1. Move currency normalize to BuildingBlocks or Payment.Application contract
  2. Keep IWalletDirectory behind Wallet.Contracts / Payment gateway ACL
  3. Drop direct Wallet.Domain ProjectReference
- Risk: medium (payment verify path)

## Note on App→App
Order/Cart/Inventory/Pricing/Promotion Application edges are live orchestration dependencies (not dead). Covered separately in order-hub-analysis.md.
