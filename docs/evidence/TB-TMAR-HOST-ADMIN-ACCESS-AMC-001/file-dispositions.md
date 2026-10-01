# file-dispositions — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001

ANALYSIS_ONLY — one primary disposition per file.

| # | File | Disposition | Authority test | Notes |
| --- | --- | --- | --- | --- |
| 1 | AdminPanelAccess.cs | KEEP_AS_HOST_PLATFORM_ACCESS_SEAM | platform session/tenant/auth composition only | Static helper; DevActorHeader Development-only; FA titles debt |
| 2 | HostAdminPanelAccess.cs | KEEP_AS_HOST_PLATFORM_ACCESS_SEAM | platform + Marketplace Dev composition | Sole `IAdminPanelAccess` implementer in Program |
| 3 | HostLocalizationAdminAuthorizer.cs | KEEP_AS_THIN_HOST_MODULE_AUTH_ADAPTER | pass-through to IAdminPanelAccess | DI registered |
| 4 | HostOperatorProfileAdminAuthorizer.cs | KEEP_AS_THIN_HOST_MODULE_AUTH_ADAPTER | pass-through | DI registered |
| 5 | HostOrderAdminAuthorizer.cs | KEEP_AS_THIN_HOST_MODULE_AUTH_ADAPTER | panel gate + permission#check via IAuthorizationService | Module codes on capability fail; FA titles |
| 6 | HostOrderAdminEffectiveAccessReader.cs | KEEP_AS_THIN_HOST_MODULE_AUTH_ADAPTER | maps IPlatformEffectiveAccessReader → Order.Contracts | No AccessControl Application/Domain |
| 7 | HostPaymentAdminAuthorizer.cs | KEEP_AS_THIN_HOST_MODULE_AUTH_ADAPTER | pass-through | DI registered |
| 8 | HostPromotionAdminAuthorizer.cs | DEAD_ZERO_CONSUMER_RESIDUE | N/A | Not in Program; PromotionEndpointModule registers `PromotionAdminAuthorizer` |
| 9 | HostReturnAdminAuthorizer.cs | KEEP_AS_THIN_HOST_MODULE_AUTH_ADAPTER | pass-through | DI registered |
| 10 | HostSettlementAdminAuthorizer.cs | KEEP_AS_THIN_HOST_MODULE_AUTH_ADAPTER | pass-through | DI registered |
| 11 | HostSupportAdminAuthorizer.cs | KEEP_AS_THIN_HOST_MODULE_AUTH_ADAPTER | panel + capability | FA titles on unavailable/denied |
| 12 | HostUserPreferenceAdminAuthorizer.cs | KEEP_AS_THIN_HOST_MODULE_AUTH_ADAPTER | pass-through | DI registered |
| 13 | HostWalletAdminAuthorizer.cs | KEEP_AS_THIN_HOST_MODULE_AUTH_ADAPTER | panel + capability | FA titles on unavailable/denied |

No MOVE_TO_MODULE / MUST_SPLIT / BLOCKED_NEEDS_ARCHITECT_DECISION required for this analyze.
