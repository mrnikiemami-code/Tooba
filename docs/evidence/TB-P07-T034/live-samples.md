# Live samples — TB-P07-T034

## List at scale
- `GET /v1/admin/products?page=1&pageSize=20` → 200
- `POST /v1/admin/products/query` server paging → totalCount **287** (283 demo Draft + 4 legacy Published leftovers outside seam reset scope for non-demo slugs)
- Demo products status: **Draft=283**, Published demo=0

## Sample coverage (VIEW via `GET /v1/admin/products/{id}`)

| Domain sample | Title | Status | Media | Variants | Publish Ready |
|---|---|---|---:|---:|---|
| Feature phone | گوشی ساده مجموعه 4 | Draft | 5 (1 primary) | 3 | yes (`publication.aggregateReadiness.isReady`) |
| Laptop bag | زارا کیف لپ‌تاپ نسخه 3 | Draft | 5 | 0 | yes |
| Shoes/clothing | نایک کفش زنانه مجموعه 4 | Draft | 5 | 9 | yes |
| Home appliance | پاناسونیک یخچال و فریزر انتخاب 5 | Draft | 5 | 0 | yes |
| Tool | کیچن‌اید آچار مجموعه 4 | Draft | 5 | 0 | yes |
| Watch | نایک ساعت کلاسیک مجموعه 4 | Draft | 5 | 0 | yes |
| Cookware | پاناسونیک قابلمه انتخاب 5 | Draft | 5 | 0 | yes |

Also observed leaf categories with 3–5 products: گوشی هوشمند, اولترابوک, پیراهن مردانه, کتاب کودک, تنقلات, etc.

fa/en translations, SEO title/description, TipTap HTML description present on detail payload.
Commercial readinessWarnings (no-offer/price/stock) expected — Catalog-only task; no Price/Stock introduced.
