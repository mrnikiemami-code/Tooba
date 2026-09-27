# Host/Admin remaining disposition (post-W20)

Inventory: 52 `*.cs` under `src/backend/Host/Tooba.Host/Admin`.
W20 complete: brand-options owned by Catalog; Host ProductWorkspace routes = 17.

True folder-empty is **not** attainable in one wave: KEEP platform seams + BLOCK StoreAppearance remain until Architect unlocks.

## Counts (file-level)

| Disposition | Count | Notes |
|---|---:|---|
| RETAIN_PARTIAL (ProductWorkspace*) | 4 | W21–W24 plan |
| MOVE → Catalog | ~24 | Landing, Menu, Demo, seeds, CatalogActor, attribute bootstrap |
| MOVE → Promotion | ~3 | Merchandising admin (MUST_SPLIT; Contracts for Offer/Pricing/Inventory/Party) |
| MOVE → Order/Payment/Cart (settings) | ~5 | Hold/Checkout/Reservation settings — split by owner |
| KEEP_AS_HOST_PLATFORM | ~12 | Host*Authorizer, AdminPanel*, AdminGrid*, DevActor, panel access |
| BLOCK_DEFERRED | 2 | StoreAppearance* (Host.Storefront projector coupling) |

## File map

| File | Disposition | Destination / blocker |
|---|---|---|
| ProductWorkspaceEndpoints.cs | RETAIN_PARTIAL | ProductWorkspace.Endpoints + Catalog commands (W21–W24) |
| ProductWorkspaceComposer.cs | RETAIN_PARTIAL | ProductWorkspace composition |
| ProductWorkspaceModels.cs | RETAIN_PARTIAL | ProductWorkspace |
| ProductWorkspaceDevelopmentBootstrap.cs | MOVE later | Catalog or ProductWorkspace Development |
| StoreLandingPageEndpoints.cs | MOVE | Catalog.Endpoints (Admin + Storefront pages) |
| StoreLandingPageComposer.cs | MOVE | Catalog.Application (or Endpoints composer → CQRS) |
| LandingPageDevelopmentSeed.cs | MOVE | Catalog Development |
| StoreMenuEndpoints.cs | MOVE | Catalog.Endpoints |
| StoreMenuComposer.cs | MOVE | Catalog.Application |
| StoreMenuDevelopmentSeed.cs | MOVE | Catalog Development |
| MerchandisingCampaignAdminEndpoints.cs | MOVE (MUST_SPLIT) | Promotion.Endpoints + Contracts readers |
| MerchandisingCampaignDevelopmentSeed.cs | MOVE | Promotion Development |
| MerchandisingStoreLandingReferenceGate.cs | KEEP thin OR MOVE | Host adapter for `IStoreLandingExternalReferenceGate` (Promotion→Catalog Contracts) |
| CatalogDemo/* (10) | MOVE | Catalog Development |
| CatalogAttributeSchemaDevelopmentBootstrap.cs | MOVE | Catalog Development |
| FashionTemplateCatalogSeed.cs | MOVE | Catalog Development |
| IndustryBatchA/B/CTemplateCatalogSeed.cs | MOVE | Catalog Development |
| HoldPolicySettingsEndpoints.cs | MUST_SPLIT | Payment + Cart + Order settings owners |
| CheckoutAbuseSettingsEndpoints.cs | MOVE | Order.Endpoints |
| CheckoutIdentitySettingsEndpoints.cs | MOVE | Order.Endpoints |
| ReservationPolicyAdminEndpoints.cs | MOVE | Order.Endpoints |
| ReservationPolicyAdminComposer.cs | MOVE | Order.Application |
| ReservationPolicyAdminModels.cs | MOVE | Order.Application |
| StoreAppearanceSettingsEndpoints.cs | BLOCK_DEFERRED | Host.Storefront projector coupling |
| StoreAppearanceSettingsComposer.cs | BLOCK_DEFERRED | same |
| Host*Authorizer.cs (7) + HostOrderAdminEffectiveAccessReader.cs | KEEP_AS_HOST_PLATFORM | thin security adapters |
| HostAdminPanelAccess.cs | KEEP_AS_HOST_PLATFORM | |
| AdminPanelAccess.cs | KEEP_AS_HOST_PLATFORM | shared require-auth helper (or relocate with panel) |
| AdminPanelEndpoints/Composer/Models.cs | KEEP_AS_HOST_PLATFORM | cross-module admin shell |
| AdminGridQueryEndpoint.cs | KEEP_AS_HOST_PLATFORM | grid transport |
| AdminDevActorBootstrap.cs | KEEP_AS_HOST_PLATFORM | Host Development |
| CatalogActorHttpBinding.cs | MOVE or KEEP | Catalog.Endpoints already has CatalogActorRequestBinding — retire Host duplicate when last Catalog Host route gone |

## Next migrate waves (file-removing)

1. **StoreLandingPage*** (+ LandingPageDevelopmentSeed if free) → Catalog
2. **StoreMenu*** (+ StoreMenuDevelopmentSeed) → Catalog
3. **CatalogDemo/** + template/attribute seeds → Catalog Development
4. **Checkout*/Reservation*** → Order; HoldPolicy MUST_SPLIT
5. **Merchandising*** → Promotion (after Contracts ports for member enrichment)
6. **ProductWorkspace W21–W24** → then delete Host PW shells
7. StoreAppearance only after Architect unlock

## Cannot empty yet

- StoreAppearance* (BLOCK)
- Host*Authorizer + AdminPanel* + AdminGrid* + DevActor (KEEP platform)
- Merchandising until cross-module join removed
- ProductWorkspace* until W24
