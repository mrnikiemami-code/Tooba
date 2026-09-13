# TB-P10-T004-R22-R1 — Identity Policy

`StoreCheckoutIdentitySettings` singleton. Default `AuthenticatedOnly`. Admin can set `GuestAllowed`.

`GET /v1/storefront/checkout-identity-policy` → `{ policy, cartAnonymousAllowed: true, checkoutAuthenticationRequired }`.

Missing row ⇒ AuthenticatedOnly (`CheckoutIdentityGate.GetEffectiveAsync`).

Runtime: default AuthenticatedOnly; Admin PUT GuestAllowed then restore AuthenticatedOnly.
