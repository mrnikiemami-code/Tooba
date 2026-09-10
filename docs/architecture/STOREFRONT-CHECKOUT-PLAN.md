# Storefront Checkout Plan (P10)

```text
Phase: P10 — Storefront Checkout Journey
UI contract: Shopeiva structure locked (Tooba accent #2563EB)
Price / stock / quantity: backend-authoritative
USER_VISUAL_ACCEPTED=NO until user accepts
```

## T001 — Cart foundation (THIS TASK)

Connect Shopeiva cart UI to real Host Cart APIs:

- Product-card / PDP Add-to-Cart → Offer-backed Cart lines
- Success toast + temporary card “اضافه شد” state (existing react-toastify)
- Header badge = real `itemCount`
- Mini-cart drawer (overlay, left `max-w-sm`, qty/remove/total, تکمیل خرید → `/cart`)
- Real `/cart` lines/totals; coupon via checkout preview when available
- Cart recommendations from existing live merchandising/home feed
- No fabricated free shipping at cart stage
- Guest secret ownership preserved; no invented merge policy

## T002 — Shipping (NOT THIS TASK)

Reuse existing `/shipping` UI when present:

- Saved address selection + add address
- Recipient/address validation
- Shipping methods from Store/Admin configuration
- Seller/offer availability constraints
- Delivery date/time from seller readiness + method
- Customer may choose a **later** valid slot; never earlier than calculated minimum

Do **not** invent T002 scope from T001.

## T003 — Payment (NOT THIS TASK)

Reuse existing `/payment` UI:

- Remove template-only card-number / CVV / expiry forms
- Load only Store-enabled payment methods from Tooba registry
- Order summary remains backend-authoritative
- No fake payment-provider UI

## T004 — Checkout E2E hardening (NOT THIS TASK)

Cart → Shipping → Payment → successful Order:

- Guest/auth paths
- Pricing/inventory concurrency
- Sold-out / price-changed handling
- Refresh / back / retry / idempotency
- Multilingual RTL/LTR + mobile/desktop
- Visual regression vs Shopeiva

## Non-goals for T001

- No Shipping/Payment implementation
- No new Promotions domain (use existing checkout coupon evaluation)
- No redesign of Shopeiva DOM/layout
- No second toast system
- No TB-P10-T002 invention by Worker
