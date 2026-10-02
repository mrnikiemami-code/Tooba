# service-locator — TB-TMAR-HOST-HEALTH-AMC-001

## Current

`EvaluateReadinessAsync` / `HostReadinessEvaluator.EvaluateAsync` take `IServiceProvider services` and call:

```csharp
services.GetService<IBusControl>()
```

## Classification

**GENUINE_SERVICE_LOCATOR_DEBT** — not lifetime-required (unlike middleware scoped Assigner). Optional registration is the reason; architecture prefers explicit DI.

## Clean targets (min)

1. Prefer: inject `IBusControl?` (or `IOptions`+optional) via method DI on endpoint handler — null when messaging disabled / not registered.
2. Better long-term: `IMessagingReadinessProbe` owned by Host/Messaging returning safe labels only (no schema dump).

No factory/IServiceProvider retention once optional DI is available.
