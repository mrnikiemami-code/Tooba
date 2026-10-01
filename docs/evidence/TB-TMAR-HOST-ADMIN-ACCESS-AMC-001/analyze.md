# analyze — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001

ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED

## Folder enumeration (disk-verified)

Path: `src/backend/Host/Tooba.Host/Admin/Access/`

| Moment | Production `*.cs` count | Notes |
| --- | --- | --- |
| Start | 13 | 2 root + 11 Authorizers |
| End | 13 | unchanged (analysis only) |

### Access/ (namespace `Tooba.Host.Admin.Access`)

| File | Visibility | Role |
| --- | --- | --- |
| AdminPanelAccess.cs | `internal static` | Single-Store tenant#view panel gate + Dev actor resolve |
| HostAdminPanelAccess.cs | `internal sealed` : `IAdminPanelAccess` | Public Host DI seam; Marketplace Dev synthetic tenant branch |

### Access/Authorizers/ (namespace `Tooba.Host.Admin.Access.Authorizers`)

| File | Visibility | Interface |
| --- | --- | --- |
| HostLocalizationAdminAuthorizer.cs | public | `ILocalizationAdminAuthorizer` |
| HostOperatorProfileAdminAuthorizer.cs | public | `IOperatorProfileAdminAuthorizer` |
| HostOrderAdminAuthorizer.cs | internal | `IOrderAdminAuthorizer` |
| HostOrderAdminEffectiveAccessReader.cs | internal | `IOrderAdminEffectiveAccessReader` |
| HostPaymentAdminAuthorizer.cs | public | `IPaymentAdminAuthorizer` |
| HostPromotionAdminAuthorizer.cs | public | `IPromotionAdminAuthorizer` |
| HostReturnAdminAuthorizer.cs | public | `IReturnAdminAuthorizer` |
| HostSettlementAdminAuthorizer.cs | public | `ISettlementAdminAuthorizer` |
| HostSupportAdminAuthorizer.cs | public | `ISupportAdminAuthorizer` |
| HostUserPreferenceAdminAuthorizer.cs | public | `IUserPreferenceAdminAuthorizer` |
| HostWalletAdminAuthorizer.cs | public | `IWalletAdminAuthorizer` |

Host/Admin recursive production file count remains **18**. Admin/Grid directory **ABSENT**.

## Disposition summary

| Disposition | Count | Files |
| --- | --- | --- |
| KEEP_AS_HOST_PLATFORM_ACCESS_SEAM | 2 | AdminPanelAccess, HostAdminPanelAccess |
| KEEP_AS_THIN_HOST_MODULE_AUTH_ADAPTER | 10 | all Authorizers except Promotion |
| DEAD_ZERO_CONSUMER_RESIDUE | 1 | HostPromotionAdminAuthorizer (no Program DI; module owns PromotionAdminAuthorizer) |

## Decisive findings

1. **Platform seam justified:** `HostAdminPanelAccess` must stay Host-owned (Marketplace Dev `marketplace-platform` tenant + `ControlPlaneRegistry` edition branch; consumed by MarketplaceAdminDevBootstrap).
2. **AdminPanelAccess** is production-consumed only by `HostAdminPanelAccess` (static helper). Tests call it directly. Can become private implementation detail without redesigning auth.
3. **Duplication:** Marketplace branch re-implements tenant#view check (parallel to AdminPanelAccess Single-Store path) — justified for edition/tenant absence; presentation debt duplicated.
4. **Hard-coded FA titles:** 13 `PlatformHttpException` sites across 5 files; **8 unique stable codes** currently carry hard-coded titles.
5. **Unregistered foundation codes:** `admin.actor.missing`, `admin.tenant.missing`, `admin.authorization.unavailable` have no Foundation catalog entry; only `admin.authorization.denied` is registered.
6. **Authorizer family:** 7 pure pass-through; Order/Support/Wallet add capability checks via neutral `IAuthorizationService` (thin, not module business ownership); Promotion Host adapter is dead DI residue.
7. **Access = NOT_CERTIFIED_BY_PANEL_CERT.** Stale guard comments claiming whole Host/Admin CERTIFIED are metadata debt only.

## Certified protection (untouched)

Panel CERT, Admin/Development CERT, Party sellers CERT, Admin/Grid HOST_ZERO preserved. Production code change = NONE.
