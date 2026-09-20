# TB-TMAR-BOUNDARY-V1 Contracts Extraction Sequence

Do **not** create Contracts projects in this task. First extraction sequence:

## 1) Offer.Contracts (highest leverage)
- Owner: Offer
- First types: commercial read models / offer identity + status DTOs needed by Cart/Order/Pricing/Inventory/Promotion
- Consumers: Cart, Order, Pricing, Inventory, Promotion Applications (and eventually Domains after Domain refs deleted)
- Adapter: Offer.Infrastructure implements; consumers depend only on Contracts
- Risk: medium
- Reduction: unblocks Domain ref deletion story + shrinks App→App Offer edges

## 2) Wallet.Contracts
- Owner: Wallet
- First types: `IWalletDirectory` (or payment-facing debit port) + currency normalize helper moved out of Domain entity
- Consumers: Payment.Infrastructure gateway
- Adapter: Wallet.Infrastructure
- Risk: medium (payment verify)
- Reduction: removes Payment.Infrastructure→Wallet.Domain (and ideally Wallet.Application) edges

## 3) Inventory.Contracts + Pricing.Contracts + Tax.Contracts
- Owner: respective modules
- First types: reservation/quote/tax ports used by Order checkout
- Consumers: Order.Application
- Risk: high (checkout consistency)
- Reduction: shrinks Order hub App→App edges

## 4) Cart.Contracts
- Owner: Cart
- First types: cart read/convert ports for Order
- Consumers: Order.Application
- Risk: medium
- Reduction: Order→Cart Application edge

## 5) Promotion.Contracts
- Owner: Promotion
- After Pricing/Offer ports stable
- Consumers: Order.Application / storefront composers later

## Parallel safe cleanups (pre-Contracts, optional BOUNDARY-R1)
Delete unused Domain→Offer ProjectReferences in Cart/Order/Pricing if compile proves zero symbol usage — shrink Domain baseline immediately without Contracts projects.
