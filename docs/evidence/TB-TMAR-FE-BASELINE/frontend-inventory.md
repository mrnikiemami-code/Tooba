# Frontend inventory — TB-TMAR-FE-BASELINE

## Canonical root

Task text referenced `src/Web`. Repository canonical frontend root is **`src/frontend`** (Next.js App Router package `tooba-web`). No `src/Web` directory exists. All inventory paths use `src/frontend`.

## Totals (hand-written scan; excludes node_modules/.next)

| Metric | Count |
| --- | ---: |
| All files under frontend (excl. node_modules/.next) | 971 |
| TS/TSX/JS/JSX source | 710 |
| Test / guard files | 167 |
| CSS/style files | 5 |
| Route composition files (page/layout/loading/error/…) | 152 |
| `page.tsx` routes | 144 |
| `layout.tsx` | 6 |
| `"use client"` files | 253 |
| Oversized (>800) + Critical (>1200) | 26 |
| WATCH (>500) | 39 |

## Ownership (by path heuristic)

- **admin**: 268
- **design-system**: 94
- **app-other**: 87
- **storefront**: 76
- **lib**: 74
- **seller**: 38
- **customer**: 30
- **content**: 22
- **ops**: 11
- **other**: 10

## App Router top-level under `app/`

- `[slug]`
- `access-control`
- `admin`
- `api`
- `best-seller`
- `blogs`
- `brand`
- `brands`
- `cart`
- `category`
- `checkout`
- `composition`
- `content`
- `customer-panel`
- `design-system`
- `evidence`
- `fulfillment`
- `landing`
- `login`
- `most-viewed`
- `new-products`
- `offers`
- `order`
- `payment`
- `products`
- `returns`
- `sale`
- `seller-profile`
- `sellers`
- `settlement`
- `shipping`
- `storefront`
- `stories`
- `support`
- `template-preview`
- `trending`
- `vendor-panel`
- `wallet`

## Structure notes

- **Admin**: large flat accumulation under `app/admin/*-screen.tsx` + panels + `admin-api.ts` (god API client).
- **Storefront**: route pages under `app/{category,products,cart,...}` compose modules in `app/storefront/` (implementation co-located under app, not `features/`).
- **Shared UI**: `design-system/` (AppDataGrid, category tree, workspace primitives).
- **Libs**: `lib/i18n`, `lib/auth`, `lib/storefront-appearance`, `lib/storefront-composition`.
- **Generated/vendor excluded from size analysis**: `node_modules`, `.next`, minified assets.

Machine-readable: `frontend-inventory.json`.
