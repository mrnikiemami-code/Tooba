# R3 runtime smoke — TB-P09-T022-R3

Script: `docs/evidence/TB-P09-T022/_r3_runtime_smoke.mjs`

## Host
Host was stopped for build/file-lock safety before focused tests. Smoke script reported Host down / unreachable — acceptable.

## Proof (unit + integration)
Focused filter:

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~PaidOrderReservationLifecycleTests|FullyQualifiedName~InventoryFoundationTests|FullyQualifiedName~WholeOrderCancelUntilDispatchTests" -v q
```

**Result: Passed! Failed: 0, Passed: 22, Skipped: 0, Total: 22**

Includes:
- Domain `CommitForPaidOrder` (clear TTL, idempotent, no resurrect)
- Postgres: cart-style ExpiresAt → commit → ReleaseExpiredHolds releases 0; decimal 1.25 preserved
- Postgres: Released commit throws `inventory.reservation.not_active`
- Inventory foundation + whole-order cancel regression suites

## Promotion + expiry contract
- Payment success: `OrderPaymentBridge.ApplyVerifiedSuccessAsync` → `CommitReservationForPaidOrderAsync`
- Expiry worker: `expires_at IS NOT NULL` — committed paid holds excluded
