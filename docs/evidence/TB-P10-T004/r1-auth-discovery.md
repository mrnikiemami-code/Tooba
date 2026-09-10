# Auth Discovery — TB-P10-T004-R1

Supported customer auth path (no DevActor login substitute):

1. Host `POST /v1/auth/register` + `POST /v1/auth/login` → Bearer `accessToken` (real UserId).
2. FE BFF `GET /api/auth/csrf` → `tooba_csrf`; `POST /api/auth/login` with `X-Tooba-Csrf` → HttpOnly `tooba_session`.
3. AddressBook `/v1/customer/addresses` requires Bearer; ownership scoped to authenticated UserId.
4. Checkout with saved address sets `PlacedByUserId` to authenticated user; payment actor must match session (fixed in StorefrontPaymentComposer.ResolvePaymentActor).
5. Cart still carries `X-Tooba-Guest-Secret` for guest-origin carts; auth does not replace guest secret via DevActor.

Evidence runtime: `r1-runtime-raw.json` steps auth-register-login + auth-bff-tooba-session.
