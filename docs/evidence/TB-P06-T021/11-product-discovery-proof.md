# 11 — Product discovery proof (TB-P06-T021)

Rule: sellable product must be discoverable through at least one **legitimate** storefront path — not only a hardcoded demo PDP URL.

## Legitimate discovery paths

| Path | Route / API | Status |
|---|---|---|
| Listing | `http://127.0.0.1:3000/fa/products` · `GET /v1/storefront/products` | LIVE |
| Category / filters | Query on listing (`category`, search params via `products/page.tsx`) · storefront category endpoints | LIVE |
| Search | Listing search against Host catalog compose | LIVE |
| Home rails | `http://127.0.0.1:3000/fa` · home compose includes product rails where seeded/authored items qualify | LIVE |
| Direct PDP | `/fa/products/{slug}` | LIVE (must not be the **only** path) |

## Eligibility for listing inclusion

Same gate as PDP (`08-storefront-sale-eligibility.md`):

1. Catalog Product **Published**
2. Offer **Active**
3. Pricing **Active** base for market/channel/currency
4. Inventory available units **> 0**

Admin `POST /v1/admin/products` + Seller Offer/price/inventory writes (`05`–`07`) produce rows that enter the same `StorefrontComposer` listing filter as Development seed — **no direct DB mutation** required for discovery.

## Explicit non-claim

- Discovery does not require advanced multi-axis variant matrices (`ADVANCED_VARIANT_DEFERRED`).
- Hardcoded seed-only PDP is **not** the sole proof path once seller-authored Offers are Active with price+stock.

## Verdict

```text
PRODUCT_DISCOVERY = LIVE
```
