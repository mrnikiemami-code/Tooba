# tests-guards — TB-TMAR-HOST-HEALTH-AMC-001

| Guard / test | Coverage |
|---|---|
| `HostReadinessBoundaryGuardTests` | AccessControl Contracts-only; Program Map; probe semantics |
| MultiTenancy skip `/health` `/ready` | TenantResolutionMiddleware SkipPrefixes |
| SessionAuthenticationMiddleware skip | `/health` `/ready` |
| Health structure/path namespace guard | **ABSENT** |
| HostReadinessEvaluator unit matrix (edition/refs/messaging) | **ABSENT / thin** |
| Disclosure sanitize assertions | **ABSENT** |
| IServiceProvider absence | **ABSENT** |

Missing coverage → add in W1/W2-CERT (`HostHealthAmcW1GuardTests` / Cert).
