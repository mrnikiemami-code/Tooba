# Authenticated E2E — TB-P10-T004-R1

Runtime (HTTP + BFF cookies + page smoke; no Playwright):

- Register/login customer A (real session)
- Create saved AddressBook address
- Cart + KG offer `01a03826-9936-7000-b499-ff26a6123a8c`
- Shipping projection/selection/commit with saved address
- Payment initiate `manual` amount == payable; idempotent retry same paymentId
- Customer order GET 200 for owner
- FE `/fa/cart`, `/fa/shipping`, `/fa/payment?checkoutId=…` 200; no CVV/cardNumber fields
- FE `/api/customer/orders/{checkoutId}` 200 with session cookie

CheckoutId (sample): `01a08ca7-75af-7000-a453-e4c2e6e4a51c`; payable `201088`.

Defect fixed: StorefrontPaymentComposer previously hard-coded guest actor → `payment.guest.invalid` on auth-placed checkout; now ResolvePaymentActor uses CurrentAuthenticatedSession.UserId when authenticated.

Raw: `r1-runtime-raw.json` authenticated-e2e ok=true.
