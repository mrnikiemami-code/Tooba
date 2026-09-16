# Performance Baseline — TB-P10-T022-R9-R1

Measured against Host `:5088` + FE `:3000` before code changes (post FE restart for isolation).

## Host APIs

| Call | Status | Time |
|------|--------|------|
| `GET /health` | 200 | ~26 ms |
| `GET /v1/storefront/pages/landing-demo?locale=fa` | 200 | ~16–20 ms |
| `GET /v1/storefront/pages` | 200 | ~17 ms |
| `GET /v1/storefront/home?locale=fa-IR` | 200 | ~2300 ms |
| `GET /v1/storefront/products?sort=newest` | 200 | ~2200 ms |

## FE SSR (application path)

| Scenario | Time | Notes |
|----------|------|-------|
| Cold `/landing/landing-demo` (after FE restart) | ~5819 ms | Includes Next compile of `/landing/[slug]` |
| Warm landing ×3 | ~2310 / 2315 / ~2750 ms | Low-single-digit seconds when FE healthy |
| `/` Home | ~2680 ms | |
| Landing under concurrent FE saturation (prior R9 capture / hung FE) | ~90–180 s | FE log: `GET /landing/landing-demo 200 in 179990ms` while WorkingSet ~3.6GB; Host resolve stayed fast |

## Metadata / request shape (pre-fix)

- `generateMetadata` + page each called `loadPublishedLandingPage` → **duplicate Host page resolve**
- Landing page also `Promise.all(loadStorefrontHome, loadLandingRenderContext)` where context **re-fetched home** + **always fetched full products listing**
- Effective waterfall: 2× page + 2× home + 1× listing (+ menus)

Cold compile/dev-bundle overhead is documented separately from application SSR stall (duplicate fetches + listing waterfall + FE overload).
