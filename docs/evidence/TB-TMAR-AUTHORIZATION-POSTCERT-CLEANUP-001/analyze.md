# TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001 — Analyze

Skills: `tooba-architecture-analyze` → `tooba-architecture-migrate` → `tooba-architecture-certify`.

## Context

`HEAD = 7d8ea21155109def56866eee2acdab2067fb457b`. Root Global Boundaries R3 is final certified.
Authorization evacuation remains accepted. Two residual debts remain, both inside Authorization only.

## DEBT A — Host readiness consumes AccessControl Infrastructure types

Current Host readiness composition:

| File | Consumed type | Namespace |
| ---- | ------------- | --------- |
| `src/backend/Host/Tooba.Host/Health/HostReadinessEvaluator.cs` | `SpiceDbAuthorizationOptions`, `SpiceDbHealthProbe` | `Tooba.AccessControl.Infrastructure.Authorization` |
| `src/backend/Host/Tooba.Host/Health/HostHealthEndpoints.cs` | `IOptions<SpiceDbAuthorizationOptions>` | `Tooba.AccessControl.Infrastructure.Authorization` |
| `src/backend/Host/Tooba.Host/Program.cs` | `using Tooba.AccessControl.Infrastructure.Authorization;` | composition import only |

This is tolerated evacuation composition debt, not the final canonical boundary. Host should depend
only on a narrow contract, ideally in the now-existing `Tooba.AccessControl.Contracts`.

`SpiceDbHealthProbe` is a DI singleton registered by `AuthorizationRegistration`; the current
evaluator resolves it with `services.GetService<SpiceDbHealthProbe>()` and probes only when
`ReadinessProbeEnabled`.

## DEBT B — `AppliedVersion` does not mean "successfully applied"

`ConfiguredAuthorizationSchemaBootstrapper.BootstrapIfConfiguredAsync` sets

```csharp
_appliedVersion = _schema.SchemaVersion;
```

**before** the real `SpiceDbAuthorizationAdapter.WriteSchemaAsync(...)` call. Therefore:

- when `Mode != "SpiceDb"` (or no services) the version is marked applied although nothing was written;
- when `WriteSchemaAsync` throws, the version stays non-null although nothing was applied.

Required semantic: `AppliedVersion` becomes non-null **only** after a successful actual write.

## Scope confirmation

In scope: the narrow readiness seam, the adapter, the bootstrapper semantic, focused guards/tests,
evidence, SoT.

Out of scope (explicitly forbidden by the task): authorization permission model redesign,
schema version changes, route changes, AccessControl role/capability behavior changes,
SpiceDB retry redesign, Host folder migrations, frontend, DB schema/migrations.
