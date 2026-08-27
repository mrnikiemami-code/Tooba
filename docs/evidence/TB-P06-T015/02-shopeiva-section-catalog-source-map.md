# 02 — Shopeiva section catalog source map (TB-P06-T015)

Reference root: `../SarvNewVerRequirment/reference/shopeiva/`

| Home section (Tooba type) | Shopeiva / Tooba renderer | Notes |
|---|---|---|
| `hero` | Home hero slider (`HomeHeroSlider`) | Locked carousel/autoplay |
| `stories` | Stories / category chips | Horizontal story strip |
| `category_grid` | Category grid block | Featured categories |
| `product_rail_flash` | Flash / special offers rail | `ProductRailSection` tone=accent |
| `best_sellers` | Best sellers columns | Multi-column Shopeiva pattern |
| `product_rail_most_viewed` | Most-viewed rail | `ProductRailSection` tone=plain |
| `middle_banners` | Middle promo banners | Static Shopeiva chrome + live media where bound |
| `brands` | Brands row | Catalog brands |
| `newest_products` | New arrivals grid | Catalog new arrivals |
| `customer_reviews` | Testimonials / reviews | Existing home reviews section |
| `latest_articles` | Articles rail | Content `latestArticles` (T013) |

## Mapping rule

- Inspect actual Home structure first; do **not** invent foreign section chrome.
- Admin configures **order / visibility / safe config only** — not CSS/JS/HTML.
- Tooba reuse path: `renderHomeSection` switch in `storefront-home.tsx` → existing section components.
