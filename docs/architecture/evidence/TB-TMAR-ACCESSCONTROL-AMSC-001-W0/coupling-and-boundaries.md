# Coupling and boundaries — AccessControl (W0)

## Verdict

| Field | Value |
| --- | --- |
| Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` |
| Cross-Module-Join-State | `NONE` |
| Persistence-Ownership-State | `CORRECT` |
| Endpoint-Ownership-State | `MODULE_OWNED` |
| Contracts-Boundary-State | `CLEAN` |
| Host-Residue-State | ZERO illegal |

## Foreign project references (from csproj)

| AccessControl project | Foreign references |
| --- | --- |
| `Tooba.AccessControl.Contracts` | none |
| `Tooba.AccessControl.Domain` | none |
| `Tooba.AccessControl.Application` | `Identity.Contracts`, `OperatorProfile.Contracts`, `Catalog.Contracts` |
| `Tooba.AccessControl.Infrastructure` | `Catalog.Contracts`, `Identity.Contracts`, `Party.Contracts` |
| `Tooba.AccessControl.Endpoints` | none |

Every foreign edge is a `*.Contracts` project. Zero foreign `*.Application`, `*.Infrastructure` or
`*.Domain` edges.

## Foreign source usings (verified ZERO)

Scan over `src/backend/Modules/AccessControl/**` for
`using Tooba.<Foreign>.{Application,Infrastructure,Domain}` returned **no matches**.

Legal Contracts usings present:

- `Infrastructure/Directories/AccessControlDirectory.cs` → `Tooba.Catalog.Contracts`,
  `Tooba.Catalog.Contracts.Ports`
- `Infrastructure/Development/Seller/SellerDevContextBootstrap.cs` → `Tooba.Identity.Contracts`,
  `.Auth`, `.Problems`, `Tooba.Party.Contracts.Ports`
- `Application/Access/Queries/SearchAccessUsersQuery.cs` → `Tooba.Identity.Contracts`, `.Actors`,
  `.Contacts`, `Tooba.OperatorProfile.Contracts.Ports`
- `Application/Access/Queries/ListScopeResourcesQuery.cs` → `Tooba.Catalog.Contracts`,
  `Tooba.Catalog.Contracts.Ports`

## Cross-module join inventory

**NONE.** Every LINQ `join` in `AccessControlDirectory.cs` joins only module-owned sets:

- `ListAssignmentsAsync` — `_db.Assignments` ⋈ `ScopedRoles(owner)` (own `Roles`).
- `GetEffectiveAccessAsync` — `_db.Assignments` ⋈ `ScopedRoles(owner)` (own `Roles`).

Category/Brand/Product names and existence checks go through
`Tooba.Catalog.Contracts.Ports.IAccessControlScopeResourceLookup`
(implementation `Catalog.Infrastructure/Directories/CatalogAccessControlScopeResourceLookup.cs`).
No foreign `DbSet`, no foreign `DbContext`, no raw SQL, no navigation property crossing modules.

## Persistence ownership

- One module DbContext: `Infrastructure/Persistence/AccessControlDbContext.cs` (113 LOC).
- Owned sets: `Roles`, `RolePermissions`, `Assignments`, `SellerCeilings`, `AuditEvents`.
- Two migrations owned by the module:
  `20260827140753_InitialAccessControl`, `20260827181000_AddSellerCeilingScope` + model snapshot.
- `Application` and `Endpoints` reference no `DbContext`.

## Endpoint ownership

- `Tooba.Host/AccessControl/` does **not** exist on disk.
- `AccessControlEndpointModule.MapAccessControlModuleEndpoints` owns all four route groups.
- Host `Program.cs` only composes: `AddAccessControlEndpointPresentation()` and
  `MapAccessControlModuleEndpoints()`.
- Endpoints reference no `Tooba.AccessControl.Domain` and no `Tooba.AccessControl.Infrastructure`.

## Host authority classification

| Host reference | Classification |
| --- | --- |
| `Program.cs` → `AddAccessControlEndpointPresentation` | `ALLOWED_COMPOSITION_ROOT` |
| `Program.cs` → `MapAccessControlModuleEndpoints` | `ALLOWED_COMPOSITION_ROOT` |
| `Program.cs` → `IAuthorizationReadinessProbe` / `IAccessControlEffectiveAccessReader` consumption | `ALLOWED_CONTRACT_CONSUMPTION` |

`ILLEGAL_BUSINESS_AUTHORITY`, `ILLEGAL_PERSISTENCE_AUTHORITY` and `ILLEGAL_ENDPOINT_OWNERSHIP`
are all **ZERO**.

## Microservice extractability

No direct foreign Domain/Application/Infrastructure dependency, no shared mutable aggregate, no
cross-module transaction, no shared DbContext. The module communicates outward only through its own
Contracts and consumes other modules only through their Contracts. It is extractable as-is.
