# TB-TMAR-HOST-REMAINDER-AUDIT-001 — Classification

Every top-level production area outside `Admin` is classified as exactly one of:
`PLATFORM_KEEP` / `READY_TO_MIGRATE` / `NEEDS_PRECERT_REPAIR` /
`GLOBAL_HOST_BOUNDARY_REVIEW` / `DEVELOPMENT_ONLY_REVIEW`.

Admin: `CANONICAL_PLATFORM_BOUNDARY_CERTIFIED` (verified untouched, not classified).

## PLATFORM_KEEP

Platform composition, cross-cutting infrastructure, or Host-owned abstractions. No business
ownership, no module business reads.

| Area | Files | Rationale |
| --- | --- | --- |
| `Authorization` | 6 | SpiceDB adapters + schema hosted service + host options + instrumentation. Zero foreign layer imports. |
| `Caching` | 4 | `MemoryToobaCache`, cache registration/instrumentation/options. Architecture-neutral. |
| `Messaging` | 6 | In-process / MassTransit / disabled publishers + options + retry configurator. |
| `Outbox` | 3 | `OutboxDispatcher` + host options + worker seams (`Contracts`-only: 2). |
| `Transport` | 3 | SQL transport options mapper + integration transport consumer/message. |
| `Jobs` | 1 | `BackgroundWorkerRegistry` — worker lifecycle registry. |
| `Persistence` | 1 | `DatabaseConnectionResolver` — connection resolution abstraction. |
| `Configuration` | 1 | `ToobaPlatformOptions` — platform options + validation. |
| `Health` | 3 | Health endpoint, readiness evaluator, SpiceDB probe. `DbContext` use is a readiness probe (platform lawful). |
| `Observability` | 1 | Request observability enrichment middleware. |
| `Security` | 2 | Security headers middleware + auth security options. |
| `Errors` | 2 | `PlatformExceptionMapper` + `ToobaExceptionHandler` — global exception boundary. |
| `MultiTenancy` | 1 | `TenantResolutionMiddleware` — tenant resolution; `RequestServices` in middleware is the canonical ASP.NET pattern. |
| `Authentication` | 9 | **Global Host platform boundary** (locked decision). `Contracts`-only (6) + `IIdentitySessionResolver` seam; `RequestServices` appears only in middleware/problem-factory. Drift check: PASS — no `Identity.Infrastructure`, no `Identity.Application`. |
| `Grid` (partial) | 4 | `BoundedListGridQueryEngine`, `InMemoryGridField`, `InMemoryGridFieldKind`, `AdminListGridQueryPolicy` — generic grid primitives. |
| `Composition/ToobaModuleComposition.cs` | 1 | Composition root aggregating per-module `Add*Module()`; `Infrastructure` imports are structurally required for DI wiring. |

## READY_TO_MIGRATE

Business endpoint surfaces (module-owned routes/use cases currently living in Host) with a clear,
already-existing module destination.

| Area | Files | Module destination | Residue |
| --- | --- | --- | --- |
| `Media` | 1 | `Tooba.Media.Endpoints` | `Media.Application` import; `/v1/admin/media` + binary delivery routes |
| `OperatorProfile` | 1 | `Tooba.OperatorProfile.Endpoints` | `OperatorProfile.Application`; `title = ex.Message` |
| `Wishlist` | 3 | `Tooba.Wishlist.Endpoints` (+ dev seed to Infrastructure) | `Wishlist.Application/Domain/Infrastructure`, `Catalog.Domain`, `Catalog.Infrastructure.Persistence`, `SaveChangesAsync` |
| `ProductQnA` | 1 | `Tooba.ProductQnA.Endpoints` + `Tooba.BulkInquiry.Endpoints` | two `*.Application` imports in one Host endpoint file |
| `Preferences` | 2 | `Tooba.UserPreference.Endpoints` | `UserPreference.Application/Domain`; `title = ex.Message` |
| `CatalogAdapters` | 3 | `Tooba.Catalog` / `Tooba.Promotion` / `Tooba.Content` | `Catalog.Application`, `Promotion.Application`, `Promotion.Domain`, `Content.Domain.Rules` |
| `Composition/ContentDevelopmentSeedHost.cs` | 1 | `Tooba.Content.Infrastructure` (dev seed) | `Content.Infrastructure*`, `Localization.Infrastructure.Persistence`, `Media.Infrastructure.Persistence` |
| `Support/SupportDevelopmentSeedHost.cs` | 1 | `Tooba.Support.Infrastructure` (dev seed) | `Support.Infrastructure*`, `AccessControl.Application.*` |
| `Wallet/WalletDevelopmentSeedHost.cs` | 1 | `Tooba.Wallet.Infrastructure` (dev seed) | `Wallet.Infrastructure*` (4) |

## NEEDS_PRECERT_REPAIR

Business surfaces with semantic-error / message-parsing / persistence defects that must be fixed
**before** evacuation can be certified (the defects are not module-behavior-preserving as-is).

| Area | Files | Blockers |
| --- | --- | --- |
| `PageComposition` | 2 | Message-text classification (`ex.Message.Contains(...)` for 404/400/403) at lines 211-213; Persian-message-derived machine codes; `PageComposition.Domain` import; `PageCompositionPanelComposer`. |
| `Story` | 2 | Message-text classification at lines 52-53 (`"یافت نشد"`, `"ناامن"`); `Story.Infrastructure.Persistence` hub in `StoryPanelComposer`. |
| `Reviews` | 2 | Message-text classification at line 58 (`"قبلاً"`) for duplicate detection; `ReviewPanelComposer` holds `Catalog.Infrastructure.Persistence` + `Reviews.Infrastructure.Persistence` joins; `Reviews.Domain` import. |
| `Localization` | 2 | `LocaleAdminEndpoints.cs:171` sets `errorCode = ex.Message`; `ContentLanguageReferenceGuard` holds `DbContext`. |
| `Wishlist` | 3 | (also READY) — `WishlistDevelopmentSeed` `SaveChangesAsync` + `Catalog.Infrastructure.Persistence`. |
| `Grid/AdminReviewGridQueryEngine.cs`, `Grid/AdminStoryGridQueryEngine.cs`, `Grid/AdminSellersGridQueryEngine.cs`, `Grid/AdminListGridPolicies.cs` | 4 | Persistence read engines for Reviews/Story/Sellers grids; `Catalog.Domain`/`Infrastructure`/`Application` imports. Migrate only with their owning module. |

## GLOBAL_HOST_BOUNDARY_REVIEW

Root-level Host files that own cross-module policy with heavy foreign Application/Infrastructure
coupling — these are architectural decisions, not simple evacuations.

| File | Blockers |
| --- | --- |
| `CheckoutReservationHoldPolicy.cs` | 20 foreign imports: `Order.Application.*` (9 namespaces), `Payment.Infrastructure.*` (5). |
| `CommerceHoldPolicy.cs` | 15 foreign imports: `Catalog.Domain`, `Catalog.Infrastructure.Persistence`, `Order.Application.*` (6), `Payment.Infrastructure.Providers`. |
| `UnpaidOrderExpiryHostedService.cs` | `Order.Application.*` (7 namespaces) — hosted service implementing Order reservation-expiry business policy in Host. |
| `Program.cs` | Composition root + 5 foreign imports incl. `Localization.Application`, `Catalog.Application.Development.CatalogDemo`, `Content.Infrastructure.Development`, `Offer.Infrastructure.Adapters.Tracing`. |
| `GlobalUsings.SettlementApp.cs`, `GlobalUsings.SettlementDomain.cs` | Settlement global usings injected into Host compilation. |
| `UnpaidOrderExpiryHostOptions.cs` | Platform options for the above hosted service. |

## DEVELOPMENT_ONLY_REVIEW

| Area | Files | Blockers |
| --- | --- | --- |
| `Development` | 12 | `Catalog.Infrastructure.Development`, `Identity/Order/Party/Payment/Settlement/Notification/AccessControl.Infrastructure.Persistence`, `Catalog/Inventory/Party/Pricing/Tax Application+Domain`; `ProductWorkspaceDevelopmentBootstrap` performs `SaveChangesAsync` on catalog+party DBs; `CatalogAttributeSchemaSellableEnricher` reaches `Party.Infrastructure.Persistence`. |
| `Seller/SellerDevActorBootstrap.cs` | 1 | `Identity.Infrastructure`, `Party.Application/Domain/Infrastructure.Persistence`. |
| `Settings/SettingsFoundationDevelopmentSeed.cs` | 1 | `OperatorProfile/Party/UserPreference.Application`, `Party.Infrastructure.Persistence`. |

## Seller (mixed)

`Seller` (15 files) is **GLOBAL_HOST_BOUNDARY_REVIEW / NEEDS_PRECERT_REPAIR** as a whole:

* 7 seller authorizers use `HttpContext.RequestServices.GetRequiredService(...)` service-locator
  anti-pattern (the exact CANON-002/003-class defect already repaired for Admin).
* `HostSellerOrderViewAccessReader.cs` imports `AccessControl.Application`, `AccessControl.Domain`,
  `AccessControl.Application.Models`, `AccessControl.Application.Permissions` (the exact
  CANON-006-class defect).
* `HostSupportSellerAuthorizer.cs` imports `AccessControl.Application/Domain` + `Support.Application.Errors`
  (the exact CANON-009-class defect).
* `SellerPanelEndpoints.cs`, `SellerSettingsEndpoints.cs`, `SellerPanelComposer.cs` import
  `Catalog.Application/Domain/Infrastructure.Persistence`, `Party.Application`,
  `Order.Application.Seller.Queries.*`, `AccessControl.Application/Domain` — business HTTP +
  persistence in Host.
* `SellerPanelModels.cs`, `HostSellerPanelAccess.cs`, `SellerPanelAccess.cs` are candidate
  `PLATFORM_KEEP`/thin-adapter files that only become certifiable once the above are repaired.

## Storefront (global)

`Storefront` (12 files) is **NEEDS_PRECERT_REPAIR + GLOBAL_HOST_BOUNDARY_REVIEW**:

* 18 `*.Application`, 11 `*.Infrastructure`, 11 `*.Domain` imports.
* `StorefrontComposer.cs`, `StorefrontDemoCatalogBootstrap.cs`, `FashionTemplatePreviewQuery.cs`,
  `IndustryTemplatePreviewQuery.cs`, `CheckoutIdentityGate.cs` perform `DbContext` reads and
  `SaveChangesAsync` on catalog persistence.
* `StorefrontDemoCatalogBootstrap.cs` is a demo-data bootstrap that belongs to module
  Infrastructure/Development.
* `HostPaymentStorefrontAuthorizer.cs` uses `RequestServices` service locator.

## Summary counts

| Classification | Areas |
| --- | --- |
| PLATFORM_KEEP | 16 (Authorization, Caching, Messaging, Outbox, Transport, Jobs, Persistence, Configuration, Health, Observability, Security, Errors, MultiTenancy, Authentication, Grid-primitives, Composition/ToobaModuleComposition) |
| READY_TO_MIGRATE | 9 (Media, OperatorProfile, Wishlist, ProductQnA, Preferences, CatalogAdapters, ContentDevelopmentSeedHost, SupportDevelopmentSeedHost, WalletDevelopmentSeedHost) |
| NEEDS_PRECERT_REPAIR | 6 (PageComposition, Story, Reviews, Localization, Grid-admin-engines, Wishlist) |
| GLOBAL_HOST_BOUNDARY_REVIEW | 7 (CheckoutReservationHoldPolicy, CommerceHoldPolicy, UnpaidOrderExpiryHostedService, Program, GlobalUsings.Settlement*, Seller, Storefront) |
| DEVELOPMENT_ONLY_REVIEW | 3 (Development, SellerDevActorBootstrap, SettingsFoundationDevelopmentSeed) |

## Authentication drift verdict

`KEEP_AS_GLOBAL_HOST_PLATFORM_BOUNDARY` **stands**. `Authentication/**` imports only
`Tooba.Identity.Contracts` (6 references) plus its own `IIdentitySessionResolver` seam; there is
**no** `Tooba.Identity.Application`, `.Infrastructure`, or `.Domain` import. No ownership reopen.
