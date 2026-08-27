# 10 — Storefront Story no-drift (TB-P06-T019-R1)

## Lock statement

Storefront Story UI remains Shopeiva-canonical from TB-P06-T017.

| Surface | Change in T019-R1 |
|---|---|
| `app/storefront/stories/home-stories.tsx` (`HomeStoriesSection`) | Untouched |
| Story rail / modal / Swiper / progress / timing / animation / CSS | Untouched |
| Public API consumer `GET /v1/storefront/stories` | Same path; backend eligibility filter tightened only |
| Seller-specific storefront renderer | **None** |

## Projection path

Admin-origin **or** Approved seller-origin → same public API → same `HomeStoriesSection` → same viewer.

Guards: `home-structure.guard.test.ts` still expects `<HomeStoriesSection`. Visual contract: `SHOPEIVA_LOCKED`.
