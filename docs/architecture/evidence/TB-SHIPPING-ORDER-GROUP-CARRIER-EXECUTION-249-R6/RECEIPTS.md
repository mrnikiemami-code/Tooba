# RECEIPTS — TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R6

## Identity

- Task: `TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R6`
- Bridge row: `3ae4fb24-ae36-4fed-81c9-1342ca8f6145`
- Base-SHA: `481ec58bb91ea85b787236119241c3fbb01e8e89`
- ClaimedAtUtc: `2026-11-03T03:50:00Z` (approx wake claim)
- Channel: `tooba-main` / Worker: `tooba-worker-01`

## Tests

| Suite | Result |
|-------|--------|
| Shipping directory+intent unit | 19 passed (A) |
| CatalogPhysicalSpecificationTests | 10 passed (B) |
| Disposable PG Category=TB249R6 | **23 passed** |
| FE `test:carrier-mount` (source + mount) | **18 passed** |
| FE typecheck / production build (deploy path) | 0 / 0 |

## Disposable TB249R6 ledger

| Case | Result | Counters |
|------|--------|----------|
| identical A/B then exhausted | PASS | Creates=2, allocations=2, qty2, declared conserved; replay no add; third NotEligible |
| barrier same occurrence | PASS | startBarrier=2, Creates=1, shipments=1 |
| conflict + cross-owner + Unknown no-resend | PASS | intent_conflict; NotEligible; Creates unchanged |
| weight regression + isolation | PASS | 500.5+legacy500→null; equal/conflict/inherit; fail-closed parse |

- Container `tb249r6-pg` 127.0.0.1:15433; secret `D:\temp\tb249r6-pg.secret`; cleaned; port closed

## Canonical

- Migrations vs R5: **none**
- Host PID **45600** :5088/health 200
- FE PID **13080** :3000/fa 200
- Order Manual PendingPayment; capabilities Manual/payment_locked/canCreate=false
- DataProtection key-d82e31a6 preserved

## Code-SHA / Evidence-SHA

- Code-SHA: 4150f0414bb732ae3d5e7dbf96876f1f602df258
- Evidence-SHA: EVIDENCE_SHA_PENDING
