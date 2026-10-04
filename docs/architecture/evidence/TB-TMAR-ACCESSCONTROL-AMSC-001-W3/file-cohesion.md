# File cohesion / size — AccessControl (W3)

## Classification

| Aspect | State |
| --- | --- |
| File-Cohesion-State | `COHESIVE` |
| New god-file introduced | `ZERO` |
| New oversized file introduced | `ZERO` |
| `MULTI_RESPONSIBILITY_COHESION_VIOLATION` | `ZERO` |
| Artificial parallel decomposition to game a guard | `ZERO` |
| Meaningless tiny files without responsibility separation | `ZERO` |

## Largest production files

| File | LOC | Responsibility | Verdict |
| --- | --- | --- | --- |
| `Infrastructure/Directories/AccessControlDirectory.cs` | 968 | one cohesive directory implementing `IAccessControlDirectory` | `OVERSIZED_ONLY` → `WATCH` (baselined) |
| `Endpoints/Admin/AccessControlAdminEndpoints.cs` | 402 | admin route surface | `COHESIVE` |
| `Endpoints/Seller/AccessControlSellerEndpoints.cs` | 402 | seller route surface | `COHESIVE` |
| `Endpoints/Admin/AccessControlAdminSellerEndpoints.cs` | 342 | admin-over-seller route surface | `COHESIVE` |
| `Infrastructure/Authorization/SpiceDbAuthorizationAdapter.cs` | 344 | SpiceDB adapter | `COHESIVE` |
| `Infrastructure/Persistence/Migrations/AccessControlDbContextModelSnapshot.cs` | 296 | EF generated | generated |
| `Infrastructure/Persistence/Migrations/…InitialAccessControl.Designer.cs` | 293 | EF generated | generated |
| `Infrastructure/Authorization/AuthorizationAdapters.cs` | 193 | authorization adapter wiring | `COHESIVE` |
| `Infrastructure/Authorization/SpiceDbAuthorizationBootstrapper.cs` | 177 | SpiceDB bootstrap | `COHESIVE` |
| `Infrastructure/Development/Seller/SellerDevContextBootstrap.cs` | 147 | seller dev-context bootstrap | `COHESIVE` |

All remaining production files are ≤ 127 LOC and single-responsibility.

## `AccessControlDirectory.cs` disposition

| Check | Result |
| --- | --- |
| Single cohesive responsibility behind one port | ✅ |
| Foreign-module joins | `ZERO` |
| HTTP concerns | `ZERO` |
| Mixed capability ownership | `ZERO` |
| Multi-responsibility god-file | no |

Per `tooba-architecture-structure` §12 this is `OVERSIZED_ONLY` → `WATCH`; it does not force a
structure FAIL and no cosmetic split was performed to game a size guard. State
`accessControlDirectoryState = OVERSIZED_ONLY_WATCH` is preserved from the prior certification.

### W1 growth analysis

| Revision | Physical LOC (`TmarSourceSizeGuard.CountPhysicalLoc`) |
| --- | --- |
| `a3ba1a4f` (W0) | 965 |
| `HEAD` (W1/W2) | 969 |

Delta `+4` LOC. The W1 change to this file is purely mechanical:
- 20 raw `"access.*"` string literals → 20 `AccessControlErrorCodes.*` constant references
  (constant names are longer than the literals);
- `CreateAccessRoleCommand`/`UpdateAccessRoleCommand`/`CloneAccessRoleCommand` →
  `CreateRoleRequest`/`UpdateRoleRequest`/`CloneRoleRequest` (shorter names);
- `using` block re-sorted/alphabetised (net 0 lines) plus the new
  `using Tooba.AccessControl.Contracts.Errors;`;
- duplicate `using Tooba.AccessControl.Contracts.Enums;` removed.

`git diff` for the file is `34 insertions(+), 30 deletions(-)` — all line-level substitutions, no
new responsibility, no new method. This is **not** `OVERSIZED_GROWTH` in the architecture sense
(it is a constant-name substitution inside an already-baselined file). It is recorded honestly as
residual baseline drift in `residual-debt.md` because the size baseline still keys the file at its
pre-`Directories/` path.

## W1 cohesion repair confirmed at certification time

| W0 defect | Repair | W3 verification |
| --- | --- | --- |
| `Application/Models/AccessControlDtos.cs` (8 mixed records) | split into 11 capability models | shared `Application/Models/` = exactly `AccessOwnerScope.cs` |
| transport records shadowing CQRS `*Command` | renamed to `*RoleRequest` | no `CreateAccessRoleCommand`/`UpdateAccessRoleCommand`/`CloneAccessRoleCommand` anywhere under Application |
| single-file `Application/Exceptions/` | merged into `Validation/` | `Exceptions/` absent |
| single-file `Application/Validators/` | merged into `Validation/` | `Validators/` absent at Application root |
| 20 raw error-code literals in the directory | owned constants | repo-wide scan for `"access.*"` literals = `ZERO` |

## No over-split

The W1 split is justified by capability ownership
(`AccessRoleDto`→`Roles/Models`, `RolePermissionGrant`→`Permissions/Models`,
`UserRoleAssignmentDto`→`Assignments/Models`, `SellerCeilingEntryDto`→`Ceiling/Models`,
`EffectiveAccessDto`/`EffectivePermissionDto`/`AccessUserHitDto`→`Access/Models`). Cross-capability
`AccessOwnerScope` correctly stayed in the shared `Models/`. No file was created whose only
justification is a size/structure guard.
