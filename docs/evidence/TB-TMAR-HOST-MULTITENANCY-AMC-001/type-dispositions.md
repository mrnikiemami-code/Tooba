# type-dispositions — TB-TMAR-HOST-MULTITENANCY-AMC-001

| Type | Disposition | Reason |
|---|---|---|
| `HttpCommerceContextAccessor` | KEEP_AS_THIN_HOST_CONTEXT_ADAPTER | HTTP `HttpContext.Items` + optional worker `Assign`; implements BuildingBlocks current-context contracts; not StoreContext business authority |
| `TenantResolutionMiddleware` | KEEP_AS_GLOBAL_HOST_MULTITENANCY_PLATFORM | Host/domain edition resolution, fail-closed routing, Activity tags, assigns StoreCommerce via StoreContext contract |

Whole-file label alone is insufficient — both types remain Host-owned with distinct responsibilities → **MUST_SPLIT** into two files under `MultiTenancy/`.
