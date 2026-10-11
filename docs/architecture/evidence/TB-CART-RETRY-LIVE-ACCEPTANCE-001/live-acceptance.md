# TB-CART-RETRY-LIVE-ACCEPTANCE-001 — Live acceptance of checkout-scoped reservation retry

- **Parent task:** TOOBA-CART-EXPIRED-RESERVATION-RETRY-FIX (commit `1ae919f2c`)
- **Protocol:** BRIDGE-WAKE-V1 / channel `tooba-main`
- **Verdict:** `LIVE_PASS`
- **Baseline:** `HEAD == origin/main == 1ae919f2cee5cdf326571813936121edd3b61be1`, working tree clean under `src/`
- **Production code changed:** `ZERO` (verification-only task)

## 1. Preflight

- `git status --porcelain -- src` → empty; `git rev-parse HEAD == git rev-parse origin/main`.
- Read: `AGENTS.md`, `.cursor/skills/tooba-frontend-feature/SKILL.md`,
  `.cursor/skills/tooba-backend-feature/SKILL.md`,
  `docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md` contract surface,
  `src/backend/Modules/Order/Tooba.Order.Endpoints/Storefront/StorefrontOrderEndpoints.cs`,
  `src/backend/Host/Tooba.Host/Security/Checkout/CheckoutIdentityGate.cs`,
  `src/backend/Host/Tooba.Host/Order/HostOrderStorefrontActor.cs`,
  `src/frontend/app/storefront/storefront-pending-payments.tsx`,
  `src/frontend/app/storefront/storefront-pending-payment-api.ts`,
  `src/frontend/app/login/storefront-login.tsx`,
  `src/frontend/app/api/auth/*`, `src/frontend/lib/auth/*`.
- Bridge health `GET /health` → `{"status":"ok"}`.

## 2. Fixture and session (no bypass, no impersonation)

- No unpaid/expired checkout existed in the default tenant, so the approved
  **Development fixed-OTP login fixture** was used
  (`DevelopmentOtpLoginFixtureOptions`, auto-enabled when the Host environment is
  `Development`; identifier `09***111111` / Development code — see source, not
  reproduced here).
- Sign-in was performed through the real storefront UI at `/fa/login`, then
  `otp-request` → `otp-complete` (both `HTTP 200`), producing a legitimate
  customer session. No Dev-actor header, no identity-gate bypass, no session
  forgery, no `paymentId` fabricated anywhere.

## 3. Preconditions confirmed from server truth BEFORE the click (read-only)

`POST /v1/storefront/pending-payments` (authenticated), `serverTime 2026-10-11T17:50:50Z`:

```text
checkoutId              = 01a09b8b-3e28-7000-94ed-e37517c5cbf5
cartId                  = 01a09b33-819c-7000-a3b6-2479c63c653f
orderReference          = TB-20260913161309-01-bc0f42 / TB-20260913161309-02-7fcb8d
paymentId               = null
primaryAction           = retryAfterExpiry
paymentPresentation     = expiredRetryable
canInitiatePayment      = false
canRetryPayment         = true
reservationPresentation = ended
secondsRemaining        = 0
holdEndsAt              = 2026-09-13T16:14:09Z   (expired)
cycleNumber             = 1
maxCycles               = 2
retryCountRemaining     = 1
hasReachedRetryLimit    = false
```

The cart UI rendered the pending-payment card «در انتظار پرداخت» with
«مهلت رزرو موجودی پایان یافته است.» and an **enabled** CTA
`[data-testid="pending-payment-retry"]` labeled «بررسی موجودی و پرداخت مجدد».

## 4. The single real browser click and its wire evidence

A scripted in-page fetch recorder was installed before the click, then the
existing CTA was clicked **exactly once** in the real browser.

Recorded network activity (sanitized):

```text
POST /v1/storefront/checkout/01a09b8b-3e28-7000-94ed-e37517c5cbf5/retry-reservation?cartId=01a09b33-819c-7000-a3b6-2479c63c653f
  → HTTP 200  (no silent return)
POST /v1/storefront/pending-payments
  → HTTP 200  (capability refresh)
```

- The request path is exactly the Order-owned, checkout-scoped retry endpoint
  with `paymentId = null`; nothing was silently swallowed.
- `checkoutId` in the request equals the pre-click `checkoutId` (same identity).

## 5. Server truth AFTER the click (read-only)

`POST /v1/storefront/pending-payments`, `serverTime 2026-10-11T17:52:46Z`:

```text
checkoutId              = 01a09b8b-3e28-7000-94ed-e37517c5cbf5   (unchanged)
cartId                  = 01a09b33-819c-7000-a3b6-2479c63c653f   (unchanged)
orderReference          = TB-20260913161309-01-bc0f42 / TB-20260913161309-02-7fcb8d (unchanged)
paymentId               = null                                    (still not fabricated)
primaryAction           = pay               (was retryAfterExpiry)
paymentPresentation     = awaitingPayment   (was expiredRetryable)
canInitiatePayment      = true              (was false)
canRetryPayment         = false             (was true)
reservationPresentation = held              (was ended)
secondsRemaining        = 46                (was 0)
holdEndsAt              = 2026-10-11T17:53:32Z   (new, future hold)
cycleNumber             = 2                 (was 1)
maxCycles               = 2
retryCountRemaining     = 0                 (was 1)
```

UI after the click (localized FA, unchanged component ownership):

```text
«مهلت رزرو مجدد 00:52»  +  enabled [data-testid="pending-payment-pay"] («پرداخت»)
expired-hold message «مهلت رزرو موجودی پایان یافته است.» removed
```

## 6. Duplicate / fabrication check (read-only SQL, tenant `tooba_alpha`)

```text
order.checkouts WHERE cart_id = 01a09b33-…            → 1   (no duplicate checkout)
payment.payments WHERE checkout_id = 01a09b8b-…       → 0   (no payment row fabricated)
order.seller_orders WHERE checkout_id = 01a09b8b-…    → 2   (unchanged)
order.checkout_reservation_commits WHERE checkout_id  → 1   (unchanged)
total rows in order.checkouts                         → 8   (unchanged, no new row)
```

No duplicate checkout, payment, seller-order or reservation-commit was created;
the retry used the same checkout identity and reacquired a fresh authorized hold.

## 7. Negative checks (fail closed, stable codes)

```text
anonymous  POST /v1/storefront/checkout/…/retry-reservation   → HTTP 401  checkout.authentication_required
authorized POST on an unrelated Paid checkout                 → HTTP 404  payment.missing
```

Both fail closed with stable machine codes and no silent success.
Retry-limit (`inventory.reservation.retry_limit_reached`) and
supply-unavailable (`payment.unpaid.supply_unavailable`) paths are covered by the
focused behavioral tests below.

## 8. Focused tests actually run

```text
dotnet test src/backend/Modules/Order/Tooba.Order.Tests \
  --filter "FullyQualifiedName~StorefrontPendingReservationRetry"
→ Passed! Failed: 0, Passed: 9, Skipped: 0, Total: 9  (Tooba.Order.Tests, net8.0, 205 ms)
```

`npm run test:critical-storefront` was **not** required and not run: no frontend
production file was modified in this task (`git status --porcelain -- src` empty).

## 9. Explicitly NOT tested (bounded stop)

- **Final payment initiation was deliberately not triggered** (the enabled
  `pending-payment-pay` control was observed but not clicked) to avoid creating a
  real charge/payment intent. Payment navigation was followed only up to the
  actionable pay state.
- No real money movement, no charge, no paid order was created.

## 10. Remaining risk

- Only the Development tenant fixture was exercised (`tooba_alpha`, session user
  `01a0…8d7c`). Other tenants/locales were not exercised live; multilingual
  message resolution beyond FA was not re-verified in this task.
- The observed retry hold countdown was short (~1 minute in this Development
  configuration); production hold duration is configuration-driven and was not
  re-measured.

## 11. Stop

`USER_REVIEW_AFTER_LIVE_ACCEPTANCE` — no automatic follow-on task.
