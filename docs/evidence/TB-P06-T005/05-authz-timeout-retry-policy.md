# 05 — Adapter retry policy (TB-P06-T005)

## Scope

`SpiceDbAuthorizationAdapter.ExecuteWithRetryAsync` — infrastructure failures only.

## Settings

| Setting | Default | Behavior |
|---|---|---|
| `RetryMaxAttempts` | 3 | Minimum 1 enforced in code |
| `RetryBaseDelayMilliseconds` | 100 | Delay = `base * attempt` before next try |

## Retryable gRPC status codes

- `Unavailable`
- `DeadlineExceeded`
- `ResourceExhausted`

## Not retried

- Permission **DENY** (normal business outcome)
- Explicit cancellation (`OperationCanceledException` when token cancelled)
- Non-transport exceptions

## Exhaustion

After max attempts → `InvalidOperationException("authorization.unavailable")` → caller receives `AuthorizationDecision.Unavailable`.

## Telemetry

Each retry increments `tooba.authorization.infrastructure` with `kind=retry`.

## Applies to

- `CheckPermissionAsync`
- `WriteRelationshipsAsync`
- `WriteSchemaAsync`

## Rationale

Bounded retry absorbs brief SpiceDB/network blips without masking DENY or fail-open on sustained outage.
