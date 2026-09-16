# SEO Regression — TB-P10-T022-R13

## Scope

Home and Landing page SEO fields verified against R9 foundation + R9-R1 performance path.

## Admin fields (both page types)

| Field | Present |
| --- | --- |
| title / seoTitle | YES (`page-seo-panel`) |
| meta description | YES |
| canonical | YES |
| Index / NoIndex | YES |
| Follow / NoFollow | YES |
| OpenGraph title | YES |
| OpenGraph description | YES |
| OpenGraph image | YES |
| hreflang / locale | YES (language registry) |
| robots composition | YES |
| H1 policy | YES — single primary H1 (`store-page-primary-h1` / PrimaryH1 override) |
| typed structured data | YES — Home: WebSite+WebPage; Landing: WebPage+BreadcrumbList |
| sitemap inclusion/exclusion | YES — noindex excluded (StorePagesFoundationT022R9) |

## Storefront

- `storefront-page-seo.ts` builds metadata + JSON-LD
- No duplicate/conflicting robots/canonical tags introduced
- No raw JSON SEO editor for ordinary Admin users
- No mandatory per-section SEO wizard step

## Regression vs R9-R1

Warm Home SSR remained healthy (~516–570 ms local). No SEO-path waterfall/N+1 reintroduced.

## Evidence

- Screenshots: `home-seo-final.png`, `landing-seo-final.png`
- Host: `StorePagesFoundationT022R9Tests` PASS
- FE: `admin-store-pages-r9.guard.test.ts` PASS
