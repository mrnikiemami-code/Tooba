# tests-guards — TB-TMAR-HOST-OBSERVABILITY-AMC-001

| Area | Coverage today |
|---|---|
| Middleware order | Not locked by Host Observability-specific guard |
| Correlation fallback | Foundation Correlation / Host CorrelationRuntimeTests (not enrichment-specific) |
| Tenant/store/actor scope fields | ObservabilityLogScope foundation tests omit missing keys |
| Raw IP / HttpPath | **MISSING** Host Observability guard |
| Query string exclusion | Implicit (code uses Path.Value only) — not guarded |
| Exception propagation / scope disposal | **MISSING** dedicated test |
| Path↔namespace | **MISSING** (violation currently unguarded) |

Recommend W1 add `HostObservabilityAmcW1GuardTests` covering tree/namespace/order/IP gating/path/query exclusion/no custom tracing.
