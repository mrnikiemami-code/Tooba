# Storefront Checkout Plan (P10)

```text
Phase: P10 — Storefront Checkout Journey
UI contract: Shopeiva structure locked (Tooba accent #2563EB on cart; shipping preserves Shopeiva red hero)
Price / stock / quantity: backend-authoritative
USER_VISUAL_ACCEPTED=NO until user accepts
```

## T001 — Cart foundation (TECHNICALLY COMPLETE)

Connected Shopeiva cart UI to real Host Cart APIs (ATC, toast, badge, mini-cart, `/cart`, recommendations). Architect-accepted.

## T002 — Shipping (THIS TASK — IMPLEMENTED)

Reuse Shopeiva `/shipping` UI with real Tooba data:

- Cart → `/shipping` handoff with authoritative cart
- Saved address selection + new address (auth AddressBook; guest inline)
- Recipient prefills from address
- Shipping methods from Store-enabled ShippingService catalog (no template carriers)
- Multi-seller eligibility via max seller readiness (no Admin concepts)
- Shipping price backend-authoritative
- Minimum delivery = max(seller prep) + method lead; later OK, earlier rejected
- Delivery date/time UI preserved; order notes persisted
- State via `cart_shipping_drafts`; Continue → `/payment` template handoff only
- Do **not** implement Payment methods (T003)

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

## Non-goals for T002

- No Payment method implementation / no TB-P10-T003
- No redesign of Shopeiva Shipping or Cart
- No Consolidated Package Admin concepts on storefront
- No second toast system
