# TB-P10-T004-R13 — Payment page ownership

`/fa/payment?checkoutId=` → `loadStorefrontCheckout(checkoutId)` → `resolveCommittedCheckoutAccess` → GET `/v1/storefront/checkout/{id}?cartId=<source converted>`.

Host `GetAsync`:

1. `GetOwnedForPaymentResultAsync` — auth session **or** guest secret on **snapshot.CartId** via `TryGetForOwnershipAsync` (invalid secret → null, not throw).
2. If checkout exists for actor but not owned → `checkout.access.denied` (403), never `checkout.rejected`.
3. Request `cartId` is compatibility only (`_ = cartId`); current Active Cart is not ownership.

After rotation, Payment page still loads with committed proof. New Active Cart secret cannot load the old checkout.
