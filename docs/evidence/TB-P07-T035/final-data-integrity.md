# final-data-integrity.md — TB-P07-T035

`USER_VISUAL_ACCEPTED=NO`

## Live status (`GET /v1/admin/catalog/demo/status`)

After polish media reseed (`POST /v1/admin/catalog/demo/reset-and-seed`, ~163s):

| Metric | Value |
|---|---|
| rootsDemo | 15 |
| categoriesDemo / Total | 116 / 116 (L1=15, L2=28, L3=73) |
| brandsDemo / Total | 22 / 22 |
| tagsDemo / Total | 36 / 38 (2 non-demo residual tags tolerated; demo seam = 36) |
| attributesDemo | 41 |
| productsTotal / Demo / Draft | 283 / 283 / 283 |
| productsPublished | 0 |
| productsArchived | 0 |
| allowResetAndSeed | true |
| environment | Development |

## Grid

`POST /v1/admin/products/query` → `totalCount=283`. UI paging shows «از ۲۸۳».

## Architecture locks (spot)

- Product response exposes empty `prices`/`stock` envelopes; no Product.Price/Stock columns in Admin grid.
- Brandless products use null brand → UI «بدون برند» (no fake No-Brand entity).
- Media: exactly 5 per sampled product, one Primary (API + UI «۵ مورد / آماده»).
