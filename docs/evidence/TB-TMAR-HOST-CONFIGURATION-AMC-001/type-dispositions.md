# type-dispositions — TB-TMAR-HOST-CONFIGURATION-AMC-001

| Type | Kind | Disposition | Rationale |
| --- | --- | --- | --- |
| `ToobaPlatformOptions` | Raw bind root (`Tooba` section) | KEEP_AS_GLOBAL_HOST_CONFIGURATION_PLATFORM | Host owns process config bind model |
| `StoreCommerceOptions` | Raw nested options | KEEP_AS_GLOBAL_HOST_CONFIGURATION_PLATFORM | Control-plane comercial defaults owned by Host config, not modules |
| `MarketplaceOptions` | Raw nested | KEEP_AS_GLOBAL_HOST_CONFIGURATION_PLATFORM | Marketplace connection reference catalog entry |
| `SingleStoreOptions` | Raw nested | KEEP_AS_GLOBAL_HOST_CONFIGURATION_PLATFORM | Tenant allowlist container |
| `TenantRecordOptions` | Raw nested | KEEP_AS_GLOBAL_HOST_CONFIGURATION_PLATFORM | Pre-normalize tenant shape |
| `PostgreSqlOptions` | Raw nested | KEEP_AS_GLOBAL_HOST_CONFIGURATION_PLATFORM | ConnectionReferences catalog (+ legacy root string) |
| `TenantRecord` | Normalized snapshot | KEEP_AS_GLOBAL_HOST_CONTROL_PLANE_SNAPSHOT | Immutable runtime tenant view after BuildRegistry |
| `ControlPlaneRegistry` | Normalized snapshot | KEEP_AS_GLOBAL_HOST_CONTROL_PLANE_SNAPSHOT | Process edition/hosts/tenants snapshot — not durable CP DB |
| `PlatformOptionsValidator` | `IValidateOptions<>` | KEEP_AS_GLOBAL_HOST_CONFIGURATION_VALIDATOR | Startup fail-fast + BuildRegistry authority |

Rejected moves:

- MOVE_TO_STORECONTEXT — Configuration *builds* `StoreCommerceContext` from control-plane; StoreContext owns runtime assigner, not options bind.
- MOVE_TO_PERSISTENCE — catalog ownership stays Configuration; Persistence only resolves.
- MOVE_TO_MULTITENANCY — MultiTenancy consumes registry; does not own options schema.
- MOVE_TO_BUILDINGBLOCKS — would pull Host deployment options into shared kernel.
