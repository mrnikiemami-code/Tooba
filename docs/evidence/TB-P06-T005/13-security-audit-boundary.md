# 13 — No decision cache policy (TB-P06-T005)

## Decision

**No authorization decision cache** added in TB-P06-T005.

## Rationale

- Permission checks must reflect current SpiceDB graph state.
- Stale Allow from cache would violate fail-closed / least-privilege.
- SpiceDB client transport retries are bounded infrastructure retry — not decision caching.

## Current behavior

| Layer | Caching? |
|---|---|
| `SpiceDbAuthorizationAdapter.CanAsync` | No — every check hits SpiceDB (with configured consistency) |
| `InMemoryAuthorizationAdapter` | In-process tuple store only (dev/test mode) |
| `AuthorizationInstrumentation` | Metrics only |
| Host `ICacheService` | Unrelated to authorization decisions |

## Consistency tokens

`AuthorizationCallContext.ConsistencyToken` is a **read-consistency hint**, not a cached decision.

## Future (documented, not blockers)

- Optional short-lived cache only with explicit Architect approval, versioned invalidation, and deny-by-default on SpiceDB write events.
- ZedToken propagation from write responses to subsequent checks in same handler.

## Ops reference

See `docs/operations/authorization-spicedb.md` — caching section documents intentional absence.
