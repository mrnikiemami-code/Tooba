# RECEIPTS — TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R1

## Identity
- Task: `TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R1`
- Bridge row: `a80c33bf-7ecb-4dcc-9ded-c3a38652abb8`
- Expected-Base-SHA: `1c9bd5d2905bb0a7cfc769efabe94b054caa9a07`
- Channel: `tooba-main` / Worker: `tooba-worker-01`
- ClaimedAtUtc: `2026-11-08T04:11:20.9968838`

## Tests
| Suite | Result |
|-------|--------|
| MarketplaceOperationalDeliveryGroupAuthorityTests | **13 passed** |
| Host Category=TB253R1 MarketplaceOperationalConsolidateRelationalTests | **4 passed** |
| FE admin-order-operational-delivery.mount.test.ts | **4 passed** |
| FE typecheck / production build | **0 / 0** |
| Host Release publish → artifacts/publish-primary | **0** |

## Disposable target
- Container: `tb253r1-pg` (postgres:16-alpine)
- Bind: `127.0.0.1:15433` → 5432
- DB/user: `shipping_tb253r1` / `tb253r1`
- Env: `TOOBA_TB253R1_POSTGRES`
- Secret file (private, not git): `D:\temp\tb253r1-pg.secret`
- Migrations: OrderDbContext + FulfillmentDbContext MigrateAsync on disposable DB
- Cleanup: `docker stop/rm tb253r1-pg` after evidence stamp

## Relational case ledger
| Case | Result |
|------|--------|
| authority-refusals (seller/store/cross-checkout/missing origin/date/hour/currency + client method) | PASS |
| 1 compatible Marketplace → derived method → CreateConsolidatedPackage links both shipments | PASS |
| 2 concurrent twin allocates one Created package | PASS |
| 3 cancel then rebuild without duplicate active | PASS |

## Canonical runtime
- Host PID **38908** :5088 `/health` 200 — Edition=SingleStore DeploymentId=`store-alpha-canonical-tb253r1`
- FE PID **2532** :3000 `/fa` 200
- Actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db` order GET 200 → operationalDeliveryGroups=3 Store, cross=0
- CapturedPreview local ON; checked-in OFF
- DataProtection keys preserved (KEYS_BEFORE=True KEYS_AFTER=True)

## Code-SHA / Evidence-SHA
- Code-SHA: PENDING_COMMIT
- Evidence-SHA: PENDING_EVIDENCE
