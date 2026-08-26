# 10 — SpiceDB readiness probe (TB-P06-T005)

## Component

`SpiceDbHealthProbe` — singleton registered in `AuthorizationRegistration`.

## Probe method

gRPC `SchemaService.ReadSchema` with short deadline (min 3s, capped by `TimeoutSeconds`).

## When active

| Condition | Probe runs? |
|---|---|
| `Mode != SpiceDb` | No-op (`CheckAsync` returns true) |
| `ReadinessProbeEnabled=false` | Skipped (returns true) |
| `Mode=SpiceDb` + enabled | Live ReadSchema |

## Integration

`HostReadinessEvaluator.EvaluateAsync` resolves probe from DI:

- Success → `checks["authorization"] = "spicedb"` (via mode label after probe pass)
- Failure → `authorization=spicedb-unreachable`, readiness **503**

## Liveness unaffected

`/health/live` never calls SpiceDB.

## Tests

| Test | Result |
|---|---|
| `Readiness_probe_succeeds_when_spicedb_is_up` | true |
| `Readiness_probe_fails_when_spicedb_is_stopped` | false |

## Rationale

Lightweight connectivity check without full permission matrix scan or expensive tuple enumeration.
