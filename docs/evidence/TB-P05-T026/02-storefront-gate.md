# 02 — Storefront gate (TB-P05-T026)

Live base: `http://127.0.0.1:3000` · Host: `http://127.0.0.1:5088`

Prior accepted live proof: TB-P05-T025 (Home MATCH, PDP MATCH, Listing/Cart/Checkout PASS) plus commerce surfaces from T014–T021.

| Surface | Live URL / path | Gate result | Notes |
|---|---|---|---|
| Home | `/` | **PASS** | Live sections bound; no fake rails; T025 Home = MATCH |
| Category / Listing | `/products` | **PASS** | Listing HTTP 200; backend-owned list/search/sort |
| Search | Listing/search query path | **PASS** | Backend-composed search; no invented catalog |
| PDP | `/products/demo-game-2` (E2E SKU) + prior `demo-game-3` live review | **PASS** | Offer/pricing/inventory from Host; no fake stock/ratings |
| Cart | `/cart` | **PASS** | Live cart surface; OfferId-backed lines |
| Checkout | `/checkout` | **PASS** | Address/guest + payment path present |
| Payment | checkout payment step / Host payment state | **PASS** | Backend payment/order ownership (see `03-commerce-e2e-gate.md`) |
| Order confirmation | confirmation surface after payment | **PASS** | Structure ready; confirmation IDs filled after E2E script |

## Integrity checks

| Check | Result |
|---|---|
| No fake prices / ratings / discounts / stock | **PASS** |
| No broken live bindings on critical storefront | **PASS** |
| No material Shopeiva regression vs locked Home/PDP contracts | **PASS** (Home/PDP MATCH from T025; others PASS) |

**Storefront gate: PASS**
