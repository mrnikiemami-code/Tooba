# TB-P05-T003 — Shopeiva Customer Panel fidelity and live-binding proof

## Reused Shopeiva structure

- Preserved the gold campaign strip, white store header, search/account controls, customer identity row, right-side desktop navigation, rounded cards, toolbar/tabs, spacing hierarchy, and mobile stacked behavior visible in the accepted P04 visual atlas.
- Preserved the Shopeiva logo and customer-panel information architecture.
- Applied only the allowed Tooba blue token to active navigation, status badges, and primary actions.
- No new admin dashboard, Data Grid, or replacement navigation was introduced.

## Live Host bindings

- `GET /v1/customer/dashboard` returns order counts and recent orders scoped by the authenticated session user.
- `GET /v1/customer/orders` returns checkout-level customer orders, payment state, status, item count, seller count, and snapshot payable amount.
- `GET /v1/customer/orders/{checkoutId}` returns seller sections, line snapshots, seller names, payment/order states, and the checkout shipping snapshot.
- `GET /v1/customer/profile` returns a read-only customer view from the session identity and latest shipping snapshot.
- Production identity is the existing opaque Bearer session boundary. `X-Tooba-Dev-Actor-User-Id` is accepted only in Development/Testing to capture deterministic evidence for the existing seeded storefront guest actor.
- All Order, Catalog, and Party reads are separate Host composition queries. There is no cross-schema SQL join and no frontend business authority.

## Capability gaps kept honest

- No address-book capability exists. The Shopeiva address surface is retained as a shell and explicitly performs no persistence.
- No wishlist, notification, wallet, or gift-card ledger capability exists. Their Shopeiva routes remain visible shells with no localStorage/database fabrication.
- The profile endpoint has no write capability; its fields remain read-only.
- Payment state is presented from persisted seller-order status. Product price and stock are not inferred or authored.

## Responsive evidence

- `01-customer-dashboard-desktop-1440x900.png`
- `02-customer-orders-desktop-1440x900.png`
- `03-customer-order-detail-desktop-1440x900.png`
- `04-customer-dashboard-mobile-390x844.png`
- `05-address-capability-empty-state.png`

Automated CDP checks asserted `document.documentElement.scrollWidth <= innerWidth` at each captured route. Desktop captures are exactly 1440×900 and the mobile capture is exactly 390×844.
