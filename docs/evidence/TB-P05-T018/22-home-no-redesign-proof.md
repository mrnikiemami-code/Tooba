# 22 — Home No-Redesign Proof

Task: `TB-P05-T018`

## Source components ported (structure/classes)

- Hero heights / rounded-3xl / multi-slide pattern from `home/slider`
- Stories circle rail from `home/stories`
- Categories horizontal square cards from `home/categories` (≤20)
- Flash rail widths from `home/flashSales` (countdown intentionally omitted)
- Best sellers 4-col max-h scroll from `home/bestSellers`
- Most viewed product rail from `home/mostViewed`
- Middle banners 2×2 aspect from `home/middleBanners`
- Brands square tiles from `home/brands`
- New products rail from `home/newProducts`

## Tooba-specific necessary changes

- Accent `#2563EB` (locked Tooba token) instead of Shopeiva red
- Live Host bindings (`/v1/storefront/home` fields)
- Native horizontal scroll instead of Swiper (npm registry ECONNRESET)
- Most-viewed ordering by `reviewCount` (no Catalog ViewCount)
- Testimonials/Blog omitted (no Content module)

## Demo bindings replaced

- JSON/demo category dump → `homeCategories`
- Static-only grids → live Offer-priced cards
- Brand text chips → live brands with slugs

## Structural changes NOT made

- No new Home redesign system
- No giant category management grid
- No Mega Menu regression
- No Product.Price / fake ratings / fake countdown
