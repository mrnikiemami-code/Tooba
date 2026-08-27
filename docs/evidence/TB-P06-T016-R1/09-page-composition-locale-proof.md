# 09 — Page composition locale proof (TB-P06-T016-R1)

## Purpose

Prove Home Page Composition remains locale-route compatible: the same Home implementation loads composition for both `fa` and `en` via the content API, with matching section structure (no duplicate Home tree, no visual redesign).

Machine source: `_acceptance-proof.json` → `composition`

## API composition counts

| Locale | Section count | Result |
|---|---|---|
| `fa` | **11** | PASS |
| `en` | **11** | PASS |

## Section type order (identical)

Both locales returned the same ordered types:

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

## Routing / binding

| Check | Result |
|---|---|
| `/fa` Home HTTP | 200 (composition-backed storefront) |
| `/en` Home HTTP | 200 |
| Single Home implementation | Same `app/page.tsx` + `StorefrontShopeivaHome` under middleware rewrite |
| Locale → content API | `localeToContentApi` (`fa` → `fa-IR`, `en` → `en`) |
| Renderer reuse | T015 `renderHomeSection` switch unchanged |
| Admin locale edition scope | Still valid; routing Task does not fork composition schema |

## Visual / CSS scope

Composition proof is structural (section types + counts). No CSS/JS/carousel changes were introduced for locale routing. Visual parity is covered separately in `10-no-visual-regression-proof.md`.

## Verdict

`PAGE_COMPOSITION = LOCALE_ROUTE_COMPATIBLE` confirmed: 11 sections for both `fa` and `en` via API under prefixed routes.
