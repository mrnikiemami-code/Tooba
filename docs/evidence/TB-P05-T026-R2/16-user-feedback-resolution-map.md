# 16 — User feedback resolution map (TB-P05-T026-R2)

| User complaint | Original Shopeiva source | Repair | Live backend binding | CSS restored | Hover restored | Motion restored | Final status |
|---|---|---|---|---|---|---|---|
| Best Sellers cards visually poorer | `components/home/bestSellers/bestSellers.jsx` | Ported column/row elevation, badges, action reveal, media | Catalog/Offer/Pricing/Inventory/Reviews via home `bestSellerColumns` | YES | YES | N/A (static grid like source) | **IMPLEMENTED — awaiting user review** |
| CSS/elevation/shadows/hover lost | Same + ProductCard patterns | Ported shadow/translate/scale/transition classes; accent `#2563EB` | Live product cards | YES | YES | Transitions restored | **IMPLEMENTED — awaiting user review** |
| Brand cards lost image/gradient/overlay | `components/home/brands/brands.jsx` | Swiper FreeMode + gradient + eye overlay; logo media | `logoMediaAssetId` on brands (+ CatalogBrand) | YES | YES | Swiper rail | **IMPLEMENTED — awaiting user review** |
| Newest Products lost autoplay carousel | `components/home/newProducts/newProducts.jsx` | Swiper Autoplay 4000ms / pauseOnMouseEnter / FreeMode | `newArrivals` live Offer pricing | YES | YES | **YES (CDP transform moved)** | **IMPLEMENTED — awaiting user review** |
| Customer Reviews removed | `components/home/testimonials/testimonials.jsx` | Section restored with Swiper + pagination | Live `featuredReviews` from Reviews module | YES | YES | Autoplay + bullets | **IMPLEMENTED — awaiting user review** |
| Latest Articles removed/inadequate | `components/home/blog/blog.jsx` | Section restored; minimal Content module | Live `latestArticles` published articles | YES | YES | Autoplay 5000ms + bullets | **IMPLEMENTED — awaiting user review** |

## Explicit Worker status

```text
HOME_REPAIR_IMPLEMENTED_AWAITING_USER_REVIEW
```

Worker does **not** claim `Home = MATCH`.

## Minor technical deviations (documented)

- Accent `#E53935` → `#2563EB`
- Review helpful/like counts omitted (no backend) — structure preserved
- Article view/comment counts omitted — no fabricated metrics
