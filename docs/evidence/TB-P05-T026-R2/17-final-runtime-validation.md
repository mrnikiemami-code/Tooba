# 17 — Final three-runtime + validation (TB-P05-T026-R2)

## Runtimes at Result (left running)

| Runtime | URL | Status |
|---|---|---|
| Tooba Backend | http://127.0.0.1:5088/health | UP |
| Tooba Frontend | http://127.0.0.1:3000/ | UP (Home + PDP 200) |
| Original Shopeiva | http://127.0.0.1:3017/ | UP |

## Backend

Prior session: `scripts/run-backend-validation-with-nuget-proxy.ps1` → **205 passed / 0 failed / 0 skipped / 0 warnings** after Content + Brand logo + Featured reviews.

Host rebuilt and restarted so migrations apply on live Development DB. Home API smoke: brands with `logoMediaAssetId`, `featuredReviews` count 7, `latestArticles` count 4.

## Frontend

| Check | Result |
|---|---|
| typecheck | PASS |
| lint | PASS |
| test:critical-storefront | PASS |
| test:storefront | PASS |
| test:customer | PASS |
| test:seller | PASS |
| build | PASS (after stopping next dev / clearing `.next`) |

## Motion

See `13-home-motion-interaction-proof.md` — Newest Products Swiper transform changed without user input.
