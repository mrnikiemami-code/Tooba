# SpiceDB Authorization Operations

## Canonical decision

```text
Authorization engine = SpiceDB (ReBAC)
SDK = Authzed.Net 1.6.0 (Host only)
Modes = Disabled | InMemory (dev/test) | SpiceDb (production)
Production = fail-fast; no InMemory; TLS required
Fail-closed = infrastructure uncertainty never becomes ALLOW
```

## Configuration (`Tooba:Authorization`)

| Key | Purpose |
|---|---|
| `Mode` | `Disabled`, `InMemory`, or `SpiceDb` |
| `ApplySchemaOnStartup` | Opt-in schema write on Host start (default `false` in Production) |
| `SpiceDb:Endpoint` | gRPC endpoint (required when `Mode=SpiceDb`) |
| `SpiceDb:Token` | Bearer preshared key / credential (env-injected in Production) |
| `SpiceDb:UseTls` | Must be `true` in Production |
| `SpiceDb:TimeoutSeconds` | Per-request gRPC deadline (default 5) |
| `SpiceDb:RetryMaxAttempts` | Bounded retry for transient infra errors (default 3) |
| `SpiceDb:RetryBaseDelayMilliseconds` | Linear backoff base (default 100) |
| `SpiceDb:ConsistencyMode` | `FullyConsistent` (default) or `MinimizeLatency` |
| `SpiceDb:ReadinessProbeEnabled` | gRPC `ReadSchema` probe in `/health/ready` |

## Topology

- **Host** owns SpiceDB adapter, schema bootstrap, health probe, and use-case guards.
- **Modules** write tuples via `IAuthorizationTupleWriter` / outbox projections only.
- **Domain/Application** must not reference Authzed.Net.

## Schema governance

- Versioned schema: `src/backend/Host/Tooba.Host/authorization-foundation.zed` (v2 foundation).
- Code mirror: `FoundationAuthorizationSchemaProvider`.
- Production apply: explicit ops process or controlled `ApplySchemaOnStartup=true`.
- Rollback: SpiceDB schema changes are forward-only; plan compensating schema writes.

## Fail-closed behavior

| Condition | Check result | Write result |
|---|---|---|
| SpiceDB unreachable | `Unavailable` | `authorization.unavailable` |
| Timeout | `Unavailable` (`spicedb.timeout`) | throws |
| Missing relationship | `Deny` | n/a |
| Mode=Disabled | `Unavailable` | throws |

## Health / readiness

- **Liveness** (`/health/live`): independent of SpiceDB.
- **Readiness** (`/health/ready`): when `Mode=SpiceDb`, config validated + optional gRPC probe.
- Probe disabled: set `ReadinessProbeEnabled=false` (config-only readiness).

## Observability

- Metrics: `tooba.authorization.check`, `tooba.authorization.check.duration`, `tooba.authorization.infrastructure`.
- Outcomes: `allow`, `deny`, `unavailable` — not mixed with infrastructure retry/timeout counters.
- **Never log** tokens, credentials, or raw gRPC secrets.

## Relationship operations

- **Touch**: idempotent create/update (`TOUCH`).
- **Delete**: revoke membership (`DELETE` via `AuthorizationRelationshipOperation.Delete`).
- Party membership projection: outbox → `user → party#member`.

## Incident / outage

1. Protected routes return `503` when authorization is `Unavailable`.
2. Readiness fails if probe enabled and SpiceDB down.
3. Do **not** switch Production to InMemory — fix SpiceDB or set `Mode=Disabled` only with explicit ops approval.
4. Tuple repair: re-run projection or manual typed write via admin tooling (future).

## Secret rotation

1. Deploy new token to env / secret store.
2. Rolling restart Host instances.
3. Verify readiness and admin/seller smoke checks.

## Troubleshooting

| Symptom | Check |
|---|---|
| Startup validation fail | Endpoint/token/TLS in Production |
| Ready=false `spicedb-unreachable` | Network, SpiceDB pod, token |
| All checks Unavailable | gRPC connectivity, deadline too low |
| Deny expected but Allow | Tuple missing/wrong tenant resource id |

## Local integration tests

```powershell
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter SpiceDbIntegrationTests
```

Requires Docker for Testcontainers (`authzed/spicedb:v1.56.0`).
