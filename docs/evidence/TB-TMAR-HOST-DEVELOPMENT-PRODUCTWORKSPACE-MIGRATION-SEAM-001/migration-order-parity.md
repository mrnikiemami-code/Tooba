# Migration order parity — TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001

Source of truth for "before": deleted
`Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs` at commit
`e16781dc1899456aec20824afc01e19f53c9a70b` (extracted with `git show HEAD:<path>`).

Source of truth for "after": `ModuleSchemaMigrationOrder` constants in
`BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs` + the 28 registrations.

| # | Before (deleted bootstrap line) | After (seam order) | Match |
|---|----------------------------------|--------------------|-------|
| 1 | `CatalogDbContext` (L95) | `Catalog` = 1 | YES |
| 2 | `IOfferSchemaMigrator` (L96) | `Offer` = 2 (delegate → `IOfferSchemaMigrator`) | YES |
| 3 | `IPricingSchemaMigrator` (L97) | `Pricing` = 3 (delegate) | YES |
| 4 | `IInventorySchemaMigrator` (L98) | `Inventory` = 4 (delegate) | YES |
| 5 | `ITaxSchemaMigrator` (L99) | `Tax` = 5 (delegate) | YES |
| 6 | `PartyDbContext` (L100) | `Party` = 6 | YES |
| 7 | `IdentityDbContext` (L101) | `Identity` = 7 | YES |
| 8 | `CartDbContext` (L102) | `Cart` = 8 | YES |
| 9 | `OrderDbContext` (L103) | `Order` = 9 | YES |
| 10 | `PaymentDbContext` (L104) | `Payment` = 10 | YES |
| 11 | `FulfillmentDbContext` (L105) | `Fulfillment` = 11 | YES |
| 12 | `IPromotionSchemaMigrator` (L106) | `Promotion` = 12 (delegate) | YES |
| 13 | `PlatformProbeDbContext` (L113) | `PlatformProbe` = 13 | YES |
| 14 | `ReviewsDbContext` (L114) | `Reviews` = 14 | YES |
| 15 | `ProductQnADbContext` (L115) | `ProductQnA` = 15 | YES |
| 16 | `BulkInquiryDbContext` (L116) | `BulkInquiry` = 16 | YES |
| 17 | `WishlistDbContext` (L117) | `Wishlist` = 17 | YES |
| 18 | `AddressBookDbContext` (L118) | `AddressBook` = 18 | YES |
| 19 | `CustomerProfileDbContext` (L119) | `CustomerProfile` = 19 | YES |
| 20 | `UserPreferenceDbContext` (L120) | `UserPreference` = 20 | YES |
| 21 | `OperatorProfileDbContext` (L121) | `OperatorProfile` = 21 | YES |
| 22 | `ContentDbContext` (L122) | `Content` = 22 | YES |
| 23 | `MediaDbContext` (L123) | `Media` = 23 | YES |
| 24 | `PageCompositionDbContext` (L124) | `PageComposition` = 24 | YES |
| 25 | `StoryDbContext` (L125) | `Story` = 25 | YES |
| 26 | `NotificationDbContext` (L126) | `Notification` = 26 | YES |
| 27 | `AccessControlDbContext` (L127) | `AccessControl` = 27 | YES |
| 28 | `SupportDbContext` (L128) | `Support` = 28 | YES |

**Parity: 28 of 28, exact order.**

## Interleaved module-owned steps

| Before | After | Match |
|--------|-------|-------|
| `IMerchandisingCampaignDirectory.EnsureAmazingTypeSeededAsync` immediately after Promotion migrate (L108) | `PromotionDevelopmentSeed.EnsureAsync` registered with `AfterOrder = ModuleSchemaMigrationOrder.Promotion` | YES |
| `MerchandisingCampaignDevelopmentSeed.EnsureAsync` immediately after Promotion migrate (L111) | same step (it contains the directory call then the campaign seed) | YES |
| `MerchandisingCampaignDevelopmentSeed.EnsureAsync` in both seed branches (L152, L172) | preserved in both `DevelopmentSchemaMigrator` seed branches | YES |
| `CatalogAttributeSchemaDevelopmentSeed.ApplyAsync` in `Program.cs` under `RunLegacyBootstraps` + Development | `CatalogDevelopmentSeed.PostMigration.EnsureAsync` registered with `AfterOrder = ModuleSchemaMigrationOrder.Catalog`, guarded by Development + `RunLegacyBootstraps` | YES |

## Duplicate-order safety

`DevelopmentSchemaMigrator.MigrateOrderedAsync` groups by `Order` and throws
`InvalidOperationException("Duplicate Development schema migration order: N")` before running any
migration, so a future conflicting registration fails fast instead of silently changing semantics.

## Seed-branch parity

Both the "already live" branch and the "new product" branch of
`DevelopmentSchemaMigrator.ApplyCoreAsync` preserve the deleted bootstrap call list and order,
including `IWorkspaceDemoSeed` Catalog-owned calls and
`MerchandisingCampaignDevelopmentSeed.EnsureAsync` via the Promotion post-migration step.
