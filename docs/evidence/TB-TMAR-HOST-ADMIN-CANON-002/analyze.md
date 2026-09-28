# TB-TMAR-HOST-ADMIN-CANON-002 — Analyze

## Scope

Canonicalize ONLY the simple Host Admin endpoint-authorizer adapters so they delegate
through the existing Host platform seam `IAdminPanelAccess` instead of resolving
session/tenant/guard/environment from `HttpContext.RequestServices`.

In-scope files:

- `HostPaymentAdminAuthorizer.cs`
- `HostPromotionAdminAuthorizer.cs`
- `HostReturnAdminAuthorizer.cs`
- `HostSettlementAdminAuthorizer.cs`
- `HostAdminPanelAccess.cs` (platform implementation, verified/confirmed)

## Pre-state

| Adapter | Pre-state |
| --- | --- |
| Payment | 4× `RequestServices.GetRequiredService` + `AdminPanelAccess.RequireAuthorizedAsync` |
| Promotion | 4× `RequestServices.GetRequiredService` + `AdminPanelAccess.RequireAuthorizedAsync` |
| Return | 4× `RequestServices.GetRequiredService` + `AdminPanelAccess.RequireAuthorizedAsync` |
| Settlement | 5× `RequestServices.GetRequiredService` + duplicated SingleStore/Marketplace synthetic decision logic + `MarketplacePlatformTenantId` const |

## Duplication analysis (Settlement)

`HostSettlementAdminAuthorizer` duplicated, verbatim, the decision body of
`HostAdminPanelAccess.RequireAuthorizedAsync`:

1. `tenant.Current is not null` → `AdminPanelAccess.RequireAuthorizedAsync(...)`
2. `!environment.IsDevelopment() || registry.Edition != ToobaEdition.Marketplace` → 503 `admin.tenant.missing`
3. `AdminPanelAccess.ResolveActorUserId(...)` for the synthetic deviation actor
4. `guard.AuthorizeUseCaseAsync(Tenant/MarketplacePlatformTenantId, View, Edition=Marketplace)`
5. Allow → actor; Unavailable → 503 `admin.authorization.unavailable`; else 403 `admin.authorization.denied`

`HostAdminPanelAccess` contains the identical sequence, including the same error codes
and the same `MarketplacePlatformTenantId = "marketplace-platform"` value. Therefore
behavior parity is provable and convergence is required by the task.

## Posture decisions

- `HostAdminPanelAccess` remains the single Host platform gate and is NOT moved or split.
- `MarketplacePlatformTenantId` is consolidated onto `HostAdminPanelAccess` (the platform
  seam implementation). `MarketplaceAdminDevBootstrap` (Development orchestration, explicitly
  not out-of-scope-listed) was updated to reference the platform constant.
- Order/Support/Wallet authorizers, `AdminPanelComposer`, `AdminPanelEndpoints`,
  `AdminGridQueryEndpoint`, `AdminDevActorBootstrap` were NOT touched.

## File count

`Host/Admin` before = 15, planned after = 15 (no file added/removed in the folder).
