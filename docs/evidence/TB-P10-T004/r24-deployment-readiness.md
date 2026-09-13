# TB-P10-T004-R24 — Deployment readiness

R23 migrations remain the abuse schema:

- `20260913140000_AddCheckoutAbuseLimits` — customer lock PK, `checkout_reservation_commits` + unique checkout + `(customer_id, occurred_at)`, block events
- `20260913140000_AddStoreCheckoutAbuseSettings` — defaults 2 / 30 / 3

Deterministic SQL `IF NOT EXISTS`. Lock row strategy is PK insert-or-update inside TX. Old Orders without churn rows are simply absent from the window (no crash). No startup mass mutation. AuthenticatedOnly default remains when settings row missing (`CheckoutIdentityGate`). Dev OTP environment-gated. No new R24 migration required.
