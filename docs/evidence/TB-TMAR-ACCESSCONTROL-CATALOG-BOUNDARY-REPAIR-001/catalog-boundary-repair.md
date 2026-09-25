# TB-TMAR-ACCESSCONTROL-CATALOG-BOUNDARY-REPAIR-001 — Catalog boundary repair evidence

Parent: `TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001` (commit `40c788a9`)
Track: `HOST_FIRST_ACCESSCONTROL`
Status: PASS (backend only)

## 1. Before dependency edge

```
Tooba.AccessControl.Infrastructure
  -> Tooba.Catalog.Application          (project reference)
  -> Tooba.Catalog.Application.ICatalogLookupGateway  (field, ctor param)
```

Source evidence (pre-change) in
`src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/AccessControlDirectory.cs`:

- `using Tooba.Catalog.Application;`
- `private readonly ICatalogLookupGateway _catalog;`
- `_catalog.FindCategoryAsync(categoryId, ct)` in `SetSellerCeilingAsync` and `ValidateGrantsForOwnerAsync`
- `_catalog.GetCategoryNamesAsync(categoryIds, ct)` in `GetEffectiveAccessAsync`

csproj referenced
`..\..\Catalog\Tooba.Catalog.Application\Tooba.Catalog.Application.csproj`.

## 2. After dependency edge

```
Tooba.AccessControl.Infrastructure
  -> Tooba.Catalog.Contracts            (project reference)
  -> Tooba.Catalog.Contracts.IAccessControlScopeResourceLookup  (field, ctor param)
```

`AccessControl.Infrastructure -> Catalog.Application = ZERO`
`AccessControl.Infrastructure -> Catalog.Domain = ZERO`

## 3. Catalog.Contracts members added/reused

File: `src/backend/Modules/Catalog/Tooba.Catalog.Contracts/AccessControlScopeResourceContracts.cs`

Reused (from accepted parent): `AccessControlScopeResourceCategory/Brand/Product`,
`IAccessControlScopeResourceLookup.ListCategoriesAsync/ListBrandsAsync/ListProductsAsync`.

Added for this task:
- `Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken);`
- `Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken);`

Contract remains neutral: records only primitive ids/strings; `Tooba.Catalog.Contracts`
still references only `Tooba.BuildingBlocks` (no Catalog.Application/Domain/Infrastructure).

## 4. Catalog-owned implementation mapping

File: `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogAccessControlScopeResourceLookup.cs`

- `CategoryExistsAsync` → `catalog.FindCategoryAsync(...) is not null` (single boolean projection).
- `GetCategoryNamesAsync` → delegates directly to `catalog.GetCategoryNamesAsync(...)`, preserving
  the dictionary (missing ids absent → callers get `null` via `GetValueOrDefault`).

`ICatalogLookupGateway` remains internal to Catalog; not exposed through the contract.
No new AccessControl-specific adapter was created in Host.

## 5. Project reference before/after

`Tooba.AccessControl.Infrastructure.csproj`

| Before | After |
|--------|-------|
| `..\..\Catalog\Tooba.Catalog.Application\Tooba.Catalog.Application.csproj` | removed |
| (none) | `..\..\Catalog\Tooba.Catalog.Contracts\Tooba.Catalog.Contracts.csproj` added |

## 6. Category-existence parity

Before: `var found = await _catalog.FindCategoryAsync(categoryId, ct); if (found is null) throw ...`.
After: `var found = await _catalog.CategoryExistsAsync(categoryId, ct); if (!found) throw ...`.

Identical predicate (existence == non-null reference). Both call sites
(`SetSellerCeilingAsync`, `ValidateGrantsForOwnerAsync`) preserved with the same
`AccessControlException("access.scope.unknown_resource", "ردهٔ scope در Catalog یافت نشد.")`.

## 7. Category-name parity

Before: `await _catalog.GetCategoryNamesAsync(categoryIds, ct)` returning `IReadOnlyDictionary<Guid,string>`.
After: same method signature through the contract seam, delegating to the same gateway.
`categoryIds.Length == 0` short-circuit and `GetValueOrDefault(...)` semantics unchanged
(missing category name still resolves to `null`).

## 8. Stable error-code parity

Unchanged codes/messages:
- `access.scope.unsupported`
- `access.scope.unknown_resource` (both existence checks)
- seller escalation/ceiling codes untouched.

No permission/ceiling/escalation/role/assignment logic modified.

## 9. Route ownership

No route ownership change. Module Admin/Seller endpoint mappings and Host
`demo-preview` residue are untouched by this task.

## 10. Boundary audit

| Edge | Result |
|------|--------|
| `AccessControl.Infrastructure` → `Catalog.Application` | ZERO |
| `AccessControl.Infrastructure` → `Catalog.Domain` | ZERO |
| `AccessControl.Infrastructure` → `Catalog.Contracts` | ALLOWED (explicit) |
| `AccessControl.Application` → `Catalog.Contracts` | ALLOWED |
| `AccessControl.Endpoints` → `Catalog.Application` | ZERO |
| `AccessControl.Endpoints` → `Catalog.Domain` | ZERO |
| `Host` AccessControl → `Catalog.Application` | ZERO |
| `Tooba.Catalog.Contracts` → Catalog.Application/Domain/Infrastructure | ZERO |
| `ICatalogLookupGateway` in AccessControl.Infrastructure | ZERO |
| `Tooba.Catalog.Application` in AccessControl.Infrastructure source | ZERO |
| Catalog-owned impl (CatalogAccessControlScopeResourceLookup) owns seam impl | yes |

Test-side note: `Tooba.Host.Tests/FakeCatalogLookupGateway.cs` is a Host test double;
it now additionally implements `IAccessControlScopeResourceLookup` so the
`AccessControlDirectory` constructor can be satisfied in tests. This is test-only,
not a production boundary.

## 11. Focused builds/tests

| Command | Result |
|---------|--------|
| `dotnet build .../Tooba.Catalog.Contracts.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build .../Tooba.Catalog.Infrastructure.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build .../Tooba.AccessControl.Infrastructure.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet test Tooba.Host.Tests --filter "…AccessControl_module_boundary_static_checks|…Unknown_category_scope_is_rejected|…Ceiling_escalation_and_category_scope_policy"` | Passed 1, Failed 0, Skipped 2 (Docker/Testcontainers unavailable) |

## 12. Remaining AccessControl Host residue

- `AccessControlEndpoints.cs` — `GET /v1/admin/access-control/demo-preview` only.
- `AccessControlDevelopmentSeed.cs`
- `AccessControlDemoSnapshot.cs`
- `Program.cs` legacy AccessControl mapping/bootstrap residue.
- Target `src/backend/Host/Tooba.Host/AccessControl = ZERO files` is a later cleanup task.

AccessControl remains `IN_PROGRESS`; no structure certification, no `COMPLETE_REFERENCE_PATTERN`.
