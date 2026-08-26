# 13 — Home motion / interaction proof (TB-P05-T026-R2)

Deterministic CDP checks against live Tooba Home after repair.

```json
{
  "sections": {
    "bestSellers": true,
    "brands": true,
    "newProducts": true,
    "testimonials": true,
    "articles": true
  },
  "newProductsAutoplay": {
    "transformBefore": "matrix(1, 0, 0, 1, 0, 0)",
    "transformAfter": "matrix(1, 0, 0, 1, 236, 0)",
    "moved": true,
    "hasSwiper": true
  },
  "reviewsPagination": true,
  "articlesPagination": true,
  "brandHasOverlayClasses": true,
  "bestHasHoverShadowClasses": true,
  "articleHasHoverLift": true
}
```

| Check | Result |
|---|---|
| Newest Products Swiper present | PASS |
| Newest Products translate changed without user input (~4.5s) | PASS (autoplay) |
| Reviews pagination bullets | PASS |
| Articles pagination bullets | PASS |
| Brand gradient overlay classes | PASS |
| Best Sellers hover shadow/lift classes | PASS |
| Articles hover lift classes | PASS |

Autoplay config (source-compatible): delay 4000ms, `pauseOnMouseEnter: true`, `disableOnInteraction: false` (New Products / Testimonials); Articles delay 5000ms.

**Motion proof: PASS**
