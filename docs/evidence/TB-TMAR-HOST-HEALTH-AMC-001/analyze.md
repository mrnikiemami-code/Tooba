# analyze — TB-TMAR-HOST-HEALTH-AMC-001

## Mode

ANALYSIS_ONLY — production change ZERO.

## Exact tree

```text
src/backend/Host/Tooba.Host/Health/
  HostHealthEndpoints.cs
  HostReadinessEvaluator.cs
```

| Metric | Value |
|---|---|
| Production `.cs` count | 2 |
| Production types | 2 (+ nested `Evaluation` record) |
| Namespace (current) | `Tooba.Host` |
| Path-derived namespace | `Tooba.Host.Health` |
| Path↔namespace | **VIOLATION** |
| Routes | 4 |

## Types

| Type | Visibility | Role |
|---|---|---|
| `HostHealthEndpoints` | internal static | Map live/ready routes; compose readiness JSON |
| `HostReadinessEvaluator` | internal static | Evaluate readiness checks |
| `HostReadinessEvaluator.Evaluation` | nested sealed record | `(Ready, Checks)` |

## Verdict (summary)

- Host retains liveness/readiness platform endpoints (KEEP).
- Path↔namespace MUST become `Tooba.Host.Health`.
- `IServiceProvider.GetService<IBusControl>()` is genuine optional service-locator debt → inject `IBusControl?` or narrow Host messaging readiness seam.
- Readiness is **CONFIGURED** presence (not DB connectivity); naming can mislead operators.
- Disclosure debt: `missing-reference:{reference}` and `messaging-schema` expose config-internal topology to unauthenticated callers.
- AccessControl boundary CURRENT Contracts-only via `IAuthorizationReadinessProbe`.
- Recommended: **W1** (namespace + DI hygiene + disclosure sanitize + optional messaging seam) → **W2-CERT**.
- Protected: MultiTenancy/Errors/Security/Admin CERT **PRESERVED**.
- Implementation SHA unchanged: `cfbc94d258de837fdc018ddb29db68233cc25783`
