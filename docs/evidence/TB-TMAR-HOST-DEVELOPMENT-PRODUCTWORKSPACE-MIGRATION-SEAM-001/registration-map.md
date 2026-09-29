# Registration map — TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001

## Neutral seam location

`src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs`
(`Tooba.Persistence` is already referenced by all 28 module Infrastructure projects and by
`Tooba.Host`; no project reference was added.)

## 28 module registrations (own composition root)

| Order | Module | Composition root | Registration | Context / special migrator |
|-------|--------|------------------|--------------|-----------------------------|
| 1 | Catalog | `Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs` | `AddModuleSchemaMigrator<CatalogDbContext>` + step | `CatalogDbContext` |
| 2 | Offer | `Offer/.../DependencyInjection/OfferModule.cs` | delegate | `IOfferSchemaMigrator` |
| 3 | Pricing | `Pricing/.../DependencyInjection/PricingModule.cs` | delegate | `IPricingSchemaMigrator` |
| 4 | Inventory | `Inventory/.../DependencyInjection/InventoryModule.cs` | delegate | `IInventorySchemaMigrator` |
| 5 | Tax | `Tax/.../DependencyInjection/TaxModule.cs` | delegate | `ITaxSchemaMigrator` |
| 6 | Party | `Party/Tooba.Party.Infrastructure/PartyModule.cs` | `AddModuleSchemaMigrator<PartyDbContext>` | `PartyDbContext` |
| 7 | Identity | `Identity/.../IdentityModule.cs` | `AddModuleSchemaMigrator<IdentityDbContext>` | `IdentityDbContext` |
| 8 | Cart | `Cart/.../DependencyInjection/CartModule.cs` | `AddModuleSchemaMigrator<CartDbContext>` | `CartDbContext` |
| 9 | Order | `Order/Tooba.Order.Infrastructure/OrderModule.cs` | `AddModuleSchemaMigrator<OrderDbContext>` | `OrderDbContext` |
| 10 | Payment | `Payment/.../DependencyInjection/PaymentModule.cs` | `AddModuleSchemaMigrator<PaymentDbContext>` | `PaymentDbContext` |
| 11 | Fulfillment | `Fulfillment/.../DependencyInjection/FulfillmentModule.cs` | `AddModuleSchemaMigrator<FulfillmentDbContext>` | `FulfillmentDbContext` |
| 12 | Promotion | `Promotion/.../DependencyInjection/PromotionModule.cs` | delegate + step | `IPromotionSchemaMigrator` |
| 13 | PlatformProbe | `PlatformProbe/.../PlatformProbeModule.cs` | `AddModuleSchemaMigrator<PlatformProbeDbContext>` | `PlatformProbeDbContext` |
| 14 | Reviews | `Reviews/.../ReviewsModule.cs` | `AddModuleSchemaMigrator<ReviewsDbContext>` | `ReviewsDbContext` |
| 15 | ProductQnA | `ProductQnA/.../ProductQnAModule.cs` | `AddModuleSchemaMigrator<ProductQnADbContext>` | `ProductQnADbContext` |
| 16 | BulkInquiry | `BulkInquiry/.../BulkInquiryModule.cs` | `AddModuleSchemaMigrator<BulkInquiryDbContext>` | `BulkInquiryDbContext` |
| 17 | Wishlist | `Wishlist/.../WishlistModule.cs` | `AddModuleSchemaMigrator<WishlistDbContext>` | `WishlistDbContext` |
| 18 | AddressBook | `AddressBook/.../DependencyInjection/AddressBookModule.cs` | `AddModuleSchemaMigrator<AddressBookDbContext>` | `AddressBookDbContext` |
| 19 | CustomerProfile | `CustomerProfile/.../CustomerProfileModule.cs` | `AddModuleSchemaMigrator<CustomerProfileDbContext>` | `CustomerProfileDbContext` |
| 20 | UserPreference | `UserPreference/.../UserPreferenceModule.cs` | `AddModuleSchemaMigrator<UserPreferenceDbContext>` | `UserPreferenceDbContext` |
| 21 | OperatorProfile | `OperatorProfile/.../OperatorProfileModule.cs` | `AddModuleSchemaMigrator<OperatorProfileDbContext>` | `OperatorProfileDbContext` |
| 22 | Content | `Content/.../ContentModule.cs` | `AddModuleSchemaMigrator<ContentDbContext>` | `ContentDbContext` |
| 23 | Media | `Media/.../MediaModule.cs` | `AddModuleSchemaMigrator<MediaDbContext>` | `MediaDbContext` |
| 24 | PageComposition | `PageComposition/.../PageCompositionModule.cs` | `AddModuleSchemaMigrator<PageCompositionDbContext>` | `PageCompositionDbContext` |
| 25 | Story | `Story/.../StoryModule.cs` | `AddModuleSchemaMigrator<StoryDbContext>` | `StoryDbContext` |
| 26 | Notification | `Notification/.../DependencyInjection/NotificationModule.cs` | `AddModuleSchemaMigrator<NotificationDbContext>` | `NotificationDbContext` |
| 27 | AccessControl | `AccessControl/.../AccessControlModule.cs` | `AddModuleSchemaMigrator<AccessControlDbContext>` | `AccessControlDbContext` |
| 28 | Support | `Support/.../DependencyInjection/SupportModule.cs` | `AddModuleSchemaMigrator<SupportDbContext>` | `SupportDbContext` |

**Registered module migrator count: 28 of 28.**

## Module-owned post-migration steps

| Owner | Step | Guard | Why it must follow its module migrate |
|-------|------|-------|----------------------------------------|
| Catalog | `CatalogDevelopmentSeed.PostMigration.EnsureAsync` | `IHostEnvironment.IsDevelopment()` + `CatalogDemoSeedOptions.RunLegacyBootstraps` | previously the `Program.cs` attribute-schema call after the Catalog migrate |
| Promotion | `PromotionDevelopmentSeed.EnsureAsync` | module-internal Development guard | previously inlined after the Promotion migrate; must run even when legacy bootstraps are off (LOCK-SF-402) |

## Catalog legacy demo bootstraps

`CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync` (Catalog Infrastructure, Development folder)
hosts the optional legacy demo orchestration (`FashionTemplateCatalogSeed`,
`IndustryBatchA/B/C TemplateCatalogSeed`, `LandingPageDevelopmentSeed`, `StoreMenuDevelopmentSeed`),
guarded by Development + `RunLegacyBootstraps`. `Program.cs` only calls that Catalog-owned seam.

## Ownership after the change

- Host: orders the neutral seam and assigns the Development commerce context. No module type.
- Module: owns its context/migrator and any Development step that must follow its migrate.
- Platform: owns only the neutral contracts and the generic EF adapter.
