# RECEIPTS — TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R7

## Identity

- Task: `TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R7`
- Bridge row: `b963778e-6a91-4448-b933-9268b10e3df7`
- Base-SHA: `76e609065ddc3ac2bd64291117ceaa1a2644bf9a` (R6 final pushed envelope)
- ClaimedAtUtc: `2026-11-03T04:47:31.0554513`
- Channel: `tooba-main` / Worker: `tooba-worker-01`

## Tests

| Suite | Result |
|-------|--------|
| FE `test:carrier-mount` | 24 passed |
| Directory Client_prebound unit | 1 passed |
| Disposable PG Category=TB249R7 | **3 passed** |
| Adjacent R6 barrier/identical/weight on same disposable | 4 passed |
| FE tsc / build / Host publish | 0 / 0 / 0 |

## Disposable

- Container: `tb249r7-pg` postgres:16-alpine `127.0.0.1:15433`
- Env identity (hooks): `TOOBA_TB249R6_POSTGRES` → shipping_tb249r6/tb249r6 (fail-closed R6 target type reused)
- Secret: `D:\temp\tb249r7-pg.secret`
- prebound concurrent: Creates=1 Shipments=1
- legacy no-locator after Succeeded: Creates=2 distinct occurrences
- Cleanup: stop/rm; port 15433 closed

## Canonical

- Migrations: **none new**
- Host PID **18428** :5088/health 200
- FE PID **38896** :3000/fa 200
- Manual + payment_locked + canCreate=false; mutated=false
- DataProtection key-d82e31a6 preserved

## Code-SHA / Evidence-SHA

- Code-SHA: 78fe85d8cff9387e8e6b9b15196fe52071f59d1f
- Evidence-SHA: EVIDENCE_SHA_PENDING
