# R16 customer pending-payment discovery

## Current Cart / Shopeiva surface

- Route: `src/frontend/app/cart/page.tsx` → `StorefrontShopeivaCart`
- Cart UI: `src/frontend/app/storefront/storefront-cart.tsx`
- Mini-cart: `storefront-mini-cart.tsx` (Active Cart only; not redesigned)
- Empty Active Cart was the only body: «سبد خرید شما خالی است»
- No pending-payment section existed

## Fit (no Cart redesign)

- Insert `در انتظار پرداخت` after `CartHero`, before Active Cart lines / empty state
- Keep Shopeiva cards, `#2563EB`, rounded-2xl
- When Active Cart is empty and pending Orders exist, empty copy becomes «سبد فعال شما خالی است» so the pending section is the primary action

## Data sources before R16

- R15 `IReservationCycleDirectory.GetProjectionAsync` was in-process only
- Checkout GET is one-id + committed proof
- Payment retry: `POST /payments/{id}/unpaid-retry` → `EnsureRetryAfterExpiryAsync`
- Guest ownership: `tooba.storefront.committedCheckoutProofs` (R13), not Active Cart
- Customer panel lists orders with per-order payment reads (N+1; not used for Cart)

## New block

- Host `POST /v1/storefront/pending-payments`
- Auth / Dev-actor: session-owned checkouts
- Guest: only submitted committed proofs; no enumeration
- Product supports multiple guest pending Orders via the proof map (not first-order-only)
