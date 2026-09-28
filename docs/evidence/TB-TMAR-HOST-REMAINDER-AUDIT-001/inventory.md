# TB-TMAR-HOST-REMAINDER-AUDIT-001 — Inventory

Analysis only. **Zero production edits.** Admin remains untouched.

## Totals

| Metric | Value |
| --- | --- |
| Host production `.cs` files (excluding `bin/`/`obj/`) | 137 |
| `Admin/**` (certified, excluded from classification) | 15 |
| Non-Admin production `.cs` files | **122** |
| Top-level production areas outside `Admin` | **34** (33 folders + repo-root files) |

## Area inventory (file counts + foreign-layer import counts)

`app` = `using Tooba.<M>.Application`, `inf` = `...Infrastructure`, `dom` = `...Domain`,
`ctr` = `...Contracts`, `ep` = `...Endpoints`.

| Area | Files | app | inf | dom | ctr | ep |
| --- | --- | --- | --- | --- | --- | --- |
| `(root)` | 7 | 33 | 13 | 5 | 2 | 20 |
| `Admin` (certified) | 15 | 0 | 0 | 0 | 0 | 0 |
| `Authentication` | 9 | 0 | 0 | 0 | 6 | 0 |
| `Authorization` | 6 | 0 | 0 | 0 | 0 | 0 |
| `Caching` | 4 | 0 | 0 | 0 | 0 | 0 |
| `CatalogAdapters` | 3 | 6 | 0 | 2 | 0 | 0 |
| `Composition` | 2 | 0 | 45 | 0 | 0 | 0 |
| `Configuration` | 1 | 0 | 0 | 0 | 2 | 0 |
| `Development` | 12 | 18 | 52 | 7 | 15 | 0 |
| `Errors` | 2 | 0 | 0 | 0 | 0 | 0 |
| `Grid` | 8 | 4 | 3 | 3 | 3 | 0 |
| `Health` | 3 | 0 | 0 | 0 | 0 | 0 |
| `Jobs` | 1 | 0 | 0 | 0 | 0 | 0 |
| `Localization` | 2 | 2 | 1 | 1 | 0 | 0 |
| `Media` | 1 | 1 | 0 | 0 | 0 | 0 |
| `Messaging` | 6 | 0 | 0 | 0 | 0 | 0 |
| `MultiTenancy` | 1 | 0 | 0 | 0 | 1 | 0 |
| `Observability` | 1 | 0 | 0 | 0 | 0 | 0 |
| `OperatorProfile` | 1 | 1 | 0 | 0 | 0 | 0 |
| `Order` | 1 | 3 | 0 | 0 | 1 | 0 |
| `Outbox` | 3 | 0 | 0 | 0 | 2 | 0 |
| `PageComposition` | 2 | 2 | 0 | 1 | 0 | 0 |
| `Persistence` | 1 | 0 | 0 | 0 | 0 | 0 |
| `Preferences` | 2 | 2 | 0 | 1 | 0 | 0 |
| `ProductQnA` | 1 | 2 | 0 | 0 | 0 | 0 |
| `Reviews` | 2 | 3 | 2 | 1 | 0 | 0 |
| `Security` | 2 | 0 | 0 | 0 | 0 | 0 |
| `Seller` | 15 | 18 | 3 | 5 | 2 | 8 |
| `Settings` | 1 | 3 | 1 | 0 | 0 | 0 |
| `Storefront` | 12 | 18 | 11 | 11 | 13 | 1 |
| `Story` | 2 | 0 | 1 | 0 | 0 | 0 |
| `Support` | 1 | 2 | 3 | 0 | 0 | 0 |
| `Transport` | 3 | 0 | 0 | 0 | 0 | 0 |
| `Wallet` | 1 | 0 | 4 | 0 | 0 | 0 |
| `Wishlist` | 3 | 2 | 2 | 2 | 0 | 0 |

Repo-root production files: `Program.cs`, `CheckoutReservationHoldPolicy.cs`,
`CommerceHoldPolicy.cs`, `GlobalUsings.SettlementApp.cs`, `GlobalUsings.SettlementDomain.cs`,
`UnpaidOrderExpiryHostedService.cs`, `UnpaidOrderExpiryHostOptions.cs`.

## Route-owning areas (mapped HTTP endpoints)

| Area | File | Route group |
| --- | --- | --- |
| `Authentication` | `AuthenticationHttpBoundary.cs` | auth flow |
| `Health` | `HostHealthEndpoints.cs` | health/ready |
| `Localization` | `LocaleAdminEndpoints.cs` | `/v1/admin/locales` |
| `Media` | `MediaEndpoints.cs` | `/v1/admin/media` + public delivery |
| `OperatorProfile` | `OperatorProfileEndpoints.cs` | operator profile |
| `PageComposition` | `PageCompositionEndpoints.cs` | page composition |
| `Preferences` | `UiPreferenceEndpoints.cs`, `UserPreferenceEndpoints.cs` | preferences |
| `ProductQnA` | `ProductQnAEndpoints.cs` | QnA + bulk inquiry |
| `Reviews` | `ReviewEndpoints.cs` | reviews |
| `Seller` | `SellerPanelEndpoints.cs`, `SellerSettingsEndpoints.cs` | seller panel |
| `Storefront` | `StorefrontEndpoints.cs` | storefront |
| `Story` | `StoryEndpoints.cs` | story |
| `Wishlist` | `WishlistEndpoints.cs` | wishlist |
| `(root)` | `Program.cs` | app composition |

## Persistence residue (`DbContext` / `IQueryable` in Host)

23 files. Notably outside Development:

* `Grid/AdminReviewGridQueryEngine.cs`, `Grid/AdminStoryGridQueryEngine.cs`
* `Reviews/ReviewPanelComposer.cs`
* `Seller/SellerPanelComposer.cs`, `Seller/SellerDevActorBootstrap.cs`
* `Storefront/StorefrontComposer.cs`, `Storefront/CheckoutIdentityGate.cs`,
  `Storefront/FashionTemplatePreviewQuery.cs`, `Storefront/IndustryTemplatePreviewQuery.cs`,
  `Storefront/StorefrontDemoCatalogBootstrap.cs`
* `Story/StoryPanelComposer.cs`
* `Wishlist/WishlistDevelopmentSeed.cs`
* `Localization/ContentLanguageReferenceGuard.cs`
* `Health/HostReadinessEvaluator.cs`
* `CommerceHoldPolicy.cs`, `Program.cs`

`SaveChanges` / transaction residue outside Development:

* `Storefront/StorefrontDemoCatalogBootstrap.cs:184,387`
* `Wishlist/WishlistDevelopmentSeed.cs:30`
* `Development/ProductWorkspaceDevelopmentBootstrap.cs:423,424`
* `Program.cs:152` (outbox interceptor registration — platform)

## Service locator residue (`HttpContext.RequestServices`, runtime paths)

| File | Count |
| --- | --- |
| `Seller/HostPromotionSellerAuthorizer.cs` | 3 (compressed line) |
| `Seller/HostOfferSellerAuthorizer.cs` | 3 |
| `Seller/HostOrderSellerAuthorizer.cs` | 3 |
| `Seller/HostReturnSellerAuthorizer.cs` | 3 |
| `Seller/HostNotificationSellerAuthorizer.cs` | 3 |
| `Seller/HostSettlementSellerAuthorizer.cs` | 3 |
| `Seller/HostSupportSellerAuthorizer.cs` | 3 |
| `Storefront/HostPaymentStorefrontAuthorizer.cs` | 1 |
| `MultiTenancy/TenantResolutionMiddleware.cs` | 1 (middleware — lawful) |
| `Authentication/SessionAuthenticationMiddleware.cs` | 1 (middleware — lawful) |
| `Authentication/AuthenticationHttpProblem.cs` | 1 (problem factory — lawful) |

## Semantic-error residue

Message-text classification / leak:

* `PageComposition/PageCompositionEndpoints.cs:211-213,222` — `ex.Message.Contains("یافت نشد"/"کاتالوگ"/"ممنوع"/"ناشناخته")` used as machine classification.
* `Story/StoryEndpoints.cs:52-53` — `ex.Message.Contains("یافت نشد"/"ناامن")` used as machine classification.
* `Reviews/ReviewEndpoints.cs:58` — `ex.Message.Contains("قبلاً")` used as machine classification.
* `Localization/LocaleAdminEndpoints.cs:171` — `errorCode = ex.Message` (message as machine code).
* `Authorization/SpiceDbAuthorizationAdapter.cs:170` — `ex.Message == "authorization.unavailable"` (internal adapter sentinel, Host-owned, low risk).

`title = ex.Message` (display-only) also appears in `OperatorProfile`, `Preferences`,
`Seller*`, `Reviews`, `PageComposition`, `Story`, `Grid/AdminListGridQueryPolicy`.

## Development-only residue

`Development/**` (12), `Settings/SettingsFoundationDevelopmentSeed.cs`,
`Support/SupportDevelopmentSeedHost.cs`, `Wallet/WalletDevelopmentSeedHost.cs`,
`Composition/ContentDevelopmentSeedHost.cs`, plus Dev bootstraps in `Seller/` and mirrored
dev-actor helpers in `Admin/` (certified).

## Existing architecture guards

`src/backend/Host/Tooba.Host.Tests/Architecture/`: 69 guard files — `Admin` waves
CANON-001..009 + certification, plus AMC waves W1..W36, AddressBook/CustomerProfile/
Content/Cart/Caching/Development AMC guards, and `TmarCompleteReferenceStructureGateTests`.

## Host project reference posture

`Tooba.Host.csproj` references module `Infrastructure` + `Endpoints` projects (composition
root), so foreign `*.Infrastructure` imports inside Host are structurally legal only for DI
composition; business reads via `DbContext` remain out-of-boundary.
