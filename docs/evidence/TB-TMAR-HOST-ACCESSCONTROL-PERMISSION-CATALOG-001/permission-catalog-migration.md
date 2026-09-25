# TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001 — permission-catalog migration

Mode: evacuate only the Permission Catalog HTTP family. No other route family touched.

## Before / after route ownership

| Route | Before | After |
| --- | --- | --- |
| `GET /v1/admin/access-control/permissions` | Host `AccessControlEndpoints.AdminListCatalogAsync` | `AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.ListPermissionsAsync` |
| `GET /v1/seller/access-control/permissions` | Host `AccessControlEndpoints.SellerListCatalogAsync` | `AccessControl.Endpoints/Seller/AccessControlSellerEndpoints.ListPermissionsAsync` |

## Query / handler path (real MediatR 12.5 CQRS)

- Admin: `Tooba.AccessControl.Application/Queries/ListPermissionCatalog/ListPermissionCatalogQuery.cs`
  - `ListPermissionCatalogQuery : IRequest<IReadOnlyList<PermissionDefinition>>` + `ListPermissionCatalogQueryHandler`.
  - Handler delegates to `IAccessControlDirectory.ListCatalog()` (which returns `PermissionCatalog.All`) — the existing single catalog authority.
- Seller: `Tooba.AccessControl.Application/Queries/ListSellerPermissionCatalog/ListSellerPermissionCatalogQuery.cs`
  - `ListSellerPermissionCatalogQuery(Guid SellerPartyId) : IRequest<IReadOnlyList<SellerPermissionCatalogItem>>` + handler.
  - Handler calls `GetSellerCeilingAsync(SellerPartyId, ct)` then projects `ListCatalog()` with the same ceiling/disabled/platform-only logic that Host previously performed inline.

`PermissionCatalog` itself remains owned by `AccessControl.Application` and was **not** relocated, duplicated, or wrapped in a facade.

## Admin endpoint path

`AccessControlAdminEndpoints.ListPermissionsAsync`:
`IAdminPanelAccess.RequireAuthorizedAsync` → `AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, ct)` → `sender.Send(new ListPermissionCatalogQuery())` → `Results.Json(...)`.

## Seller endpoint path

`AccessControlSellerEndpoints.ListPermissionsAsync`:
`ISellerPanelAccess.RequireAuthorizedAsync` (yields actor + sellerId) → `AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, ct)` → `sender.Send(new ListSellerPermissionCatalogQuery(sellerId))` → `Results.Json(...)`.

## Host removals

Removed from `Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs`:
- `admin.MapGet("/permissions", AdminListCatalogAsync);`
- `seller.MapGet("/permissions", SellerListCatalogAsync);`
- `AdminListCatalogAsync` method
- `SellerListCatalogAsync` method

Shared helpers still used by remaining Host routes were kept (only those two methods were dead after removal).

## Authorization parity

Unchanged ordering: panel authorization first, then `AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", ...)`, then data read. Fail-open `accesscontrol.view` policy was intentionally not modified.

## Response parity

- Admin: `IReadOnlyList<PermissionDefinition>` with `PermissionId`, `Module`, `DisplayNameKey`, `DescriptionKey`, `Delegable`, `ScopeKinds` — same catalog ordering (`PermissionCatalog.All`).
- Seller: `SellerPermissionCatalogItem` with the same property names as the previous anonymous object (`PermissionId`, `Module`, `DisplayNameKey`, `DescriptionKey`, `Delegable`, `ScopeKinds`, `DisabledByCeiling`, `PlatformOnly`), same projection semantics and ordering. JSON payload shape preserved.

## Validator classification

`NO_VALIDATOR_REQUIRED` — `ListPermissionCatalogQuery` has no transport input (parameterless); `ListSellerPermissionCatalogQuery` carries only the trusted `SellerPartyId` derived from the seller panel authorization seam, not user-controlled transport input. No ceremonial validators added.

## Single-route-ownership proof

- Host `/permissions` route mappings for this family: **ZERO** (remaining `/permissions` matches are `roles/{roleId}/permissions`, a different family).
- Host `AdminListCatalogAsync` / `SellerListCatalogAsync`: **ZERO**.
- Module admin `"/permissions"` mapping: **EXACTLY ONE**.
- Module seller `"/permissions"` mapping: **EXACTLY ONE**.
- New queries sent through `ISender`: yes.
- New handlers are real MediatR `IRequestHandler`: yes.
- `AccessControl.Endpoints` direct `IAccessControlDirectory` usage: **ZERO**.
- Duplicate `PermissionCatalog` implementation: none.

## Boundary audit

- `AccessControl.Application` → `Tooba.Host.*`: ZERO.
- `AccessControl.Application` → `Tooba.AccessControl.Endpoints`: ZERO.
- `AccessControl.Application` → `Microsoft.AspNetCore.*`: ZERO.

## Focused builds

- `dotnet build Tooba.AccessControl.Application.csproj --no-restore`: Build succeeded, 0 errors.
- `dotnet build Tooba.AccessControl.Endpoints.csproj --no-restore`: Build succeeded, 0 errors.
- `dotnet build Tooba.Host.csproj --no-restore`: Build succeeded, 0 errors.

Focused tests: none run — no existing focused test directly covers this route ownership or the new CQRS wiring.
No solution-wide build; no broad suite; no retry loop.

## Residual Host AccessControl families (still in Host, out of scope)

- role routes (admin/seller/admin-seller), role permission routes
- assignment routes
- ceiling routes (admin-seller ceiling, seller ceiling)
- user search / users/{id}/effective routes
- demo-preview (DEV-only)
- catalog scope-resource routes (categories/brands/products) and deferred scope routes
- `EnrichUserHitsAsync`, `AccessControlDevelopmentSeed.cs`, `AccessControlDemoSnapshot.cs`

## Status honesty

AccessControl is **NOT** marked COMPLETE_REFERENCE_PATTERN and **NOT** structure-certified by this task. Host AccessControl residue is not claimed as ZERO. AccessControl remains IN_PROGRESS.

## Protected state

Checkout untouched (PAUSED_AT_SAFE_W5_CHECKPOINT). Frontend frozen — no production changes. No migrations/schema changes. Fulfillment untouched.
