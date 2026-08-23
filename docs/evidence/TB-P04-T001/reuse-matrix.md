# TB-P04-T001 — Reuse classification evidence

## REUSE

- Skeleton family under `src/components/skeleton`
- RTL root layout pattern (`lang=fa`, `dir=rtl`)
- Local Persian font loading idea (implementation needs weight fix)

## ADAPT

- Header/Footer/shell width
- Listing/category/brand/PDP visual sections
- Cart item/summary layout
- Seller public profile chrome
- Blog/static pages
- next-themes dark utilities
- RHF/zod form samples
- Drawer/modal *ideas*

## REBUILD

- Search (Fuse + JSON)
- Checkout/payment
- Cart/auth stores as source of truth
- Vendor/admin operations
- Product workspace
- Data Grid
- Offer/price/tax/inventory UI
- OTP/auth against Tooba Identity

## DROP

- `/index2`, `/index3`
- `public/jsons` as product truth
- `product.price` / `product.stock` on catalog-like records
- Hardcoded SEO hosts / placeholder google verification
- `images.remotePatterns: hostname **`
- Auth polling every 20s on protected paths
- Fake 400ms loading gates

## DEFER

- Wallet, gift cards, referral, premium, site-survey
- Chart.js analytics
- Compare table as a product
- zustand, framer-motion, persian date widgets, OTP input package
- Tailwind 4 migration for Tooba
