# 07 — Consistency token support (TB-P06-T005)

## Call context

`AuthorizationCallContext.ConsistencyToken` optional on every check.

## Adapter behavior (`BuildConsistency`)

| Input | SpiceDB consistency |
|---|---|
| Non-empty `ConsistencyToken` | `AtLeastAsFresh` with `ZedToken` |
| Empty token + `ConsistencyMode=MinimizeLatency` | `MinimizeLatency=true` |
| Empty token + default | `FullyConsistent=true` |

## Configuration default

`Tooba:Authorization:SpiceDb:ConsistencyMode=FullyConsistent` — safe default for permission checks without caller-supplied token.

## When to pass token

After relationship write, callers may pass returned ZedToken (future wiring) to read-your-writes checks in same request flow.

## Not cached

Consistency tokens are per-request inputs; no global decision cache stores tokens.

## Validator

`ConsistencyMode` must be exactly `FullyConsistent` or `MinimizeLatency`.

## Ops note

Production permission checks on security-sensitive paths should prefer `FullyConsistent` unless latency SLO explicitly allows `MinimizeLatency`.
