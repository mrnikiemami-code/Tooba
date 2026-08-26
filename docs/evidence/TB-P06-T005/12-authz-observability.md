# 12 — Authorization observability (TB-P06-T005)

## Meter

OpenTelemetry meter `Tooba` via `AuthorizationInstrumentation`.

## Metrics

| Metric | Tags | When |
|---|---|---|
| `tooba.authorization.check` | `outcome`, `resource_type`, `permission`, `edition` | Every `CanAsync` |
| `tooba.authorization.check.duration` | same | Latency histogram (ms) |
| `tooba.authorization.infrastructure` | `kind`, `resource_type` | Retry / unavailable / timeout |

## Infrastructure kinds

| `kind` | Source |
|---|---|
| `retry` | Bounded retry attempt |
| `unavailable` | Decision Unavailable |
| `timeout` | gRPC DeadlineExceeded / cancel (via adapter catch) |

## Structured logs

Adapter logs: ResourceType, Permission, Outcome, Edition — **no token, no user id in metric tags**.

## Security audit sink

`IAuthorizationSecurityEventSink` records: `permission_denied`, `relationship_changed`, `relationship_revoked`.

Dev/test: `InMemoryAuthorizationSecurityEventSink`.

## OTLP export

When `Tooba:Observability:OtlpEndpoint` configured.

## Not present

- Per-tenant authorization backlog gauge
- SpiceDB tuple count scan
