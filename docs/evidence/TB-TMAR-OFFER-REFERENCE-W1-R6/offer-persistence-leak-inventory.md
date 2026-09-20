# offer-persistence-leak-inventory

Total production+test hits outside Offer.Infrastructure: 111

## HOST_ADMIN (19)
- src/backend/Host/Tooba.Host/Admin/AdminPanelComposer.cs:9: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host/Admin/AdminPanelComposer.cs:31: `private readonly OfferDbContext _offers;`
- src/backend/Host/Tooba.Host/Admin/AdminPanelComposer.cs:50: `OfferDbContext offers,`
- src/backend/Host/Tooba.Host/Admin/MerchandisingCampaignAdminEndpoints.cs:8: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host/Admin/MerchandisingCampaignAdminEndpoints.cs:109: `private readonly OfferDbContext _offers;`
- src/backend/Host/Tooba.Host/Admin/MerchandisingCampaignAdminEndpoints.cs:119: `OfferDbContext offers,`
- src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs:8: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs:24: `private readonly OfferDbContext _offers;`
- src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs:36: `OfferDbContext offers,`
- src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminComposer.cs:5: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminComposer.cs:81: `OfferDbContext offers,`
- src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminComposer.cs:108: `OfferDbContext offers,`
- src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminEndpoints.cs:4: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminEndpoints.cs:146: `OfferDbContext offers,`
- src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminEndpoints.cs:173: `OfferDbContext offers,`
- src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminEndpoints.cs:209: `OfferDbContext offers,`
- src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminEndpoints.cs:280: `OfferDbContext offers,`
- src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs:16: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs:57: `Descriptor<OfferDbContext>("Offer", OfferDbContext.Schema),`

## HOST_GRID (6)
- src/backend/Host/Tooba.Host/Grid/AdminProductGridQueryEngine.cs:6: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host/Grid/AdminProductGridQueryEngine.cs:19: `private readonly OfferDbContext _offers;`
- src/backend/Host/Tooba.Host/Grid/AdminProductGridQueryEngine.cs:25: `OfferDbContext offers,`
- src/backend/Host/Tooba.Host/Grid/AdminSellersGridQueryEngine.cs:5: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host/Grid/AdminSellersGridQueryEngine.cs:19: `private readonly OfferDbContext _offers;`
- src/backend/Host/Tooba.Host/Grid/AdminSellersGridQueryEngine.cs:24: `OfferDbContext offers,`

## HOST_SEED_BOOTSTRAP (13)
- src/backend/Host/Tooba.Host/AccessControl/AccessControlDevelopmentSeed.cs:28: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host/AccessControl/AccessControlDevelopmentSeed.cs:89: `var offerDb = provider.GetRequiredService<OfferDbContext>();`
- src/backend/Host/Tooba.Host/AccessControl/AccessControlDevelopmentSeed.cs:352: `OfferDbContext offerDb,`
- src/backend/Host/Tooba.Host/Admin/CatalogAttributeSchemaDevelopmentBootstrap.cs:13: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host/Admin/CatalogAttributeSchemaDevelopmentBootstrap.cs:189: `var offerDb = provider.GetRequiredService<OfferDbContext>();`
- src/backend/Host/Tooba.Host/Admin/MerchandisingCampaignDevelopmentSeed.cs:10: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host/Admin/MerchandisingCampaignDevelopmentSeed.cs:65: `var offerDb = provider.GetRequiredService<OfferDbContext>();`
- src/backend/Host/Tooba.Host/Admin/MerchandisingCampaignDevelopmentSeed.cs:327: `OfferDbContext offerDb,`
- src/backend/Host/Tooba.Host/Admin/ProductWorkspaceDevelopmentBootstrap.cs:15: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host/Admin/ProductWorkspaceDevelopmentBootstrap.cs:102: `await MigrateAsync(provider.GetRequiredService<OfferDbContext>());`
- src/backend/Host/Tooba.Host/Storefront/StorefrontDemoCatalogBootstrap.cs:12: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host/Storefront/StorefrontDemoCatalogBootstrap.cs:122: `provider.GetRequiredService<OfferDbContext>(),`
- src/backend/Host/Tooba.Host/Storefront/StorefrontDemoCatalogBootstrap.cs:474: `OfferDbContext offers,`

## HOST_STOREFRONT (3)
- src/backend/Host/Tooba.Host/Storefront/StorefrontComposer.cs:9: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host/Storefront/StorefrontComposer.cs:34: `private readonly OfferDbContext _offers;`
- src/backend/Host/Tooba.Host/Storefront/StorefrontComposer.cs:49: `OfferDbContext offers,`

## TEST_ONLY (70)
- src/backend/Host/Tooba.Host.Tests/CampaignCartPriceIntegrityTests.cs:21: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host.Tests/CampaignCartPriceIntegrityTests.cs:338: `private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)`
- src/backend/Host/Tooba.Host.Tests/CampaignCartPriceIntegrityTests.cs:343: `var options = new DbContextOptionsBuilder<OfferDbContext>();`
- src/backend/Host/Tooba.Host.Tests/CampaignCartPriceIntegrityTests.cs:344: `ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));`
- src/backend/Host/Tooba.Host.Tests/CampaignCartPriceIntegrityTests.cs:346: `return new OfferDbContext(options.Options);`
- src/backend/Host/Tooba.Host.Tests/CartExpiryPostgresTests.cs:24: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host.Tests/CartExpiryPostgresTests.cs:200: `private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)`
- src/backend/Host/Tooba.Host.Tests/CartExpiryPostgresTests.cs:205: `var options = new DbContextOptionsBuilder<OfferDbContext>();`
- src/backend/Host/Tooba.Host.Tests/CartExpiryPostgresTests.cs:206: `ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));`
- src/backend/Host/Tooba.Host.Tests/CartExpiryPostgresTests.cs:208: `return new OfferDbContext(options.Options);`
- src/backend/Host/Tooba.Host.Tests/CartFoundationTests.cs:29: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host.Tests/CartFoundationTests.cs:334: `private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)`
- src/backend/Host/Tooba.Host.Tests/CartFoundationTests.cs:339: `var options = new DbContextOptionsBuilder<OfferDbContext>();`
- src/backend/Host/Tooba.Host.Tests/CartFoundationTests.cs:340: `ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));`
- src/backend/Host/Tooba.Host.Tests/CartFoundationTests.cs:342: `return new OfferDbContext(options.Options);`
- src/backend/Host/Tooba.Host.Tests/CheckoutOrderFoundationTests.cs:23: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host.Tests/CheckoutOrderFoundationTests.cs:496: `private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)`
- src/backend/Host/Tooba.Host.Tests/CheckoutOrderFoundationTests.cs:501: `var options = new DbContextOptionsBuilder<OfferDbContext>();`
- src/backend/Host/Tooba.Host.Tests/CheckoutOrderFoundationTests.cs:502: `ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));`
- src/backend/Host/Tooba.Host.Tests/CheckoutOrderFoundationTests.cs:504: `return new OfferDbContext(options.Options);`
- src/backend/Host/Tooba.Host.Tests/InventoryFoundationTests.cs:20: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host.Tests/InventoryFoundationTests.cs:338: `private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)`
- src/backend/Host/Tooba.Host.Tests/InventoryFoundationTests.cs:343: `var options = new DbContextOptionsBuilder<OfferDbContext>();`
- src/backend/Host/Tooba.Host.Tests/InventoryFoundationTests.cs:344: `ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));`
- src/backend/Host/Tooba.Host.Tests/InventoryFoundationTests.cs:346: `return new OfferDbContext(options.Options);`
- src/backend/Host/Tooba.Host.Tests/MerchandisingCampaignAdminTests.cs:15: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host.Tests/MerchandisingCampaignRuntimeTests.cs:19: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host.Tests/MerchandisingCampaignRuntimeTests.cs:545: `private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)`
- src/backend/Host/Tooba.Host.Tests/MerchandisingCampaignRuntimeTests.cs:550: `var options = new DbContextOptionsBuilder<OfferDbContext>();`
- src/backend/Host/Tooba.Host.Tests/MerchandisingCampaignRuntimeTests.cs:551: `ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));`
- src/backend/Host/Tooba.Host.Tests/MerchandisingCampaignRuntimeTests.cs:553: `return new OfferDbContext(options.Options);`
- src/backend/Host/Tooba.Host.Tests/OfferDirectoryTestHelper.cs:12: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host.Tests/OfferDirectoryTestHelper.cs:26: `private readonly OfferDbContext _db;`
- src/backend/Host/Tooba.Host.Tests/OfferDirectoryTestHelper.cs:33: `public OfferDirectory(OfferDbContext db, IOfferUseCaseGuard guard, ICatalogLookupGateway catalog, IPartyLookupGateway party, IClock clock, IIdGenerator ids)`
- src/backend/Host/Tooba.Host.Tests/OfferFoundationTests.cs:18: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host.Tests/OfferFoundationTests.cs:118: `Assert.Equal("offer", OfferDbContext.Schema);`
- src/backend/Host/Tooba.Host.Tests/OfferFoundationTests.cs:269: `private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)`
- src/backend/Host/Tooba.Host.Tests/OfferFoundationTests.cs:274: `var options = new DbContextOptionsBuilder<OfferDbContext>();`
- src/backend/Host/Tooba.Host.Tests/OfferFoundationTests.cs:275: `ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));`
- src/backend/Host/Tooba.Host.Tests/OfferFoundationTests.cs:277: `return new OfferDbContext(options.Options);`
- src/backend/Host/Tooba.Host.Tests/PaidOrderReservationLifecycleTests.cs:17: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host.Tests/PaidOrderReservationLifecycleTests.cs:371: `private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)`
- src/backend/Host/Tooba.Host.Tests/PaidOrderReservationLifecycleTests.cs:376: `var options = new DbContextOptionsBuilder<OfferDbContext>();`
- src/backend/Host/Tooba.Host.Tests/PaidOrderReservationLifecycleTests.cs:377: `ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));`
- src/backend/Host/Tooba.Host.Tests/PaidOrderReservationLifecycleTests.cs:379: `return new OfferDbContext(options.Options);`
- src/backend/Host/Tooba.Host.Tests/PricingFoundationTests.cs:15: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host.Tests/PricingFoundationTests.cs:278: `private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)`
- src/backend/Host/Tooba.Host.Tests/PricingFoundationTests.cs:283: `var options = new DbContextOptionsBuilder<OfferDbContext>();`
- src/backend/Host/Tooba.Host.Tests/PricingFoundationTests.cs:284: `ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));`
- src/backend/Host/Tooba.Host.Tests/PricingFoundationTests.cs:286: `return new OfferDbContext(options.Options);`
- src/backend/Host/Tooba.Host.Tests/ProductHistoryTests.cs:79: `Assert.DoesNotContain("OfferDbContext", directory, StringComparison.Ordinal);`
- src/backend/Host/Tooba.Host.Tests/StorefrontDemoCatalogSeedTests.cs:15: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.Host.Tests/StorefrontDemoCatalogSeedTests.cs:288: `private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)`
- src/backend/Host/Tooba.Host.Tests/StorefrontDemoCatalogSeedTests.cs:290: `var options = new DbContextOptionsBuilder<OfferDbContext>();`
- src/backend/Host/Tooba.Host.Tests/StorefrontDemoCatalogSeedTests.cs:291: `ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));`
- src/backend/Host/Tooba.Host.Tests/StorefrontDemoCatalogSeedTests.cs:293: `return new OfferDbContext(options.Options);`
- src/backend/Host/Tooba.MigrationRunner.Tests/OfferMigrationIntegrityTests.cs:9: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Host/Tooba.MigrationRunner.Tests/OfferMigrationIntegrityTests.cs:115: `private static OfferDbContext CreateOffer(string connectionString)`
- src/backend/Host/Tooba.MigrationRunner.Tests/OfferMigrationIntegrityTests.cs:117: `var options = new DbContextOptionsBuilder<OfferDbContext>();`
- src/backend/Host/Tooba.MigrationRunner.Tests/OfferMigrationIntegrityTests.cs:121: `OfferDbContext.Schema,`
- src/backend/Host/Tooba.MigrationRunner.Tests/OfferMigrationIntegrityTests.cs:122: `typeof(OfferDbContext));`
- src/backend/Host/Tooba.MigrationRunner.Tests/OfferMigrationIntegrityTests.cs:123: `return new OfferDbContext(options.Options);`
- src/backend/Modules/Offer/Tooba.Offer.Tests/Architecture/OfferArchitectureGuardTests.cs:144: `.Where(x => x.Text.Contains("OfferDbContext", StringComparison.Ordinal))`
- src/backend/Modules/Offer/Tooba.Offer.Tests/Architecture/OfferArchitectureGuardTests.cs:166: `Assert.DoesNotContain("OfferDbContext", composer, StringComparison.Ordinal);`
- src/backend/Modules/Offer/Tooba.Offer.Tests/Infrastructure/OfferDbContextOwnershipTests.cs:9: `using Tooba.Offer.Infrastructure.Persistence;`
- src/backend/Modules/Offer/Tooba.Offer.Tests/Infrastructure/OfferDbContextOwnershipTests.cs:13: `public sealed class OfferDbContextOwnershipTests`
- src/backend/Modules/Offer/Tooba.Offer.Tests/Infrastructure/OfferDbContextOwnershipTests.cs:16: `public void OfferDbContext_schema_is_offer_and_maps_seller_offer_only()`
- src/backend/Modules/Offer/Tooba.Offer.Tests/Infrastructure/OfferDbContextOwnershipTests.cs:18: `var options = new DbContextOptionsBuilder<OfferDbContext>()`
- src/backend/Modules/Offer/Tooba.Offer.Tests/Infrastructure/OfferDbContextOwnershipTests.cs:21: `using var db = new OfferDbContext(options);`
- src/backend/Modules/Offer/Tooba.Offer.Tests/Infrastructure/OfferDbContextOwnershipTests.cs:22: `Assert.Equal("offer", OfferDbContext.Schema);`

