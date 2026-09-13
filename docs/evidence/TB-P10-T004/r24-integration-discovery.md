# TB-P10-T004-R24 — Integration discovery

Anonymous: Browse → AddToCart (`POST /v1/storefront/cart` + lines) → Cart → Shipping/Checkout gated (`401 checkout.authentication_required`) → `/fa/login` OTP → `POST /v1/storefront/cart/merge` (no Reservation/Order/Payment) → Shipping.

Authenticated: Shipping projection/selection → `POST /v1/storefront/shipping/commit` → Host identity gate → R21 `TransactionScope` → `EnsureCanStartInitialReservationAsync` (open-unpaid then churn) → `ReserveCartLinesForOrderAsync` → Cycle #1 → `PrepareInitialCommit` → Order write → Cart Convert → `scope.Complete()` → payment later.

After commit: payment fail/retry same Order (no new churn); expiry → Cycle #2+ (no new churn); cancel frees open slot not churn; hide presentation-only; open-unpaid max 2; churn window 30m / max 3; Admin GET/PUT `/v1/admin/settings/checkout-abuse` + identity + reservation-policy.

No product contradiction found. No TB-P10-T005.
