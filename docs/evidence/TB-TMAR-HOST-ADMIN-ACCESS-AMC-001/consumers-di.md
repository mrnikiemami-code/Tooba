# consumers-di — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001

## AdminPanelAccess (static internal)

| Consumer kind | Location | Production? |
| --- | --- | --- |
| HostAdminPanelAccess.RequireAuthorizedAsync / ResolveActorUserId | Access/HostAdminPanelAccess.cs | YES — sole production consumer |
| AdminPanelAuthorizationTests / StoryFoundationTests / ContentPermissionEnforcementTests stubs | Host.Tests | TEST ONLY |
| Guards asserting absence of direct `AdminPanelAccess.RequireAuthorizedAsync` on module endpoints | Host.Tests Architecture/* | TEST ONLY |

**AdminPanelAccess-Direct-Consumer-State (production):** ONLY_VIA_HOST_ADMIN_PANEL_ACCESS

## HostAdminPanelAccess : IAdminPanelAccess

| Registration | `Program.cs` `AddScoped<IAdminPanelAccess, HostAdminPanelAccess>()` |
| --- | --- |
| Const consumer | `Development/MarketplaceAdminDevBootstrap.cs` → `HostAdminPanelAccess.MarketplacePlatformTenantId` |
| Interface consumers | Module.Endpoints Admin surfaces resolving `IAdminPanelAccess` (AccessControl, Media, CatalogAdminAuthorizer, ContentAdminAuthorizer, PageCompositionAdminAuthorizer, FulfillmentAdminAuthorizer, PromotionAdminAuthorizer, …) + Host thin Authorizers |

## Authorizer DI (Program.cs)

| Type | Registered? | Module interface |
| --- | --- | --- |
| HostOrderAdminAuthorizer | YES | IOrderAdminAuthorizer |
| HostOrderAdminEffectiveAccessReader | YES | IOrderAdminEffectiveAccessReader |
| HostPaymentAdminAuthorizer | YES | IPaymentAdminAuthorizer |
| HostUserPreferenceAdminAuthorizer | YES | IUserPreferenceAdminAuthorizer |
| HostOperatorProfileAdminAuthorizer | YES | IOperatorProfileAdminAuthorizer |
| HostLocalizationAdminAuthorizer | YES | ILocalizationAdminAuthorizer |
| HostSettlementAdminAuthorizer | YES | ISettlementAdminAuthorizer |
| HostReturnAdminAuthorizer | YES | IReturnAdminAuthorizer |
| HostSupportAdminAuthorizer | YES | ISupportAdminAuthorizer |
| HostWalletAdminAuthorizer | YES | IWalletAdminAuthorizer |
| HostPromotionAdminAuthorizer | **NO** | Module registers `PromotionAdminAuthorizer` in PromotionEndpointModule |

**Active-Authorizer-Count:** 10 (9 AdminAuthorizer + EffectiveAccessReader)  
**Dead-Authorizer-Count:** 1 (HostPromotionAdminAuthorizer)
