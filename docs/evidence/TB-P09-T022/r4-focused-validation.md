# R4 focused validation — TB-P09-T022-R4

Commands:

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~PaidOrderReservationLifecycleTests|FullyQualifiedName~InventoryFoundationTests|FullyQualifiedName~WholeOrderCancelUntilDispatchTests|FullyQualifiedName~AdminOrderCorrectiveActionsTests" -v q

git diff --check

node --test docs/ai/recovery-staleness.guard.test.mjs
```

## Results

| Check | Result |
| --- | --- |
| Focused Host tests | **Passed: 46 / Failed: 0 / Skipped: 0** (Duration ~27s) |
| `git diff --check` | **PASS** (exit 0) |
| Recovery staleness guard | **PASS** (after SoT bump to R4) |

Filter coverage:

- `PaidOrderReservationLifecycleTests` (paid commit / expiry / consume / not_active)
- `InventoryFoundationTests`
- `WholeOrderCancelUntilDispatchTests`
- `AdminOrderCorrectiveActionsTests` (T020-R2 restore gates + inventory-before-fulfillment rebind assertion)

No product/Inventory design code changed in this repair (evidence + SoT only). Temporary Host HoldTtl=90s used for wall-clock observability then source reverted to 30m.
