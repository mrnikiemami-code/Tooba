# Capability map — AccessControl (W2)

Capabilities are business responsibility axes already present in the module; none was invented
from Command type names.

## Application capabilities

| Capability root | Business responsibility | Commands | Queries | Models | Validators |
| --- | --- | --- | --- | --- | --- |
| `Access` | effective-access evaluation, scope resources, access-user search | — | 3 | 3 | — |
| `Assignments` | user↔role assignment lifecycle | 2 | 1 | 1 | 1 |
| `Bootstrap` | access-control seed/bootstrap of platform roles | 1 | — | — | — |
| `Ceiling` | platform seller permission ceiling | 1 | 1 | 1 | 1 |
| `Permissions` | role permission grants + permission catalog reads | 1 | 3 | 1 | 1 |
| `Roles` | role CRUD/archive/clone | 4 | 2 | 4 | 3 |

## Shared (cross-capability) seams

| Folder | Responsibility |
| --- | --- |
| `Authorization/` | `AccessControlCapabilityGate` — capability admission for every request |
| `Composition/` | `AccessControlOperation` — Result → operation mapping shared by all capabilities |
| `Development/` | seller dev-context capability (query + port + models) used by seller tooling |
| `Models/` | `AccessOwnerScope` — shared owner-scope primitive |
| `Ports/` | `IAccessControlDirectory` — the single Application→Infrastructure persistence port |
| `Validation/` | typed fault, reusable Fluent rules, validation codes shared by all capabilities |

`Authorization`, `Composition`, `Models`, `Ports` and `Validation` are **not** business
capabilities; they are the module's canonical shared seams and are allowed to live at the
Application root by the repository pattern.

## Endpoints capabilities

| Folder | Audience / responsibility |
| --- | --- |
| `Admin/` | admin role/permission/ceiling/assignment surface (`AccessControlAdminEndpoints.cs`, `AccessControlAdminSellerEndpoints.cs`) |
| `Seller/` | seller self-service role/assignment/ceiling surface |
| `Seller/Development/` | seller dev-context surface |
| `Errors/` | error catalog contributor + HTTP problem mapping |
| `Resources/` | localized error resources |
| root | `AccessControlEndpointModule.cs` — composition entry only |

## Infrastructure capabilities / integrations

| Folder | Responsibility |
| --- | --- |
| `Adapters/` (+ `Adapters/Security/`) | effective-access readers, authorization readiness probe |
| `Authorization/` | SpiceDB adapter, bootstrapper, options, registration, instrumentation |
| `Development/` (+ `Development/Seller/`) | development seed prelude + seller dev-context bootstrap |
| `Directories/` | `AccessControlDirectory` — the Application port implementation |
| `Messaging/` | outbox registration |
| `Observability/` | module instrumentation |
| `Persistence/` (+ `Persistence/Migrations/`) | EF DbContext + migrations |
| root | `AccessControlModule.cs` — composition entry only |

## Contracts / Domain

| Project | Capability folders |
| --- | --- |
| `Tooba.AccessControl.Contracts` | `Access/`, `Development/`, `Enums/`, `Errors/`, `Readiness/` |
| `Tooba.AccessControl.Domain` | `Aggregates/` (`AccessRole`, `RolePermission`, `UserRoleAssignment`, `PlatformSellerCeiling`, `AccessAuditEvent`) |

## Cross-module boundary

`AccessControl` consumes only foreign `*.Contracts` projects:

- `Tooba.Identity.Contracts`
- `Tooba.OperatorProfile.Contracts`
- `Tooba.Catalog.Contracts`
- `Tooba.Party.Contracts`

No foreign `Domain`/`Application`/`Infrastructure` reference exists in any AccessControl
`.csproj`. Boundary state: `CONTRACTS_ONLY`.
