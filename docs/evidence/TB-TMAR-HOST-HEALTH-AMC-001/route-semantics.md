# route-semantics — TB-TMAR-HOST-HEALTH-AMC-001

## Registration

`Program.cs`: `HostHealthEndpoints.Map(app, enableCors: true);`

| Route | Handler | Auth | Tenant resolve | CORS |
|---|---|---|---|---|
| `GET /health/live` | static `{ status: "ok" }` | none (SessionAuthenticationMiddleware skips `/health`) | skipped (MultiTenancy SkipPrefixes `/health`) | RequireCors when enabled |
| `GET /health` | same liveness alias | none | skipped | RequireCors when enabled |
| `GET /health/ready` | `EvaluateReadinessAsync` | none | skipped | none |
| `GET /ready` | same readiness alias | none | skipped (SkipPrefixes `/ready`) | none |

## Liveness

Process-only. No DB, no bus, no auth probe, no tenant resolution. Both aliases are deliberate backward-compatible endpoints.

## Duplicate routes

No other Host production `MapGet("/health...")` / `MapGet("/ready")` found. Guard: `HostReadinessBoundaryGuardTests` asserts Program maps HostHealthEndpoints.
