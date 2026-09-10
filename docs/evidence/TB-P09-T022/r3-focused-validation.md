# R3 focused validation — TB-P09-T022-R3

Command:

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~PaidOrderReservationLifecycleTests|FullyQualifiedName~InventoryFoundationTests|FullyQualifiedName~WholeOrderCancelUntilDispatchTests" -v q
```

**Passed: 22 / Failed: 0 / Skipped: 0**

See `r3-runtime-smoke.md`.
