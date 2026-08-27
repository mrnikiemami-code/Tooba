# 04 — Locale link preservation (TB-P06-T016-R1)

## Purpose

Prove storefront internal links keep the active public locale prefix: browsing under `/fa` yields `/fa/...` targets; under `/en` yields `/en/...`. Unprefixed public storefront links must be empty on primary surfaces. Panel paths may remain unprefixed **by design**.

Machine source: `_acceptance-proof.json` → `browser.desktop[].links` / `browser.mobile[].links`

## Method

Headless Chrome CDP captures collected `a[href]` samples from live pages after navigation. Probe classified:

- **prefixed public** — `/fa/...` or `/en/...`
- **unprefixedPublic** — storefront paths missing locale prefix (must be `[]`)
- **panels** — `/admin/...`, `/customer-panel/...`, `/vendor-panel/...` (allowed unprefixed)

## Primary surface results

| Capture | Page | Sample public links (excerpt) | `unprefixedPublic` |
|---|---|---|---|
| `01-fa-home-desktop.png` | `/fa` | `/fa`, `/fa/products`, `/fa/cart`, `/fa/offers`, `/fa/products?categoryId=...` | **[]** |
| `02-en-home-desktop.png` | `/en` | `/en`, `/en/products`, `/en/cart`, `/en/offers`, `/en/products?categoryId=...` | **[]** |
| `03-fa-pdp-desktop.png` | `/fa/products/demo-game-3` | `/fa`, `/fa/products`, `/fa/products/demo-game-2` | **[]** |
| `04-en-pdp-desktop.png` | `/en/products/demo-game-3` | `/en`, `/en/products`, `/en/products/demo-game-2` | **[]** |
| `05-fa-blogs-desktop.png` | `/fa/blogs` | `/fa/blogs/guide-online-shopping`, `/fa/blogs/mobile-buying-tips`, … | **[]** |
| `09-fa-home-mobile.png` | `/fa` | `/fa`, `/fa/products`, … | **[]** |
| `10-en-home-mobile.png` | `/en` | `/en`, `/en/products`, … | **[]** |

## Panel links (by design unprefixed)

On Home/PDP samples, panel hrefs remain:

- `/customer-panel/wishlist`
- `/admin/products`

These are **not** counted as `unprefixedPublic` storefront SEO leaks. Account-scoped panels stay outside the public locale prefix contract (T016 route map).

## Implementation basis

- `LocalizedLink` wraps Next `Link` and applies `localePath(activeLocale, internal)` unless path is panel/API/excluded or `unprefixed` is forced.
- `LocaleProvider.localizePath` / `useLocalizedPath` keep client-side navigations on the URL-derived locale after the R1 sync repair.

## Verdict

From `/fa`, links stay `/fa`; from `/en`, links stay `/en`. `unprefixedPublic = []` on primary surfaces. Panels remain unprefixed by design.
