# TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R4 — WORKER RESULT

Status: **PASS** within scope. Stop-State: STOP_FOR_ARCHITECT. automaticNextImplementationTask: NONE.  
Worker ≠ Architect / Visual ACCEPT.

## Defect

`app/cart/page.tsx` awaited full `loadStorefrontHome()` before the cart route could render (shell categories + recommendations). Mini-cart CTA used LocalizedLink but only the global 1px bar acknowledged the click, so the drawer looked idle until route commit.

## Repair

- Cart RSC critical path: `loadStorefrontCategories()` only; empty `searchCatalog` (no Home block).
- Recommendations: client `pickCartRecommendations(loadStorefrontHome())` starts only after primary cart `loading===false`, one owner, cancel on unmount; quiet failure keeps cart visible.
- Mini-cart CTA: action-level FA/EN busy label + spinner + `aria-busy` / `data-action-pending` while navigation pending targets `/cart`; global pending bar retained; LocalizedLink duplicate/native rules unchanged.
- Ownership ledger (`storefront-cart-read-ownership.ts`) + stale-generation/cancel on cart route, mini-cart, header badge.

## Proof

Focused: cart-navigation.guard + cart-ui.guard + navigation-pending(+guard) + destination-choice.guard → **32/32**; typecheck clean; production build exit 0.

Live FE3000→Host5088 (buyer `01a0996c…`, cart `01a17b2d…` Active v48 qty18):

| Metric | Value |
| --- | --- |
| click→pending (CTA) | ~9 ms |
| click→cart usable | ~393 ms |
| `/v1/storefront/home` before usable | **0** |
| home after usable (recommendations) | 1 |
| beforeunload / hard reload | 0 / 0 |
| 3× product→mini-cart→cart cycles | +1 mount cart / +1 mini open / +1 rec per cycle (no runaway) |

245-R2 timeout + 245-R3 destination guards remain green. No cart/shipping/payment mutation.

## SHAs

Expected-Base / Base: `c659914b346350e52b71d7de4e46c160bda4b10f`  
Code-SHA: `21ee8e8bc2d52e5c6bbc4a0952b7af3654bc6125`
Evidence-SHA: `8e18c2fe740d227b44b9f9c0552342bbc1233509`
