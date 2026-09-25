# TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001 — Scope-resources evacuation evidence

Parent: `TB-TMAR-RECOVERY-SOT-SYNC-ACCESSCONTROL-001` (commit `a53a866b`)
Production parent: `TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001` (commit `4a6074e6`)
Track: `HOST_FIRST_ACCESSCONTROL`
Status: PASS (backend only)

## 1. Route ownership before/after (12 routes)

| # | Route | Before (Host) | After (module) |
|---|-------|---------------|----------------|
| 1 | `GET /v1/admin/access-control/scope-resources/categories` | Host `AdminListCategoriesAsync` | `AccessControlAdminEndpoints` → `ListScopeResourcesQuery(Category)` |
| 2 | `GET /v1/admin/access-control/scope-resources/brands` | Host `AdminListBrandsAsync` | `AccessControlAdminEndpoints` → `ListScopeResourcesQuery(Brand)` |
| 3 | `GET /v1/admin/access-control/scope-resources/products` | Host `AdminListProductsAsync` | `AccessControlAdminEndpoints` → `ListScopeResourcesQuery(Product)` |
| 4 | `GET /v1/admin/access-control/scope-resources/warehouses` | Host `AdminDeferredScopeAsync` | `AccessControlAdminEndpoints` → `ListScopeResourcesQuery(Warehouse)` |
| 5 | `GET /v1/admin/access-control/scope-resources/stores` | Host `AdminDeferredScopeAsync` | `AccessControlAdminEndpoints` → `ListScopeResourcesQuery(Store)` |
| 6 | `GET /v1/admin/access-control/scope-resources/order-segments` | Host `AdminDeferredScopeAsync` | `AccessControlAdminEndpoints` → `ListScopeResourcesQuery(OrderSegment)` |
| 7 | `GET /v1/seller/access-control/scope-resources/categories` | Host `SellerListCategoriesAsync` | `AccessControlSellerEndpoints` → `ListScopeResourcesQuery(Category)` |
| 8 | `GET /v1/seller/access-control/scope-resources/brands` | Host `SellerListBrandsAsync` | `AccessControlSellerEndpoints` → `ListScopeResourcesQuery(Brand)` |
| 9 | `GET /v1/seller/access-control/scope-resources/products` | Host `SellerListProductsAsync` | `AccessControlSellerEndpoints` → `ListScopeResourcesQuery(Product)` |
| 10 | `GET /v1/seller/access-control/scope-resources/warehouses` | Host `SellerDeferredScopeAsync` | `AccessControlSellerEndpoints` → `ListScopeResourcesQuery(Warehouse)` |
| 11 | `GET /v1/seller/access-control/scope-resources/stores` | Host `SellerDeferredScopeAsync` | `AccessControlSellerEndpoints` → `ListScopeResourcesQuery(Store)` |
| 12 | `GET /v1/seller/access-control/scope-resources/order-segments` | Host `SellerDeferredScopeAsync` | `AccessControlSellerEndpoints` → `ListScopeResourcesQuery(OrderSegment)` |

Module mapping = exactly one per route. Host mapping = zero.

## 2. New Catalog.Contracts seam

`src/backend/Modules/Catalog/Tooba.Catalog.Contracts/AccessControlScopeResourceContracts.cs`

- `AccessControlScopeResourceCategory(Guid CategoryId, Guid? ParentCategoryId, string Name, string Status)`
- `AccessControlScopeResourceBrand(Guid BrandId, string Name, string Status)`
- `AccessControlScopeResourceProduct(Guid ProductId, string Title, string Status)`
- `IAccessControlScopeResourceLookup` with `ListCategoriesAsync` / `ListBrandsAsync` / `ListProductsAsync(string? search, CancellationToken)`.

Contract DTO field names match the previous JSON shape emitted by Host
(`categoryId`, `parentCategoryId`, `name`, `status`; `brandId`, `name`, `status`;
`productId`, `title`, `status`). `Tooba.Catalog.Contracts` still references only
`Tooba.BuildingBlocks` — no Application/Infrastructure reference added.

## 3. Catalog-owned implementation + registration

- `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogAccessControlScopeResourceLookup.cs`
  — thin internal adapter around `ICatalogLookupGateway`; maps Application-layer
  `AccessControlCategoryItem` / `AccessControlBrandItem` / `AccessControlProductItem`
  into the neutral contract records. `ICatalogLookupGateway` is **not** exposed to AccessControl.
- `CatalogModule.cs` registers
  `services.AddScoped<IAccessControlScopeResourceLookup, CatalogAccessControlScopeResourceLookup>();`

## 4. AccessControl CQRS query

`src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Queries/ListScopeResources/ListScopeResourcesQuery.cs`

- `enum AccessScopeResourceKind { Category, Brand, Product, Warehouse, Store, OrderSegment }`
- `record ScopeResourceListResult(bool Deferred, IReadOnlyList<object> Items)`
- `record ListScopeResourcesQuery(AccessScopeResourceKind Kind, string? Search) : IRequest<ScopeResourceListResult>`
- Handler depends only on `Tooba.Catalog.Contracts.IAccessControlScopeResourceLookup`.
  Category/Brand/Product call the seam; Warehouse/Store/OrderSegment return
  `Deferred = true, Items = []`.

Added project reference: `Tooba.AccessControl.Application` → `Tooba.Catalog.Contracts` (contracts only).

## 5. JSON parity

Before (Host) concrete resources:
```json
{ "deferred": false, "items": [ ... ] }
```
After (module): same anonymous JSON shape via `ScopeResourceListResult`
(`Deferred` → `deferred`, `Items` → `items`; camelCase HTTP policy unchanged).

## 6. `q` parity

`q` is bound identically (`string? q = null`) and passed straight to the
Catalog seam, which forwards to the pre-existing `ICatalogLookupGateway`
methods. No new filtering/paging invented.

## 7. Admin/Seller auth parity

- Admin: `IAdminPanelAccess.RequireAuthorizedAsync` → `AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", ...)`.
- Seller: `ISellerPanelAccess.RequireAuthorizedAsync` → same capability gate.

Identical capability (`accesscontrol.view`) and panel gate as before.

## 8. Seller filtering explicitly NOT added

`ListScopeResourcesQuery` carries no seller id; the seller handler discards
`SellerId`. This preserves the pre-change behavior where Host did not pass
`sellerId` into Catalog lookup. No silent seller filtering was introduced.

## 9. Deferred-resource parity

`warehouses`, `stores`, `order-segments` continue to return
`{ "deferred": true, "items": [] }` (no `message` key added).

## 10. Host removals

`src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs`
- Removed 12 scope-resource mappings.
- Removed `AdminListCategoriesAsync`, `AdminListBrandsAsync`, `AdminListProductsAsync`,
  `AdminDeferredScopeAsync`, `SellerListCategoriesAsync`, `SellerListBrandsAsync`,
  `SellerListProductsAsync`, `SellerDeferredScopeAsync`.
- Removed `using Tooba.Catalog.Application;`.
- Removed now-unused `RequireSellerAsync` and the `#region Seller` block.
- Retained `AdminDemoPreviewAsync` (out of scope), `Trace`, `MapError`.
- Remaining Host mapping: `GET /v1/admin/access-control/demo-preview` only.

## 11. Boundary audit

| Edge | Result |
|------|--------|
| `AccessControl.Application` → `Catalog.Contracts` | ALLOWED (added) |
| `AccessControl.Application` → `Catalog.Application` | ZERO |
| `AccessControl.Application` → `Catalog.Domain` | ZERO |
| `AccessControl.Endpoints` → `Catalog.Application` | ZERO |
| `AccessControl.Endpoints` → `Catalog.Domain` | ZERO |
| `Host AccessControlEndpoints.cs` → `Catalog.Application` | ZERO |
| old Host handler names in production | ZERO occurrences |
| `Tooba.Catalog.Contracts` → Catalog.Application/Infrastructure | ZERO |

Pre-existing (unchanged, out of scope): `AccessControl.Infrastructure`
references `Tooba.Catalog.Application` and `AccessControlDirectory.cs` uses
`ICatalogLookupGateway`; `AccessControl.Infrastructure` references
`Tooba.AccessControl.Application`, so `Catalog.Contracts` flows transitively —
no new edge introduced, no Catalog.Application/Domain leak into
Application/Endpoints.

## 12. Stale test handling

`Tooba.Host.Tests/AccessControlFoundationTests.AccessControl_module_boundary_static_checks`

Updated only the stale boundary assertions: removed the two assertions that
expected evacuated Host route text (`/v1/seller/access-control` and
`/v1/admin/sellers/{sellerId:guid}/access-control`) and the stale Host
`/scope-resources/categories` / `/me/capabilities` assertions; added assertions
that the module Admin/Seller endpoint files own `/scope-resources` and
`/me/capabilities` and that `AccessControlEndpointModule.cs` owns
`/v1/seller/access-control`. No other assertions altered; no new suite added.

## 13. Focused build/test results

| Command | Result |
|---------|--------|
| `dotnet build .../Tooba.Catalog.Contracts.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build .../Tooba.Catalog.Infrastructure.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build .../Tooba.AccessControl.Application.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build .../Tooba.AccessControl.Endpoints.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet test Tooba.Host.Tests --filter FullyQualifiedName~AccessControl_module_boundary_static_checks` | Passed: 1, Failed: 0 |

## 14. Residual Host AccessControl files/routes

- `AccessControlEndpoints.cs` — retains `GET /v1/admin/access-control/demo-preview` only.
- `AccessControlDevelopmentSeed.cs`
- `AccessControlDemoSnapshot.cs`
- `Program.cs` legacy AccessControl mapping/bootstrap residue (final cleanup pending).

Final Host target remains `src/backend/Host/Tooba.Host/AccessControl = ZERO files`, not in this task.
AccessControl stays `IN_PROGRESS`; no structure certification, no `COMPLETE_REFERENCE_PATTERN`.
