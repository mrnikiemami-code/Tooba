# 06 — Section data source boundaries (TB-P06-T015)

Page Composition stores **arrangement**, not commercial payloads.

| Section type | Live data owner | Binding |
|---|---|---|
| `hero` / `stories` / `category_grid` / `middle_banners` | Catalog / storefront home composer | Existing home props |
| `product_rail_flash` | Offers / special offers | `specialOffers` → `ProductRailSection` |
| `best_sellers` | Catalog best-seller columns | `bestSellerColumns` |
| `product_rail_most_viewed` | Catalog / discovery | `mostViewedProducts` |
| `brands` | Catalog brands | `brands` |
| `newest_products` | Catalog new arrivals | `newArrivals` |
| `customer_reviews` | Reviews / featured reviews | `featuredReviews` |
| `latest_articles` | Content module | `latestArticles` (T013) |

## Rules

- No cross-module SQL JOIN from `page_composition` into Catalog/Content tables.
- Public composition API returns ordered **visible** section descriptors; Host/Frontend bind live payloads via existing contracts.
- Empty product arrays → section may omit render (honest empty), not fake cards.
