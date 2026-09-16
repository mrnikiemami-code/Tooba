# Recovery Start — TB-P10-T022-R11

## Preflight

| Check | Value |
|---|---|
| branch | `main` |
| HEAD | `701966357fb48815359dc280f5163ccb6df4c481` |
| origin/main | `701966357fb48815359dc280f5163ccb6df4c481` |
| HEAD == origin/main | YES |
| Expected previous HEAD | `701966357fb48815359dc280f5163ccb6df4c481` |
| git status tracked | clean (untracked `.tmp-*` / evidence leftovers preserved) |
| Last Architect-accepted | TB-P10-T022-R10 (task: ACCEPTED) |
| Last Implementation (pre) | TB-P10-T022-R10 |
| Current Task | TB-P10-T022-R11 |
| Locks preserved | LOCK-SF-001…334 |
| Appearance | USER_VISUAL_ACCEPTED=YES |
| Builder | USER_VISUAL_ACCEPTED=NO |
| RECOVERY_CONFLICT | none |

## Current Variant registry (families)

- HeroCarousel: hero.full-width, hero.contained, hero.split, hero.side-promos, hero.editorial
- StoryRail: story.circle, story.image-circles, story.rounded-cards, story.icon-shortcuts
- CategoryShowcase: category.image-cards, category.compact-tiles, category.horizontal-rail, category.editorial-tiles
- ProductShowcase: product.card-carousel, product.grid, product.compact-rows, product.category-columns, product.featured-plus-rail, product.tabbed, product.large-cards, product.minimal-list
- ProductRankedList: ranked.horizontal, ranked.grid, ranked.ticker, ranked.multi-column
- BannerShowcase: banner.single, banner.two-equal, banner.two-asymmetric, banner.three, banner.four-grid, banner.one-large-two-small, banner.one-large-four-small, banner.eight-compact (+ hidden alias banner.mosaic-2x2)
- BrandShowcase: brand.logo-rail, brand.logo-grid, brand.featured
- ReviewsShowcase: reviews.card-carousel, reviews.compact-quotes
- ArticleShowcase: article.magazine-rail, article.grid, article.featured-plus-list
- Promo/RichText/Nav: promo.default, richtext.default, nav.menu

## Geometric Variant picker (pre-R11)

- `layout-aware-previews.tsx` → `VariantPreviewCanvas` / `layoutAwareVariantPreview` CSS mosaics
- Used in `admin-section-wizard.tsx` Appearance + Review steps

## Review step (pre-R11)

- Geometric canvas + plain FA summary (`review-plain-summary`)

## Production mapping

- `shared-composition-renderer.tsx` → `renderSharedLandingSection` / `renderSharedHomeSection`

## PreviewFakeData

- `preview-fake-data.ts`, `preview-fake-media.ts`, `preview-fake-locale.ts`, `preview-fill-policy.ts`
- Cardinality via `PREVIEW_CARDINALITY` / `previewTargetItems` (R8)

## Swiper

- Brands / Reviews / Articles / Stories use Swiper; Product rails use production overflow-x scroll contract

## Unrelated user changes

- Preserved untracked `.tmp-*` / prior evidence probes — not staged
