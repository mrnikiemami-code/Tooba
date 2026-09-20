# Architecture guards
- Order checkout path uses Cart.Contracts (ICartConversionPort)
- Order.Application → Cart.Application removed; App→App baseline shrunk
- Domain→foreign Domain / Infra→foreign Domain baselines unchanged
- Shared TransactionScope remains (cross-context TX baseline)
- No async checkout orchestration
- ARCH-CHECKOUT-001…005 remain active (characterization via Atomic/Process foundation tests)
- Contracts projects must not reference Domain — Cart.Contracts complies (Offer.Contracts only)
