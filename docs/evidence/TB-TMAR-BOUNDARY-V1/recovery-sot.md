# Recovery SoT — TB-TMAR-BOUNDARY-V1

Program: TMAR
Phase: Boundary verification
Confirmed structural debt:
- Cart/Order/Pricing.Domain → Offer.Domain (unused symbols; ProjectReference CONFIRMED)
- Payment.Infrastructure → Wallet.Domain (WalletAccount.NormalizeCurrency)
- Order.Application sync hub (6 foreign Application refs; already App→App baselined)
Guards added: Domain→foreign Domain + Infra→foreign Domain exact baselines
Product-Resume-Safety: NOT_YET
Next: TB-TMAR-CONTRACTS-W1
Last Verified Task: TB-TMAR-BOUNDARY-V1
Branch: main
User work preserved: YES
