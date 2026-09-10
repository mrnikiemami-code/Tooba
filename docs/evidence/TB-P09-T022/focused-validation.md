# Focused validation — TB-P09-T022

## Host

- `ConsolidatedPackageTests` (incl. `Concurrent_create_rejects_double_active_membership`) — PASS
- `CustomerFulfillmentTrackingTests` — PASS

## Frontend (node)

- `admin-order-items-shipping` tests — PASS
- `fulfillment-api` tests — PASS

## Recovery

- `node docs/ai/recovery-staleness.guard.test.mjs` — PASS (3/3) after T022 SoT markers

## Runtime

- Runtime smoke A–J + order-cancel void: **PASS** (`runtime-smoke.md` / `runtime-raw.json`)

Do not run unrelated full repository suites. Do not invent TB-P09-T023.
