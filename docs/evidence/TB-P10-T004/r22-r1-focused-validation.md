# TB-P10-T004-R22-R1 — Focused Validation

Host: `CheckoutIdentityContractTests` + `Development_otp_login_fixture` — 6 passed (after CartAccess assertion).

FE: login-return-to (3), storefront-login.guard (1), middleware-locale including /login and /v1 (5) — 9 passed.

`npm run test:critical-storefront` — home/PDP/listing/category-plp passed (16).

`node --test docs/ai/recovery-staleness.guard.test.mjs` — 4 passed.

`git diff --check` — clean (CRLF warnings only).

No unrelated full suites. No TB-P10-T005.
