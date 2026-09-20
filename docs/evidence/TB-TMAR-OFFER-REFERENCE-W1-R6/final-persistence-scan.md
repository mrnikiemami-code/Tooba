# final-persistence-scan

Generated: 2026-09-21T03:03:10.8493746+03:30

| Path | Classification | Allowed? | Reason |
|------|----------------|----------|--------|
| `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Adapters/OfferDevelopmentSeedGateway.cs` (2 hits) | LEGITIMATE_OFFER_INFRASTRUCTURE | YES | Offer-owned EF |
| `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Adapters/OfferModuleMigration.cs` (7 hits) | LEGITIMATE_OFFER_INFRASTRUCTURE | YES | Offer-owned EF |
| `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Adapters/OfferSchemaMigrator.cs` (3 hits) | LEGITIMATE_OFFER_INFRASTRUCTURE | YES | Offer-owned EF |
| `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Adapters/OfferStore.cs` (2 hits) | LEGITIMATE_OFFER_INFRASTRUCTURE | YES | Offer-owned EF |
| `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/DependencyInjection/OfferModule.cs` (4 hits) | LEGITIMATE_OFFER_INFRASTRUCTURE | YES | Offer-owned EF |
| `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Outbox/OfferOutboxRegistration.cs` (3 hits) | LEGITIMATE_OFFER_INFRASTRUCTURE | YES | Offer-owned EF |
| `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/Configurations/SellerOfferConfiguration.cs` (1 hits) | LEGITIMATE_OFFER_INFRASTRUCTURE | YES | Offer-owned EF |
| `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/Migrations/20260823082919_InitialOffer.cs` (1 hits) | LEGITIMATE_OFFER_INFRASTRUCTURE | YES | Offer-owned EF |
| `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/Migrations/20260823082919_InitialOffer.Designer.cs` (3 hits) | LEGITIMATE_OFFER_INFRASTRUCTURE | YES | Offer-owned EF |
| `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/Migrations/20260907120000_AddOfferReturnPolicy.cs` (3 hits) | LEGITIMATE_OFFER_INFRASTRUCTURE | YES | Offer-owned EF |
| `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/Migrations/20260909130100_AddOfferQuantityLimits.cs` (3 hits) | LEGITIMATE_OFFER_INFRASTRUCTURE | YES | Offer-owned EF |
| `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/Migrations/OfferDbContextModelSnapshot.cs` (4 hits) | LEGITIMATE_OFFER_INFRASTRUCTURE | YES | Offer-owned EF |
| `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/OfferDbContext.cs` (10 hits) | LEGITIMATE_OFFER_INFRASTRUCTURE | YES | Offer-owned EF |
| `src/backend/Modules/Offer/Tooba.Offer.Contracts/Ports/IOfferSchemaMigrator.cs` (1 hits) | OTHER | NO | review |
| `src/backend/Host/Tooba.Host.Tests/AdminPanelCompositionTests.cs` (2 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.Host.Tests/CampaignCartPriceIntegrityTests.cs` (5 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.Host.Tests/CartExpiryPostgresTests.cs` (5 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.Host.Tests/CartFoundationTests.cs` (5 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.Host.Tests/CheckoutOrderFoundationTests.cs` (5 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.Host.Tests/InventoryFoundationTests.cs` (5 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.Host.Tests/MerchandisingCampaignAdminTests.cs` (1 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.Host.Tests/MerchandisingCampaignRuntimeTests.cs` (5 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.Host.Tests/OfferDirectoryTestHelper.cs` (3 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.Host.Tests/OfferFoundationTests.cs` (6 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.Host.Tests/PaidOrderReservationLifecycleTests.cs` (5 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.Host.Tests/PricingFoundationTests.cs` (5 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.Host.Tests/ProductHistoryTests.cs` (1 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.Host.Tests/StorefrontDemoCatalogSeedTests.cs` (5 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Host/Tooba.MigrationRunner.Tests/OfferMigrationIntegrityTests.cs` (6 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Modules/Offer/Tooba.Offer.Tests/Architecture/OfferArchitectureGuardTests.cs` (10 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Modules/Offer/Tooba.Offer.Tests/Infrastructure/OfferDbContextOwnershipTests.cs` (6 hits) | TEST_ONLY | YES | integration/architecture test |
| `src/backend/Modules/Offer/Tooba.Offer.Tests/Infrastructure/OfferQueryGatewayTests.cs` (4 hits) | TEST_ONLY | YES | integration/architecture test |

## Verdict
Production Offer persistence access exists ONLY in Offer.Infrastructure (+ justified TEST/TOOLING).

Module-Recovery-State candidate: COMPLETE_REFERENCE_PATTERN
