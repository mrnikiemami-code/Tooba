# TB-P09-T022-R1 — FE Admin HTML 500 fix

## Root cause

Admin HTML **500** was caused by the **FE Next process being unavailable or mid-restart** (`ECONNRESET` while proxying). It was **not** a Consolidated Package SSR/DTO defect.

`TOOBA_HOST_ORIGIN=http://127.0.0.1:5088` was already correct.

## Fix applied

Operational / readiness fix only:

1. Ensure FE (`http://127.0.0.1:3000`) is up and Next reports **Ready** before Admin HTML smoke.
2. Re-hit Admin Order Detail routes after Ready → **HTTP 200**.
3. Complete real browser visual smoke (cursor browser) on known fixtures.

No SSR disable, no catch-all swallow, no mock package projection, no domain redesign.

## Small FE polish (working tree)

Shipment cards / package dialog no longer prefer raw `shipmentId.slice(0, 8)` labels; prefer human `trackingReference` when present (in progress in working tree). Not claimed as the 500 root-cause fix.

## Verification

- Admin single + multi Order Detail: HTTP 200 after Ready — `r1-admin-runtime.md`
- Browser smoke — `r1-visual-smoke.md`
- Regression HTTP smoke script — `_r1_http_smoke.mjs` (see `r1-focused-validation.md`)
