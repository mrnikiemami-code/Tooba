# Final Product List — TB-P07-T031

## Live
- `/fa/admin/products` → 200
- `GET /v1/admin/products` → 82 rows
- `POST /v1/admin/products/query` → server page (pageSize 5) with Host-composed fields

## Columns (canonical AppDataGrid)
| Gate | Live |
|------|------|
| image / media readiness | media thumb column (`primaryMediaAssetId` → storefront URL; empty = not ready) |
| title | yes |
| Brand | **yes — repaired this gate** (`brandName` on Host list DTO + FE column) |
| Primary Category | `categorySummary` (+ `primaryCategoryId`) |
| lifecycle | status badge |
| variant count | `variantCount` (تنوع) |
| updated date | Jalali formatter |
| actions | row actions VIEW/EDIT + lifecycle |
| Product Price/Stock | **absent** (offer fields hidden only) |

## Repair (scoped)
- Host `AdminProductListItem.BrandName` + composer brand name load
- FE `AdminProductListRow.brandName` + column «برند» + export
- Live sample after restart: `brandName=آرمان` (`live-product-list-item-after.json`, `live-products-with-brand.json`)

## Create entry
- No inline `?create=1`
- Add Product → dedicated `/admin/products/new`

## Nav
- Single active leaf for products list (integrity tests green)
