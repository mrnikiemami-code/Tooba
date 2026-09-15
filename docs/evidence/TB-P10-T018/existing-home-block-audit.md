# Existing Home block audit — TB-P10-T018

Sources: `composition-api.ts` DEFAULT_HOME_SECTION_ORDER, `storefront-home.tsx`, repair section components.

| Current Component | Usage | Proposed Section Type | Proposed Variant | Reuse Strategy | Risk |
|---|---|---|---|---|---|
| Hero carousel (Swiper) | Home hero | HeroCarousel | hero.full-width | Reuse With Adapter | Low — already Swiper |
| Stories rail | stories | StoryRail | story.circle | Reuse As-Is | Low |
| Category grid/rail | category_grid | CategoryShowcase | category.image-cards | Reuse With Adapter | Low |
| Flash / deal product rail | product_rail_flash | ProductShowcase | product.card-carousel | Reuse With Adapter | Med — source heuristic |
| Best seller columns | best_sellers | ProductShowcase | product.category-columns | Reuse With Adapter | Med — not true BestSelling API |
| Most viewed compact rows | product_rail_most_viewed | ProductShowcase | product.compact-rows | Reuse With Adapter | Med — ReviewCount heuristic |
| Middle banners mosaic | middle_banners | BannerShowcase | banner.one-large-two-small | Refactor Into Shared Section Variant | Med — layout variants |
| Brands rail | brands | BrandShowcase | brand.logo-rail | Reuse As-Is | Low |
| Newest products rail | newest_products | ProductShowcase | product.card-carousel | Reuse With Adapter | Low — Newest supported |
| Customer reviews carousel | customer_reviews | ReviewsShowcase | reviews.card-carousel | Reuse As-Is | Low |
| Latest articles rail | latest_articles | ArticleShowcase | article.magazine-rail | Reuse As-Is | Low |
| Promo (via banners/flash) | overlapping | PromoSection | promo.default | Prefer Variants / avoid new type | Low |

No mandatory Home order after migration; current seed order remains fallback until migration accepted.
