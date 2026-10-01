# error-localization — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001

## Hard-coded runtime user-facing titles (Access tree)

Lock: NO hard-coded Persian/English user-facing presentation text.

### Throw sites with FA titles (13)

| File | Code | HTTP | FA title (verbatim) |
| --- | --- | --- | --- |
| AdminPanelAccess | admin.tenant.missing | 503 | زمینهٔ فروشگاه در دسترس نیست. |
| AdminPanelAccess | admin.authorization.unavailable | 503 | سرویس مجوز در دسترس نیست. |
| AdminPanelAccess | admin.authorization.denied | 403 | دسترسی مدیریت این فروشگاه مجاز نیست. |
| AdminPanelAccess | admin.actor.missing | 401 | هویت مدیر احراز نشده است. |
| HostAdminPanelAccess | admin.tenant.missing | 503 | زمینهٔ فروشگاه در دسترس نیست. |
| HostAdminPanelAccess | admin.authorization.unavailable | 503 | سرویس مجوز در دسترس نیست. |
| HostAdminPanelAccess | admin.authorization.denied | 403 | دسترسی مدیریت marketplace مجاز نیست. |
| HostOrderAdminAuthorizer | order.authorization.unavailable | 503 | سرویس مجوز در دسترس نیست. |
| HostOrderAdminAuthorizer | order.operation.denied | 403 | مجوز انجام این عملیات وجود ندارد. |
| HostSupportAdminAuthorizer | support.authorization.unavailable | 503 | سرویس مجوز در دسترس نیست. |
| HostSupportAdminAuthorizer | admin.authorization.denied | 403 | مجوز پشتیبانی وجود ندارد. |
| HostWalletAdminAuthorizer | wallet.authorization.unavailable | 503 | سرویس مجوز در دسترس نیست. |
| HostWalletAdminAuthorizer | admin.authorization.denied | 403 | مجوز کیف پول/کارت هدیه وجود ندارد. |

Pass-through authorizers (Localization, OperatorProfile, Payment, Promotion, Return, Settlement, UserPreference) and EffectiveAccessReader: **no** local PlatformHttpException titles.

**Hardcoded-Title-Code-Count (unique stable codes):** 8  
`admin.actor.missing`, `admin.tenant.missing`, `admin.authorization.unavailable`, `admin.authorization.denied`, `order.authorization.unavailable`, `order.operation.denied`, `support.authorization.unavailable`, `wallet.authorization.unavailable`

Exception-Message-Classification-State: TITLES_USED_AS_USER_FACING_PRESENTATION (not exception.Message remapping elsewhere in Access)

## Error-code matrix

| Code | Producer(s) | HTTP | Descriptor owner | Localization key | Duplicate? | Hardcoded title? |
| --- | --- | --- | --- | --- | --- | --- |
| admin.actor.missing | AdminPanelAccess.ResolveActorUserId | 401 | **UNREGISTERED** (no Foundation catalog D()) | none found | no | YES |
| admin.tenant.missing | AdminPanelAccess + HostAdminPanelAccess | 503 | **UNREGISTERED** | none found | no | YES |
| admin.authorization.unavailable | AdminPanelAccess + HostAdminPanelAccess | 503 | **UNREGISTERED** | none found | no | YES |
| admin.authorization.denied | AdminPanelAccess + HostAdminPanelAccess + Support/Wallet capability | 403 | FoundationErrorCatalogContributor (`FoundationErrorCodes.AdminAuthorizationDenied`) | catalog-owned | Support/Wallet alias constants mirror same string | YES (title still hard-coded at throw) |
| order.authorization.unavailable | HostOrderAdminAuthorizer | 503 | Order.Endpoints OrderErrorCodes | module path | no | YES |
| order.operation.denied | HostOrderAdminAuthorizer | 403 | Order.Endpoints OrderErrorCodes | module path | no | YES |
| support.authorization.unavailable | HostSupportAdminAuthorizer | 503 | Support.Endpoints SupportAdminAuthorizationCodes | module path | mirrors SupportErrorCodes string | YES |
| wallet.authorization.unavailable | HostWalletAdminAuthorizer | 503 | Wallet.Endpoints WalletAdminAuthorizationCodes | module path | mirrors WalletErrorCodes string | YES |

Flags:

- Unregistered: admin.actor.missing, admin.tenant.missing, admin.authorization.unavailable
- Descriptor ownership OK for admin.authorization.denied (Foundation) but presentation still bypassed via PlatformHttpException title
- No SemanticException hard-coded titles in Access tree
- Localization-State: INCOMPLETE_FOR_PLATFORM_ADMIN_CODES (3 missing + titles still forced)

Do NOT fix in Analyze.
