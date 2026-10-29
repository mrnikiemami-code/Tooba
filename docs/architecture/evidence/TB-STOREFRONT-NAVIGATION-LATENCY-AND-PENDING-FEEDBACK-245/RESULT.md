# TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245 — WORKER RESULT

Status: **PASS** within scope. Stop-State: STOP_FOR_ARCHITECT. automaticNextImplementationTask: NONE.  
Worker result ≠ Architect / Visual / PSP acceptance.

## Measured root cause

1. **PDP RSC waited on a full `/v1/storefront/home` (~168ms payload ~68KB) solely for shell categories**, in parallel with detail, while `generateMetadata` fetched detail again without React `cache` dedupe — compounding soft-nav latency. Dev `next dev` first-compile made the same click feel like “minutes”; production measurement is the acceptance basis.
2. **No navigation-pending acknowledgment** — product/cart clicks had zero `aria-busy` / pending UI, so stalls were indistinguishable from missed clicks.
3. **Mini-cart “تکمیل خرید” used a hard `<a>` full document navigation** instead of soft App Router navigation with pending feedback.
4. Header `navigationRoots` was a new array every render (effect churn); brands/QA client fetches lacked cancellation (Strict-Mode doubles in dev).

## Before / after (canonical production `next start` on :3000 → Host :5088)

| Journey | Pending feedback | Usable content | Home API on click | Notes |
| --- | --- | --- | --- | --- |
| BEFORE product (dev measure) | none | ~2106ms (RSC ~1993ms incl. compile) | home bundled in RSC | duplicates header/brands/cart/questions in Strict Mode |
| AFTER warm product | **10ms** | **694ms** | **0** | 1 RSC; categories+cached detail; pending cleared |
| AFTER warm cart (mini-cart CTA) | **15ms** | **347ms** | n/a (cart page still uses home once for recommendations) | LocalizedLink soft-nav |
| AFTER cold process PDP HTML | — | **762ms** | — | ≤5s cold budget |
| AFTER cold process cart HTML | — | **204ms** | — | ≤5s cold budget |

## Source repair

- PDP `page.tsx`: `loadStorefrontCategories` instead of `loadStorefrontHome`; `loadStorefrontDetail` / categories wrapped in React `cache`.
- `LocalizedLink` + `StorefrontNavigationPendingProvider`: ≤100ms pending bar + `aria-live` / `aria-busy`; duplicate same-target clicks prevented; modified/middle clicks native; cleared on route match and `popstate`.
- Product-card eye action announces pending before `router.push`.
- Mini-cart CTA → `LocalizedLink` `/cart`.
- `loading.tsx` for `products/[slug]` and `cart`.
- Header `navigationRoots` memoized; brands/QA effects cancel on unmount.

## Focused proof

16/16 navigation + cart-ui guard tests; `npm run typecheck` clean; production build succeeded.

## Canonical acceptance

FE production `127.0.0.1:3000` → Host `5088` / SingleStore / store-alpha / `tooba_alpha`.  
Cart `01a17b2d-e219…` Active v48 qty18, checkouts 0; shipping config untouched. Final usable surfaces: PDP and `/fa/cart`. No checkout/Order/payment.

## SHAs

Expected-Base-SHA: `717c2dd42` (issue commit containing this task)  
Base-SHA: `2620f80911d2af6a67805f92d253b3371486894b` (main at claim; parent 244-R3 source present)  
Code-SHA: `b9d0f1bddb1d8fa1c9c6c7bfb27898e48d0acc64`
Evidence-SHA: `b9d0f1bddb1d8fa1c9c6c7bfb27898e48d0acc64`
