# 16 — UI fidelity proof (TB-P06-T007)

## 17-ui-fidelity-proof: no UI file changed

No Shopeiva UI component, page layout, CSS, or visual asset files modified.

## Scope of changes

API layer only:

- Next.js BFF routes under `src/frontend/app/api/`
- Auth/session libraries under `src/frontend/lib/`
- Customer/storefront **API client** modules (not UI components)

## Regression guard

| Suite | Expected |
|---|---|
| `npm run test:critical-storefront` | PASS |
| Home / PDP structure guards | unchanged |

Visual governance remains locked; unauthorized UI deviation = VISUAL REGRESSION.
