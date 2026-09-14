# TB-P10-T009 — Storefront-wide application

SSR root sets `data-storefront-product-card-skin` and wraps `StorefrontProductCardSkinProvider`.

Every `StorefrontProductCardView` reads `useProductCardSkin()` and stamps `data-product-card-skin`. Admin preview may pass an optional `skin` override; listing surfaces do not.

Wishlist uses the same component. No surface fetches appearance per card.
