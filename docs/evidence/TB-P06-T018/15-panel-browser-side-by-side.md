# 15 — Panel browser side-by-side (TB-P06-T018)

## Status: PLACEHOLDER

Browser captures and comparison script will be added under `docs/evidence/TB-P06-T018/captures/`.

## Required capture set (to record)

### Customer — newly completed surface (settings)

| # | Capture | Viewport / state |
|---|---|---|
| C1 | Tooba customer settings | desktop |
| C2 | Tooba customer settings | mobile (~390) |
| C3 | Tooba customer settings | interactive (locale / profile bridge) |
| C4 | Shopeiva account/settings comparison (if available) | desktop |

### Seller — newly completed surface (settings)

| # | Capture | Viewport / state |
|---|---|---|
| S1 | Tooba vendor settings | desktop |
| S2 | Tooba vendor settings | mobile (~390) |
| S3 | Tooba vendor settings | interactive / loaded operational state |
| S4 | Shopeiva vendor settings comparison (if available) | desktop |

### Admin — newly completed surface (nav honesty)

| # | Capture | Viewport / state |
|---|---|---|
| A1 | Tooba admin shell without settings nav | desktop |
| A2 | Tooba admin shell without settings nav | mobile |
| A3 | Optional deep-link `/admin/settings` honest unavailable | desktop |

### Nav integrity spot checks

| # | Capture | Notes |
|---|---|---|
| N1 | Customer primary nav live-only | no wallet/tickets/gift-cards/notifications |
| N2 | Seller primary nav live-only | no customers/coupons/reviews/tickets/gift-cards |
| N3 | Admin primary nav without settings | — |

## Follow-up artifacts

- `captures/README.md` — index of PNGs once recorded  
- Proof script (e.g. `scripts/prove-t06-t018-panels.mjs`) — optional automation  

Until captures land, this file documents the required proof surface only.
