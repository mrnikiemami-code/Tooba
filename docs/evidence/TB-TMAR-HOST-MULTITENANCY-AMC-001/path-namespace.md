# path-namespace — TB-TMAR-HOST-MULTITENANCY-AMC-001

| Item | Current | Required if retained |
|---|---|---|
| Path | `Host/Tooba.Host/MultiTenancy/` | same |
| Namespace | `Tooba.Host` | `Tooba.Host.MultiTenancy` |
| State | **VIOLATION** | EXACT after W1 |

Impact of namespace repair: Program registrations/usings (`HttpCommerceContextAccessor`, `TenantResolutionMiddleware`) and test factories that reference types by namespace/`global using` must update; concrete types are `internal` in Host assembly so external modules unaffected.
