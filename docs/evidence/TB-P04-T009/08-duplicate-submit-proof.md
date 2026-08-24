# TB-P04-T009 — Duplicate submit proof

Expected: two `POST /v1/storefront/checkout` calls with the same guest cart and the same `IdempotencyKey` return one `CheckoutId`.

Covered by existing Order foundation tests (same key replay, different key same cart, concurrent unique CartId). Host submit uses `ICheckoutDirectory.SubmitAsync` without a second checkout model.

UI keeps `sessionStorage` idempotency key `tooba.storefront.checkoutIdempotency` so double-click reuses the same key.
