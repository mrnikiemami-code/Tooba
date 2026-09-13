# R19 security

Guest (`StorefrontCheckoutAccessTests` + R16 composer):

- Committed proof authorizes old pending Order
- Active Cart secret does not list others
- Wrong proof omitted (no enumeration)

Authenticated: `PlacedByUserId` only; no cross-customer.

Admin: reservation policy mutation via AdminPanelAccess.

Seller: GET read-only; PUT 403 `reservation.policy.seller.denied`; `reservation.policy.mutate` absent from Access Control catalog.

No client-forged capabilities: `CanExtendTimer` / seller mutate / pay-again on AwaitingAdmin are server-derived.
