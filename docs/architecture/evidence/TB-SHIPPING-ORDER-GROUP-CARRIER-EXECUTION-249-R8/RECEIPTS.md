# RECEIPTS — TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R8

## Identity

- Task: `TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R8`
- Bridge row: `19563ac2-5cff-4972-8e50-222f4ed76022`
- Base-SHA: `480241eff644b300443a40f0a712085ec932cb2a`
- ClaimedAtUtc: `2026-11-03T05:12:12.7962524`
- Channel: `tooba-main` / Worker: `tooba-worker-01`

## Tests

| Suite | Result |
|-------|--------|
| FE `test:carrier-mount` (incl. A–D) | **30 passed** |
| FE typecheck | 0 |
| FE production build | 0 |
| Disposable PG matrix | not required (FE outcome repair; R6/R7 backend retained) |

## Canonical

- Migrations: **none**
- Host PID **18428** :5088/health 200 (retained; FE-only delta)
- FE PID **17876** :3000/fa 200
- Order Manual PendingPayment; capabilities Manual/payment_locked/canCreate=false; mutated=false
- DataProtection key-d82e31a6 preserved

## Code-SHA / Evidence-SHA

- Code-SHA: CODE_SHA_PENDING
- Evidence-SHA: EVIDENCE_SHA_PENDING
