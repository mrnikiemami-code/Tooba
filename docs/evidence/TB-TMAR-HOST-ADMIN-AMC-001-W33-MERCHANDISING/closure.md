# W33-MERCHANDISING closure

## Outcome
Host Admin Merchandising evacuated into Promotion (AMC Migrate).

## Proof
- Host Admin deleted: Endpoints, DevelopmentSeed, StoreLandingReferenceGate
- Promotion owns MerchandisingCampaignAdminEndpoints via MapPromotionEndpoints
- Composer uses Contracts (ICatalogVariantLookup / IPartyLookup); no CatalogDbContext/PartyDbContext
- Seed in Promotion.Infrastructure/Development; ProductWorkspaceDevelopmentBootstrap calls relocated type
- Gate thin Host adapter: CatalogAdapters/MerchandisingStoreLandingReferenceGate
- HostPromotionAdminAuthorizer KEEP; module PromotionAdminAuthorizer over IAdminPanelAccess registered
- Host Admin `*.cs` count: **28**
- Guard: HostAdminAmcW33MerchandisingGuardTests

## SoT
- docs/architecture/tmar-current-state.json → hostAdminAmcMerchandising
