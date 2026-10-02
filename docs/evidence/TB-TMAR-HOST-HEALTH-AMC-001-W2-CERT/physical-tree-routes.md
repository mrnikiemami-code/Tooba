# physical-tree-routes — TB-TMAR-HOST-HEALTH-AMC-001-W2-CERT

## Tree

```text
src/backend/Host/Tooba.Host/Health/
  HostHealthEndpoints.cs
  HostReadinessEvaluator.cs
```

No subfolders. No duplicate copies. No alias/shim/TypeForwardedTo.

## Routes

| Route | Behavior |
|---|---|
| `GET /health/live` | liveness `{status:ok}` + CORS when enabled |
| `GET /health` | liveness alias |
| `GET /health/ready` | readiness evaluator |
| `GET /ready` | readiness alias |

`HostHealthEndpoints.Map` registered exactly once in `Program.cs`.
MultiTenancy SkipPrefixes retain `/health` and `/ready`.
SessionAuthenticationMiddleware skips `/health` and `/ready`.
