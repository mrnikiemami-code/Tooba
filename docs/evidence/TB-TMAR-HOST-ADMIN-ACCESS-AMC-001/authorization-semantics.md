# authorization-semantics — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001

## AdminPanelAccess vs HostAdminPanelAccess

| Question | Answer |
| --- | --- |
| Is duplication justified? | **Partial yes.** Single-Store path is delegated to AdminPanelAccess. Marketplace Dev path (no ICurrentTenant, synthetic `marketplace-platform`, Marketplace edition CallContext) must live on HostAdminPanelAccess. Authorization check body is intentionally parallel. |
| Is AdminPanelAccess consumed outside HostAdminPanelAccess in production? | **NO.** Only HostAdminPanelAccess (+ tests). |
| Can one become implementation detail of the other? | **YES.** Prefer AdminPanelAccess as private helper of HostAdminPanelAccess (or nest as private static) while keeping `IAdminPanelAccess` public BuildingBlocks seam. |
| Dead/duplicate auth logic? | Marketplace branch duplicates decision branching; not dead. HostPromotionAdminAuthorizer is dead DI residue (separate). |
| Does Marketplace Development require HostAdminPanelAccess Host-owned? | **YES.** Const `MarketplacePlatformTenantId` used by MarketplaceAdminDevBootstrap; edition gate uses Host `ControlPlaneRegistry`. |
| Minimum target shape for presentation debt? | Keep HostAdminPanelAccess as sole `IAdminPanelAccess`; register missing admin.* codes in Foundation catalog; throw PlatformHttpException **without** hard-coded titles (catalog/localizer resolve); preserve HTTP status + error codes + Allow/Unavailable/Deny semantics exactly. No auth model redesign. |

## Authorizer patterns

### Pass-through (thin)

Localization, OperatorProfile, Payment, Return, Settlement, UserPreference (+ dead Promotion file): `adminAccess.RequireAuthorizedAsync(request)`.

### Capability (still thin Host)

Order / Support / Wallet:

1. Panel gate via `IAdminPanelAccess`
2. `IAuthorizationService.CanAsync` permission#check with SingleStore CallContext
3. Fail-closed on Unavailable
4. Stable module/foundation codes on deny

No module Application/Domain policy ownership in Host.

### Effective access

`HostOrderAdminEffectiveAccessReader` maps `IPlatformEffectiveAccessReader` → `OrderAdminEffectiveAccess` DTO. AccessControl implementation stays behind BuildingBlocks seam.

## Auth decision path (panel)

Authenticated session UserId preferred; else Development-only `X-Tooba-Dev-Actor-User-Id`; else `admin.actor.missing` 401. Tenant required for Single-Store path; Marketplace Dev substitutes synthetic tenant id.
