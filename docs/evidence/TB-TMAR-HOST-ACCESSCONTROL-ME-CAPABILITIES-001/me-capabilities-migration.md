# TB-TMAR-HOST-ACCESSCONTROL-ME-CAPABILITIES-001 — me/capabilities migration

Mode: exactly two closely-related routes moved to module-owned Endpoints + Application CQRS.

## Migrated routes

| Route | Before | After |
| --- | --- | --- |
| `GET /v1/admin/access-control/me/capabilities` | Host `AccessControlEndpoints.AdminMeCapabilitiesAsync` | `AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.MeCapabilitiesAsync` |
| `GET /v1/seller/access-control/me/capabilities` | Host `AccessControlEndpoints.SellerMeCapabilitiesAsync` | `AccessControl.Endpoints/Seller/AccessControlSellerEndpoints.MeCapabilitiesAsync` |

## A. Application query (shared, reusable)

`src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Queries/GetEffectiveAccess/GetEffectiveAccessQuery.cs`

- `GetEffectiveAccessQuery(Guid ActorUserId, AccessOwnerScopeKind OwnerScopeKind, Guid? OwnerScopeId, string? TenantId) : IRequest<EffectiveAccessDto>`
- `GetEffectiveAccessQueryHandler : IRequestHandler<GetEffectiveAccessQuery, EffectiveAccessDto>`
- Handler constructs `AccessOwnerScope(OwnerScopeKind, OwnerScopeId, TenantId)` and calls `IAccessControlDirectory.GetEffectiveAccessAsync`.
- Carries only trusted authorization/context-derived values; no HTTP types; no Host dependency.
- **Validator: NONE** (Actor/owner are authorization/context-derived for these two endpoints).

## B. Endpoints

- `AccessControlEndpointModule.MapAccessControlModuleEndpoints` now maps group `/v1/admin/access-control` (admin) and group `/v1/seller/access-control` (seller).
- Admin handler: `IAdminPanelAccess.RequireAuthorizedAsync(request, ct)` → `GetEffectiveAccessQuery(actor, Platform, null, tenant.Current?.TenantId.Value)` → `ISender`.
- Seller handler: `ISellerPanelAccess.RequireAuthorizedAsync(request, ct)` → `(actor, sellerId)` → `GetEffectiveAccessQuery(actor, Seller, sellerId, tenant.Current?.TenantId.Value)` → `ISender`.
- Response semantics preserved (`Results.Json(EffectiveAccessDto)`).
- No `IAccessControlDirectory` in Endpoints (verified ZERO); no `Tooba.Host.*` reference.

## C. Host

Removed from `Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs` only:

- `admin.MapGet("/me/capabilities", AdminMeCapabilitiesAsync);`
- `seller.MapGet("/me/capabilities", SellerMeCapabilitiesAsync);`
- `AdminMeCapabilitiesAsync` method
- `SellerMeCapabilitiesAsync` method

All other routes/handlers and shared helpers still used by remaining Host routes unchanged.

## D. Route ownership

- Production mapping for admin `me/capabilities`: **exactly one** (`AccessControlAdminEndpoints.cs:22`).
- Production mapping for seller `me/capabilities`: **exactly one** (`AccessControlSellerEndpoints.cs:20`).
- Both module-owned. Host handler/mapping residue for the two routes: **ZERO** (repo-wide search for `AdminMeCapabilitiesAsync` / `SellerMeCapabilitiesAsync` returns no matches).

## Validation

- `dotnet build Tooba.AccessControl.Endpoints.csproj --no-restore`: Build succeeded, 0 errors.
- `dotnet build Tooba.Host.csproj --no-restore`: Build succeeded, 0 errors.
- No tests added; no solution build; no retry loop.

## Boundaries honored

- roles, assignments, ceiling, users/effective, demo-preview, catalog/scope-resources untouched.
- `AccessControlDevelopmentSeed.cs`, `AccessControlDemoSnapshot.cs` untouched.
- Identity/OperatorProfile/Catalog boundaries, Fulfillment, Checkout, frontend untouched.
