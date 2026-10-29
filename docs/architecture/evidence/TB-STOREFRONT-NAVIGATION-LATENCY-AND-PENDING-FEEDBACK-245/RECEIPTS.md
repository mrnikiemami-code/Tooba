# RECEIPTS — TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245

## Claim

- Bridge row: `af39a0ed-9115-4cf0-93e8-0aabfe469a44`
- Channel: `tooba-main`
- Parent 244-R3 source in main: `f870c716a` (ancestor of claim HEAD `2620f8091`)

## Host API timings (5088)

- `/v1/storefront/home` ≈168ms ~68KB
- `/v1/storefront/products/{slug}` ≈75ms
- `/v1/storefront/categories` ≈29ms

## Production FE proof (`next start` :3000)

Warm product click (home → Nike Men's pants):
- pendingMs=10, usableMs=694, pending cleared
- resource ledger after click: home=0, rsc=1, products questions/cart/header/brands once each

Warm cart (mini-cart CTA):
- pendingMs=15, usableMs=347, URL `/fa/cart`, title سبد خرید | توبا
- body shows ۱۸ کالا — cart preserved

Cold process restart:
- PDP HTML 762ms; cart HTML 204ms

DB: cart Active v48; checkouts 0

## Tests

```
node --experimental-strip-types --test \
  lib/storefront-navigation/navigation-pending.test.ts \
  app/storefront/storefront-navigation-pending.guard.test.ts \
  app/storefront/storefront-cart-ui.guard.test.ts
→ 16 pass
npm run typecheck → 0
npm run build → 0
```

## Touched files

- `lib/storefront-navigation/navigation-pending.ts` (+test)
- `lib/storefront-navigation/navigation-pending-context.tsx`
- `lib/i18n/LocalizedLink.tsx`
- `app/providers.tsx`
- `app/products/[slug]/{page,loading}.tsx`
- `app/cart/loading.tsx`
- `app/storefront/storefront-api.ts` (React cache)
- `app/storefront/storefront-product-card.tsx`
- `app/storefront/storefront-mini-cart.tsx`
- `app/storefront/storefront-header.tsx`
- `app/storefront/storefront-pdp-qa.tsx`
- `app/storefront/storefront-cart-ui.guard.test.ts`
- `app/storefront/storefront-navigation-pending.guard.test.ts`
