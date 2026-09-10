# TB-P10-T001 — Recommendations

No recommendation engine built.

Source: `loadStorefrontHome()` → `newArrivals`, `featuredProducts`, `specialOffers` (deduped).

Render: existing `StorefrontProductCardView` (now with real ATC).

Filter: in-stock + primaryOfferId; exclude products already in cart when possible.

Evidence marker: `data-testid="cart-recommendations"`.
