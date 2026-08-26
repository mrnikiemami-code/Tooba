# Home Fidelity Contract

Status: ACTIVE baseline for TB-P05-T019  
Locked UI: purchased Shopeiva Home structure  
Live data: Tooba Host storefront composition

## Canonical source components

- Tooba: `src/frontend/app/storefront/storefront-home.tsx`, `app/page.tsx`
- Backend rail ownership: `StorefrontComposer` `HomeCategories` / `BestSellerColumns` / `MostViewedProducts`
- Accepted visual evidence: `docs/evidence/TB-P05-T018/` (Architect-accepted Home fidelity path)

## Accepted screenshots

| Surface | Path |
| --- | --- |
| Shopeiva full | `docs/evidence/TB-P05-T018/02-original-shopeiva-home-full.png` |
| Tooba after desktop | `docs/evidence/TB-P05-T018/14-tooba-home-after-full.png` |
| Tooba after mobile | `docs/evidence/TB-P05-T018/20-tooba-home-after-mobile-390x844.png` |
| T019 desktop baseline | `docs/evidence/TB-P05-T019/06-home-desktop-baseline.png` |
| T019 mobile baseline | `docs/evidence/TB-P05-T019/07-home-mobile-baseline.png` |

## Section order (required)

1. Hero slider (`home-hero`)
2. Stories/circles (`home-stories`)
3. Home category rail (`home-categories`, ≤20 live categories — NOT full Catalog dump)
4. Flash / special offers rail (`home-flash-sales`) when data exists
5. Best sellers columns (`home-best-sellers`)
6. Most viewed rail (`home-most-viewed`)
7. Mid banners ×4 (`home-middle-banners`)
8. Brands (`home-brands`)
9. New arrivals (`home-new-products`)

Testimonials/Blog remain omitted without a live Content module (honest omission).

## Critical geometry / patterns

- Horizontal category rail (not giant multi-row catalog grid)
- Product rails use card family already used on storefront
- Container rhythm: `px-2 sm:px-4`, section spacing `py-8 md:py-10`
- Header + Mega Menu remain outside Home body but must stay integrated

## Mobile behavior

- 390×844 capture required
- Horizontal overflow for stories/categories/rails preserved
- No desktop-only collapse that removes required sections

## Forbidden deviations

- Giant category grid / dumping full Catalog on Home
- Arbitrary section reorder
- Generic grid replacing rails
- Card-family replacement / personal redesign
- Fake promotions, ratings, or prices

## Allowed minimal technical deviations

- No fake flash countdown when Promotion end seam absent
- Best-seller / most-viewed ranking uses available live signals
- Template mid-banner media with live route targets

## Backend binding expectations

- Prices/availability from Offer/Pricing/Inventory composition
- HomeCategories capped rail from Catalog hierarchy
- No Product.Price / Product.Stock authority on UI
