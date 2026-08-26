# 11 — Home / PDP Regression Proof

Commands:

```bash
cd src/frontend
npm run test:critical-storefront
```

Result after TB-P05-T020: **pass** (home + pdp + listing guards).

Code touch set:

- `storefront-listing.tsx` (PLP only)
- `products/page.tsx` (PLP data wiring)
- `storefront-merchandising.tsx` (brand grid columns only)
- listing guard test + package scripts

Home (`storefront-home.tsx`) and PDP (`storefront-pdp*.tsx`) were **not** redesigned in this Task.
