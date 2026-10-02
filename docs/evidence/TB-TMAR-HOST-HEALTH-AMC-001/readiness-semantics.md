# readiness-semantics — TB-TMAR-HOST-HEALTH-AMC-001

## Check order

1. `edition` — Unset → fail `unconfigured` (503)
2. `postgresql` — each CollectConnectionReferences key must exist non-empty in `PostgreSQL.ConnectionReferences` — else `missing-reference:{reference}` (503)
3. `authorization` — `IAuthorizationReadinessProbe.EvaluateAsync` — fail if `!Ready` (503)
4. messaging (if Enabled):
   - bus null → `bus-unavailable`
   - `CheckHealth()` Unhealthy → `unhealthy`
   - else `messaging` = status string; `messaging-transport=postgresql-sql`; `messaging-schema={Schema}`
5. messaging disabled → `messaging=disabled`, `messaging-transport=n/a`

## Connection readiness truth

**CONFIGURED** only — does **not** open DB / probe TCP. Operators must not interpret `/ready` as connectivity proof.

## Tenant readiness parity

`CollectConnectionReferences` iterates **all** `registry.Tenants` (no Active filter).

| Surface | Active-only? |
|---|---|
| MultiTenancy request resolve | YES (Active only) |
| PlatformOptionsValidator StoreCommerce | YES (Active only) |
| PlatformOptionsValidator connection refs | NO (all tenants) |
| Host readiness connection refs | NO (all tenants) — **matches connection-ref startup collect**, diverges from MultiTenancy Active-only |

Debt: inactive tenant with missing connection reference can fail readiness even though MultiTenancy never serves it. Classify as **SEMANTIC_PARITY_DEBT** (align Active-only in W1 or keep explicit documented parity with startup connection collect).

## Exception safety

No try/catch around auth probe or `bus.CheckHealth()`. Throws propagate to Host global `IExceptionHandler` / presentation — may yield 500 ProblemDetails rather than readiness JSON. Classify as **BOUNDED_HANDLING_DEBT** for W1 consideration (catch → not-ready check label) without redesign in Analyze.
