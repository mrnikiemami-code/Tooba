# Cohesion balance — AccessControl (W2)

## Classification

| Axis | State |
| --- | --- |
| File-Cohesion-State | `COHESIVE` |
| `MULTI_RESPONSIBILITY_COHESION_VIOLATION` | `ZERO` |
| `OVER_SPLIT` (cosmetic) | `ZERO` |
| `OVERSIZED_ONLY` | 1 file, `WATCH` (baselined) |

## Largest production files

| File | LOC | Responsibility | Verdict |
| --- | --- | --- | --- |
| `Infrastructure/Directories/AccessControlDirectory.cs` | 882 | one cohesive directory: implements `IAccessControlDirectory` over the module DbContext | `OVERSIZED_ONLY` — WATCH, baselined |
| `Endpoints/Admin/AccessControlAdminEndpoints.cs` | 402 | admin role/permission/ceiling/assignment route surface | `COHESIVE` |
| `Endpoints/Seller/AccessControlSellerEndpoints.cs` | 402 | seller self-service route surface | `COHESIVE` |
| `Endpoints/Admin/AccessControlAdminSellerEndpoints.cs` | 342 | admin-over-seller route surface | `COHESIVE` |
| `Infrastructure/Authorization/SpiceDbAuthorizationAdapter.cs` | 344 | SpiceDB adapter | `COHESIVE` |
| `Infrastructure/Persistence/Migrations/AccessControlDbContextModelSnapshot.cs` | 296 | EF generated snapshot | `COHESIVE` (generated) |
| `Infrastructure/Persistence/Migrations/…InitialAccessControl.Designer.cs` | 293 | EF generated designer | `COHESIVE` (generated) |
| `Infrastructure/Authorization/AuthorizationAdapters.cs` | 193 | authorization adapter wiring | `COHESIVE` |
| `Infrastructure/Authorization/SpiceDbAuthorizationBootstrapper.cs` | 177 | SpiceDB bootstrap | `COHESIVE` |
| `Infrastructure/Development/Seller/SellerDevContextBootstrap.cs` | 147 | seller dev-context bootstrap | `COHESIVE` |

Every other production file is ≤ 127 LOC and single-responsibility.

## `AccessControlDirectory.cs` disposition

The file is large but implements **one** cohesive responsibility behind **one** port
(`IAccessControlDirectory`), with no foreign-module joins, no HTTP concerns and no mixed
capability ownership. Per section 12 this is `OVERSIZED_ONLY` → `WATCH`; it does **not** force a
structure FAIL, and no cosmetic split was performed to game size guards. The state
`accessControlDirectoryState = OVERSIZED_ONLY_WATCH` is preserved from the prior certification.

## W1 cohesion repair confirmed

| Defect (W0) | W1 repair | W2 verification |
| --- | --- | --- |
| `Application/Models/AccessControlDtos.cs` — 8 unrelated top-level records in one dump | split into 11 capability-cohesive model files | shared `Application/Models/` now contains exactly `AccessOwnerScope.cs`; each capability owns its DTOs |
| transport records `*AccessRoleCommand` colliding with the MediatR command vocabulary | renamed to `*RoleRequest` under `Roles/Models/` | no `CreateAccessRoleCommand`/`UpdateAccessRoleCommand`/`CloneAccessRoleCommand` string remains anywhere under Application |
| single-file `Application/Exceptions/` folder | merged into `Application/Validation/` | `Exceptions/` absent; `Validation/` holds exactly the three shared primitives |
| single-file `Application/Validators/` folder | merged into `Application/Validation/` | `Validators/` absent at Application root; capability `Validators/` remain next to their requests |

## No over-split

No file was created whose only justification is a size or structure guard. The W1 split is
justified by capability ownership: `AccessRoleDto` → `Roles/Models`, `RolePermissionGrant` →
`Permissions/Models`, `UserRoleAssignmentDto` → `Assignments/Models`, `SellerCeilingEntryDto` →
`Ceiling/Models`, `EffectiveAccessDto`/`EffectivePermissionDto`/`AccessUserHitDto` →
`Access/Models`. Cross-capability `AccessOwnerScope` correctly stayed in the shared `Models/`.
