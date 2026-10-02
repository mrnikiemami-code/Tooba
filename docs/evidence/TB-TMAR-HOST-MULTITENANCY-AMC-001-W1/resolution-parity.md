# resolution-parity — TB-TMAR-HOST-MULTITENANCY-AMC-001-W1

| Case | HTTP | Code |
|---|---|---|
| Edition Unset | 503 | platform.edition.unconfigured |
| Marketplace connection missing | 503 | platform.connection.unconfigured |
| Marketplace success | DeploymentStoreCommerce; no tenant host lookup | — |
| SingleStore unknown host | 404 | platform.resolution.failed |
| SingleStore inactive/disabled/suspended | 404 | platform.resolution.failed |
| SingleStore active | tenant StoreCommerce + connection resolve | — |

Tenant-id header is **not** source of truth. HostNormalizer + ControlPlaneRegistry.Hosts allowlist unchanged.
Focused: `TenantResolutionTests`, `TenantResolutionPlatformErrorTests` PASS.
