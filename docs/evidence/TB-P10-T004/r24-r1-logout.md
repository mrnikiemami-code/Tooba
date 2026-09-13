# TB-P10-T004-R24-R1 — Logout

Logout calls canonical `POST /api/auth/logout` with CSRF (`ensureCsrfCookie` + `bffFetchHeaders`).

Then `clearCartSession()` (drops cartId/guestSecret pointer; does not delete Orders), `notifyCartChanged()`, `notifyAuthChanged()`.

Authenticated cart remains account-owned on the server. Next anonymous browser does not inherit that cartId.

Orders stay; customer-orders require login again.
