# Category → Products — display-only (TB-P07-T036)

## Decision
Category Admin → Products manages **only** «نمایش در این دسته» (Additional membership).
It never creates or changes Primary Category.

## Implementation
- `category-products-panel.tsx`: always `addAdminProductAdditionalCategory`; removed `assignAdminProductCategory`.
- CTAs: «افزودن محصول برای نمایش در این دسته», row «افزودن برای نمایش», bulk «افزودن موارد انتخاب‌شده برای نمایش (N)».
- Badges: Primary «دسته اصلی»; Additional «نمایش در این دسته».
- Optional non-blocking facet compatibility toast (section O).

## Tests
Source asserts in `category-products-panel.test.ts` (no Primary assign path).
