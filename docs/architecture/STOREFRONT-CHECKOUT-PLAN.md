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

## T003 — Payment (TECHNICALLY COMPLETE / Architect-accepted via T003-R1)

Reuse existing `/payment` UI:

- Remove/omit template-only card-number / CVV / expiry forms
- Load only Store-enabled payment methods from Tooba registry (gateway gated; manual when enabled; wallet quote-gated)
- Order summary + final payable backend-authoritative (includes shipping)
- Initiate Payment attempt on existing checkout (Order already created at shipping commit)
- Manual + Sandbox gateway paths; no fake Production PSP
- StoreShipping payment allocation (T003-R1) — no first-seller shortcut

## T004 — Checkout E2E hardening (IMPLEMENTED — THIS TASK)

Cart → Shipping → Payment → Order final technical gate:

- Guest E2E + multi-seller StoreShipping proven
- Confirmation payment picker Host-gated (`hostEnabledCodes`)
- Price/inventory/shipping invalidation + idempotency + security matrix A–O
- FA/EN shell smoke; visual smoke without marking USER_VISUAL_ACCEPTED
- Focused Host/FE suites green

## T004-R2 — Payment completion repair (IMPLEMENTED)

P10 final payment-completion repair (not T005):

- Sandbox/Development simulator page with Success/Failure actions; Host Verify is truth
- Manual/card-to-card customer tracking number + configurable proof upload
- Manual submit stays Pending until Admin confirm/reject
- Converted cart presents empty lines so completed checkout cannot reuse cart for a second Order
- Order result with human order number + مشاهده سفارش

## T004-R3 — Payment completion runtime proof (IMPLEMENTED)

Runtime A–J proof of R2 payment completion. Minimal defect: checkout GetAsync must not treat Converted empty cart as checkout.cart.empty so payment initiate/result still work. Not T005.

## T004-R4 — Payment result ownership + polling repair (IMPLEMENTED — THIS TASK)

Stop 401 loop after Cart finalization: Payment/Order ownership independent of mutable active Cart; state-aware result polling (no rapid-poll on AwaitingAdmin/terminal); narrow guest committed proof survives Cart clear. Not T005.

## Non-goals for T004

- No TB-P10-T005
- No redesign of Shopeiva checkout layout
- No Consolidated Package work
- No inventing COD or new provider types
- No real external PSP
