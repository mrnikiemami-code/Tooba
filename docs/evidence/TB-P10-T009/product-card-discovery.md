# TB-P10-T009 — Product card discovery

One canonical `StorefrontProductCardView` is used on:

- Home rails (`storefront-home.tsx`, `storefront-home-repair-sections.tsx`)
- Listing/search (`storefront-listing.tsx`)
- Category PLP (`storefront-category-plp.tsx`)
- PDP related (`storefront-pdp.tsx`)
- Merchandising rails (`storefront-merchandising.tsx`)
- Cart recommendations (`storefront-cart.tsx`, hover actions off)
- Wishlist (`customer-panel/wishlist/page.tsx`)

No second business card. Surfaces consume Store skin via `StorefrontProductCardSkinProvider`. No page-local appearance fetch.
