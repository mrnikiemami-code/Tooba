# 03 — Section catalog (TB-P06-T015)

Owner: `SectionCatalog` in `Tooba.PageComposition.Domain`

## Stable types (string ids — NOT React component names)

1. `hero`
2. `stories`
3. `category_grid`
4. `product_rail_flash`
5. `best_sellers`
6. `product_rail_most_viewed`
7. `middle_banners`
8. `brands`
9. `newest_products`
10. `customer_reviews`
11. `latest_articles`

## Catalog metadata

| Field | Rule |
|---|---|
| Variant | `default` only (MVP) |
| Allowed config keys | `title`, `href`, `itemCount`, `sourceKind` |
| Forbidden keys | `css`, `html`, `js`, `className` |
| `itemCount` | 1–24 |
| `title` max | 120 |
| `href` max | 256 |
| `sourceKind` | `offers` \| `most_viewed` \| `new_arrivals` |
| Repeat | Catalog-controlled; unknown type rejected |

## HTTP

`GET /v1/admin/page-composition/home/catalog` → types + allowed variants + schema metadata.

Unknown section type → validation error (foundation tests cover rejection).
