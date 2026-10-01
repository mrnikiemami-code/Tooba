# dependency-boundary — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001

| File | Foreign dependency | Layer | Allowed? | Reason |
| --- | --- | --- | --- | --- |
| AdminPanelAccess.cs | Tooba.BuildingBlocks (session/tenant/auth/PlatformHttpException) | BuildingBlocks | YES | Neutral platform auth |
| HostAdminPanelAccess.cs | BuildingBlocks + ControlPlaneRegistry | BuildingBlocks/Host ed. | YES | Platform Marketplace branch |
| HostLocalizationAdminAuthorizer.cs | Localization.Endpoints.Admin + BuildingBlocks.Security | Endpoints seam | YES | Thin adapter pattern |
| HostOperatorProfileAdminAuthorizer.cs | OperatorProfile.Endpoints.Admin + BB.Security | Endpoints seam | YES | Thin |
| HostOrderAdminAuthorizer.cs | Order.Endpoints(+Errors) + BB | Endpoints codes | YES | Edge adapter; no Application |
| HostOrderAdminEffectiveAccessReader.cs | Order.Contracts.Admin.Operations + BB.Security IPlatformEffectiveAccessReader | Contracts | YES | Contracts-only map |
| HostPaymentAdminAuthorizer.cs | Payment.Endpoints.Admin + BB.Security | Endpoints seam | YES | Thin |
| HostPromotionAdminAuthorizer.cs | Promotion.Endpoints.Admin + BB.Security | Endpoints seam | YES* | Allowed deps but **DEAD DI** |
| HostReturnAdminAuthorizer.cs | Returns.Endpoints.Admin + BB.Security | Endpoints seam | YES | Thin |
| HostSettlementAdminAuthorizer.cs | Settlement.Endpoints.Admin + BB.Security | Endpoints seam | YES | Thin |
| HostSupportAdminAuthorizer.cs | Support.Endpoints.Admin + BB (+FoundationErrorCodes) | Endpoints+BB | YES | Thin capability |
| HostUserPreferenceAdminAuthorizer.cs | UserPreference.Endpoints.Admin + BB.Security | Endpoints seam | YES | Thin |
| HostWalletAdminAuthorizer.cs | Wallet.Endpoints.Admin + BB (+FoundationErrorCodes) | Endpoints+BB | YES | Thin capability |

## Forbidden scan result

| Forbidden | State |
| --- | --- |
| Foreign .Application | ZERO |
| Foreign .Infrastructure | ZERO |
| Foreign .Domain | ZERO |
| Foreign DbContext | ZERO |
| Cross-module persistence / entity joins | ZERO |
| Module service-locator | ZERO |

Contracts-Boundary-State: CLEAN_ENDPOINTS_OR_CONTRACTS_ONLY  
Foreign-Application/Infrastructure/Domain/DbContext/Cross-Module-Persistence: ZERO
