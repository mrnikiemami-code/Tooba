# Host Architecture Audit

Scope: all `src/backend/Host/Tooba.Host/**/*.cs` (216 files). Examples are not scope limits.

## Classification counts (files may have multiple tags)

- **read-composition**: 84
- **direct-dbcontext-read**: 81
- **auth-session**: 63
- **unclear/needs-design**: 55
- **endpoint-mapping**: 54
- **direct-dbcontext-write**: 53
- **seed-dev**: 31
- **SaveChanges**: 31
- **business-decision**: 13
- **serialization-errors**: 12
- **caching**: 6
- **di-composition**: 5
- **middleware**: 4
- **transaction**: 2

## Direct write / SaveChanges candidates (undefined)

- `src/backend/Host/Tooba.Host/AddressBook/AddressBookDevelopmentSeed.cs`
- `src/backend/Host/Tooba.Host/Admin/AdminOrderCompletenessComposer.cs`
- `src/backend/Host/Tooba.Host/Admin/AdminOrderOperationsComposer.cs`
- `src/backend/Host/Tooba.Host/Admin/AdminPanelComposer.cs`
- `src/backend/Host/Tooba.Host/Admin/CatalogDemo/CatalogDemoAssignmentIntegrityService.cs`
- `src/backend/Host/Tooba.Host/Admin/CatalogDemo/CatalogDemoProductSeedService.cs`
- `src/backend/Host/Tooba.Host/Admin/CatalogDemo/CatalogDemoResetService.cs`
- `src/backend/Host/Tooba.Host/Admin/CheckoutAbuseSettingsEndpoints.cs`
- `src/backend/Host/Tooba.Host/Admin/CheckoutIdentitySettingsEndpoints.cs`
- `src/backend/Host/Tooba.Host/Admin/FashionTemplateCatalogSeed.cs`
- `src/backend/Host/Tooba.Host/Admin/HoldPolicySettingsEndpoints.cs`
- `src/backend/Host/Tooba.Host/Admin/IndustryBatchATemplateCatalogSeed.cs`
- `src/backend/Host/Tooba.Host/Admin/IndustryBatchBTemplateCatalogSeed.cs`
- `src/backend/Host/Tooba.Host/Admin/IndustryBatchCTemplateCatalogSeed.cs`
- `src/backend/Host/Tooba.Host/Admin/MerchandisingCampaignAdminEndpoints.cs`
- `src/backend/Host/Tooba.Host/Admin/MerchandisingCampaignDevelopmentSeed.cs`
- `src/backend/Host/Tooba.Host/Admin/OrderInventoryRecoveryComposer.cs`
- `src/backend/Host/Tooba.Host/Admin/OrderSupplyComposer.cs`
- `src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs`
- `src/backend/Host/Tooba.Host/Admin/ProductWorkspaceDevelopmentBootstrap.cs`
- `src/backend/Host/Tooba.Host/Admin/QuantitySettingsEndpoints.cs`
- `src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminComposer.cs`
- `src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminEndpoints.cs`
- `src/backend/Host/Tooba.Host/Admin/ShippingServiceEndpoints.cs`
- `src/backend/Host/Tooba.Host/Admin/StoreAppearanceSettingsComposer.cs`
- `src/backend/Host/Tooba.Host/Admin/StoreLandingPageComposer.cs`
- `src/backend/Host/Tooba.Host/Admin/StoreMenuComposer.cs`
- `src/backend/Host/Tooba.Host/Admin/UnitOfMeasureEndpoints.cs`
- `src/backend/Host/Tooba.Host/Customer/CustomerPanelComposer.cs`
- `src/backend/Host/Tooba.Host/CustomerProfile/CustomerProfileDevelopmentSeed.cs`
- `src/backend/Host/Tooba.Host/Grid/AdminContentGridQueryEngine.cs`
- `src/backend/Host/Tooba.Host/Grid/AdminCustomersGridQueryEngine.cs`
- `src/backend/Host/Tooba.Host/Grid/AdminFulfillmentWorkQueueQueryEngine.cs`
- `src/backend/Host/Tooba.Host/Grid/AdminOrdersGridQueryEngine.cs`
- `src/backend/Host/Tooba.Host/Grid/AdminPaymentsGridQueryEngine.cs`
- `src/backend/Host/Tooba.Host/Grid/AdminPayoutGridQueryEngine.cs`
- `src/backend/Host/Tooba.Host/Grid/AdminProductGridQueryEngine.cs`
- `src/backend/Host/Tooba.Host/Grid/AdminReturnGridQueryEngine.cs`
- `src/backend/Host/Tooba.Host/Grid/AdminReviewGridQueryEngine.cs`
- `src/backend/Host/Tooba.Host/Grid/AdminSellersGridQueryEngine.cs`
- `src/backend/Host/Tooba.Host/Grid/AdminStoryGridQueryEngine.cs`
- `src/backend/Host/Tooba.Host/Program.cs`
- `src/backend/Host/Tooba.Host/ReservationCyclePolicyResolver.cs`
- `src/backend/Host/Tooba.Host/Seller/SellerPanelComposer.cs`
- `src/backend/Host/Tooba.Host/Storefront/CheckoutAbuseGate.cs`
- `src/backend/Host/Tooba.Host/Storefront/StoreAppearanceProjection.cs`
- `src/backend/Host/Tooba.Host/Storefront/StorefrontCartComposer.cs`
- `src/backend/Host/Tooba.Host/Storefront/StorefrontComposer.cs`
- `src/backend/Host/Tooba.Host/Storefront/StorefrontDemoCatalogBootstrap.cs`
- `src/backend/Host/Tooba.Host/Storefront/StorefrontPendingPaymentComposer.cs`
- `src/backend/Host/Tooba.Host/Storefront/StorefrontPendingPaymentProjector.cs`
- `src/backend/Host/Tooba.Host/Storefront/StorefrontShippingComposer.cs`
- `src/backend/Host/Tooba.Host/Wishlist/WishlistDevelopmentSeed.cs`

## Notable business-decision sites

- `Storefront/StorefrontComposer.cs` + `StorefrontPrimaryOfferResolver.cs` — buy-box / price/availability selection
- `Admin/StoreLandingPageComposer.cs` — landing persistence + campaign overlay
- Campaign eligibility / Amazing rail filters in Host composition path
- Grid engines composing operational summaries from multiple DbContexts

## Raw inventory artifact

Machine JSON: `.tmp-tmar-host-audit.json` (session artifact; evidence summary is this file)
