# RECEIPTS — TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R3

## Identity

- Task: `TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R3`
- Bridge row: `c7c74001-8dc6-4396-8660-50f4c42a44df`
- Base-SHA: `4ec12a05ee2421782ddd5fb91b4b0ef3c4b3027b`
- ClaimedAtUtc: ~2026-11-02T16:45:42Z
- Channel: `tooba-main` / Worker: `tooba-worker-01`

## Tests

| Suite | Result |
|-------|--------|
| Shipping Carriers + classification | 42 passed |
| OrderCommittedCarrierShipmentFactsEligibilityTests | 2 passed |
| FE decode (`npx tsx --test`) | 4 passed |
| FE tsc / production build | 0 / 0 |
| Host Release publish | 0 |
| Disposable PG §5 matrix | **NOT RUN** (container provisioned then cleaned) |

## Disposable target

- Container: `tb249r3-pg` (postgres:16-alpine)
- Bind: `127.0.0.1:15433` → 5432
- DB/user: `order_shipping_tb249r3` / `tb249r3`
- Secret file (private, not git): `D:\temp\tb249r3-pg.secret`
- Cleanup: `docker stop/rm tb249r3-pg`; port 15433 closed

## Runtime (post-redeploy)

- Host PID **20192** :5088 `/health` 200
- FE PID **39012** :3000 `/fa` 200
- Authorized Host login + order GET 200 + capabilities 200 (Manual / payment_locked / canCreate=false)

## Migrations

- Canonical: no new additive migration in R3
- Disposable: migrations not applied (matrix incomplete)
