# TB-P10-T004-R22-R1 — Auth Discovery

Reused (no second auth stack):

- Host `POST /v1/auth/otp-login/request` + `/complete` on existing Identity OTP + session ticket
- Host `POST /v1/auth/login|register|logout` remain for internal/password; not shown on storefront Login
- BFF `POST /api/auth/otp-request` + `/otp-complete` set the same HttpOnly `tooba_session` cookies as password login
- Middleware injects `Authorization: Bearer` from `tooba_session` for `/v1/*`
- Identity `IOtpChallengeService` + `EstablishSessionForUserAsync`
- Customer identity = Identity `User` + Phone identifier (passwordless). No CustomerId in Identity
- Cart: guest secret + optional `OwnerUserId` after adopt/merge
- Logout: Host revoke + BFF cookie clear + `clearCartSession()`
- Locale: `/fa/login` `/en/login` via existing public prefix `/login`

| Surface | Anonymous | Auth required (AuthenticatedOnly) |
| --- | --- | --- |
| Home / PLP / PDP / ATC / Cart | yes | no |
| Shipping / Checkout / Payment / submit | no | yes (backend `checkout.authentication_required`) |
