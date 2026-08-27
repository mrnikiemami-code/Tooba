# 04 — Locale-prefixed route map (TB-P06-T016)

## Public (prefixed)

| Internal path | Public fa | Public en |
|---|---|---|
| `/` | `/fa` | `/en` |
| `/products` | `/fa/products` | `/en/products` |
| `/products/{slug}` | `/fa/products/{slug}` | `/en/products/{slug}` |
| `/blogs` | `/fa/blogs` | `/en/blogs` |
| `/blogs/{slug}` | `/fa/blogs/{slug}` | `/en/blogs/{slug}` |
| `/cart` | `/fa/cart` | `/en/cart` |
| `/checkout` | `/fa/checkout` | `/en/checkout` |
| Merch (`/offers`,`/sale`,`/new-products`,`/most-viewed`,`/best-seller`,`/brands`,`/brand`,`/sellers`,`/seller-profile`,`/trending`,`/order`,`/payment/result`) | `/{fa\|en}/...` | same |

Implementation: middleware rewrites `/{locale}{internal}` → internal App Router path + `x-tooba-locale`.

## Excluded (unprefixed)

| Path | Reason |
|---|---|
| `/admin/**` | Account/ops; not SEO-public |
| `/customer-panel/**` | Account-scoped |
| `/vendor-panel/**` | Seller account-scoped |
| `/api/**` | BFF/internal |
| `/design-system/**`, `/payment/sandbox`, static assets | Non-storefront |

## APIs

No backend route changes. Content/Composition called with BCP-47 from `localeToContentApi`.
