# RECEIPTS — TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R5

## Identity

- Task: `TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R5`
- Bridge row: `b2186e54-02d2-48be-bbdc-220866c9eabb`
- Base-SHA: `a9af3f400e968113c5f09a7edee1c70f2a51f1a6`
- ClaimedAtUtc: `2026-11-03T03:47:03.8829099Z`
- Channel: `tooba-main` / Worker: `tooba-worker-01`

## Tests

| Suite | Result |
|-------|--------|
| Shipping Carriers (+ TB249R5, excl. live TB249R4 after cleanup) | 49 passed |
| Order CatalogPhysicalSpecification + eligibility | 12 passed |
| Disposable PG Category=TB249R5 | **15 passed** |
| FE order-group-carrier-execution.test.ts | 11 passed |
| FE tsc | 0 |
| Host publish / FE production build | 0 / 0 |

## Disposable TB249R5 case ledger

| Case | Result | Notes |
|------|--------|-------|
| isolation (3) | PASS | fail-closed parse |
| a concurrent same dispatch | PASS | startBarrier=2; providerCreates=1; shipments=1 |
| b provider accept → cancel → UNKNOWN | PASS | Creates=1; replay no resend |
| c after alloc before Shipping link | PASS | AllocateAttempts=2; shipment rows=1 |
| d mixed 100/900 packages | PASS | declared 100+900; collect residual exact; own dims |
| e rotation/disable/config | PASS | no silent substitution |
| f Catalog PG + validator | PASS | exact g/cm |
| g legacy/override + Order immutable | PASS | accepted quote weight unchanged after Catalog edit |

- Container: `tb249r5-pg` postgres:16-alpine `127.0.0.1:15433`
- DB/user: `shipping_tb249r5` / `tb249r5`
- Secret: `D:\temp\tb249r5-pg.secret` (private)
- Cleanup: stop/rm; port 15433 closed

## Canonical migrations

Private backup: `D:\temp\tb249r5-backup\tooba_alpha-pre-tb249r5.dump`  
Applied on store-alpha/tooba_alpha (pending=0 readback):

- Catalog `20261103120000_AddProductPhysicalSpecification`
- Shipping `20261103120000_CarrierShipmentDispatchClaim`
- Fulfillment `20261103120000_ShipmentShippingDispatchKey`

## Runtime

- Host PID **26352** :5088 `/health` 200 — SingleStore/store-alpha/tooba_alpha
- FE PID **32348** :3000 `/fa` 200
- DataProtection `key-d82e31a6-…` preserved
- Order `01a185a2-…` 200 Manual PendingPayment; capabilities Manual / payment_locked / canCreate=false
- BFF product workspace physical columns present (NULL)

## Code-SHA / Evidence-SHA

- Code-SHA: 23f6dbd7a495159510aa3a9eede28b57b3bb4ae6
- Evidence-SHA: 2fb27f744bd4c57f8920998fb8695803964e011e
