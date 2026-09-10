# Storefront Checkout Plan (P10)

```text
Phase: P10 — Storefront Checkout Journey
UI contract: Shopeiva structure locked (Tooba accent #2563EB on cart; shipping/payment preserve Shopeiva red hero)
Price / stock / quantity: backend-authoritative
USER_VISUAL_ACCEPTED=NO until user accepts
```

## T001 — Cart foundation (TECHNICALLY COMPLETE / Architect-accepted)

Connected Shopeiva cart UI to real Host Cart APIs (ATC, toast, badge, mini-cart, `/cart`, recommendations).

## T002 — Shipping (TECHNICALLY COMPLETE / Architect-accepted via T002-R1)

Reuse Shopeiva `/shipping` UI with real Tooba data:

- Cart → `/shipping` handoff with authoritative cart
- Saved address selection + new address (auth AddressBook; guest inline)
- Recipient prefills from address
- Shipping methods from Store-enabled ShippingService catalog
- Multi-seller eligibility via max seller readiness (runtime Scenario E proven in R1)
- Shipping price backend-authoritative
- Minimum delivery = max(seller prep) + method lead; later OK, earlier rejected
- Continue → `/payment` with checkoutId

## T003 — Payment (IMPLEMENTED — THIS TASK)

Reuse existing `/payment` UI:

- Remove/omit template-only card-number / CVV / expiry forms
- Load only Store-enabled payment methods from Tooba registry (gateway gated; manual when enabled; wallet quote-gated)
- Order summary + final payable backend-authoritative (includes shipping)
- Initiate Payment attempt on existing checkout (Order already created at shipping commit)
- Manual + Sandbox gateway paths; no fake Production PSP

## T004 — Checkout E2E hardening (NOT THIS TASK)

Cart → Shipping → Payment → successful Order:

- Guest/auth paths
- Pricing/inventory concurrency
- Sold-out / price-changed handling
- Refresh / back / retry / idempotency hardening
- Multilingual RTL/LTR + mobile/desktop
- Visual regression vs Shopeiva

## Non-goals for T003

- No TB-P10-T004
- No redesign of Shopeiva Payment layout
- No inventing COD or new provider types
- No collecting raw bank-card credentials in Tooba UI
