# RECEIPTS — TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R4

## Identity

- Task: `TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R4`
- Bridge row: `aa4724a4-2db9-477f-9b86-0ce5a94e5c11`
- Base-SHA: `0bb72adf972097eb50d846b43ded1e50f4b14863`
- ClaimedAtUtc: `2026-11-03T02:54:16.0764878Z`
- Channel: `tooba-main` / Worker: `tooba-worker-01`

## Tests

| Suite | Result |
|-------|--------|
| Shipping Carriers unit + classification (excl. live PG Case* re-run after cleanup) | 49 passed |
| OrderCommittedCarrierShipmentFactsEligibilityTests | 2 passed |
| Disposable PG Category=TB249R4 (Order+Shipping+Fulfillment migrate + cases 1–10 + isolation) | **17 passed** |
| FE order-group-carrier-execution.test.ts | 8 passed |
| FE tsc / production build | 0 / 0 |
| Host Release publish | 0 |

## Disposable target

- Container: `tb249r4-pg` id `0cb34044db41` (postgres:16-alpine)
- Bind: `127.0.0.1:15433` → 5432
- DB/user: `shipping_tb249r4` / `tb249r4`
- Env: `TOOBA_TB249R4_POSTGRES`
- Secret file (private, not git): `D:\temp\tb249r4-pg.secret`
- Migrations: OrderDbContext + Shipping IMigrator + FulfillmentDbContext MigrateAsync on fresh DB
- Cleanup: `docker stop/rm tb249r4-pg`; `Test-NetConnection 15433` → closed; `docker ps -a --filter name=tb249r4-pg` empty

## Relational case ledger

| Case | Result |
|------|--------|
| 1 accepted→package+sender→ready→exact execution; financial immutable | PASS |
| 2 unpacked/exhausted/cancelled deny; foreign owner replay deny | PASS |
| 3 two packages same group; distinct qty/value; provider once each | PASS |
| 4 concurrent same-intent one outbound; competing manual/carrier no overship | PASS |
| 5 separate DbContext sees full pending snapshot before HTTP | PASS |
| 6 fail after intent/before alloc; after alloc/before link; before provider — recover same package | PASS |
| 7 timeout→UNKNOWN; replay no second create; track reconciles | PASS |
| 8 wrong ids / mismatched qty fail closed; no latest fallback | PASS |
| 9 handler+validator authenticated actor into fulfillment | PASS |
| 10 unauthorized/disabled → zero external calls | PASS |

## Runtime (post-redeploy)

- Host PID **28492** :5088 `/health` 200 — Edition=SingleStore TenantId=store-alpha
- FE PID **30456** :3000 `/fa` 200
- Authorized Host login `admin-actor@tooba.local` + order GET 200 + capabilities 200 (Manual / payment_locked / canCreate=false)
- DataProtection key preserved (`key-d82e31a6-…`)

## Migrations

- Canonical: no new additive migration in R4 (behavior/contract only)
- Disposable: full Order+Shipping+Fulfillment chain applied then discarded with container

## Code-SHA / Evidence-SHA

- Code-SHA: 1022c0b0380f4e7d890b37b0c104494c71d8e8d1
- Evidence-SHA: 7d664cbe350b31d922defbe7221393970c62ce74
