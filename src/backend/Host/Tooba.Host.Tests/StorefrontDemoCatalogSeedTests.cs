using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Storefront;
using Tooba.Inventory.Infrastructure;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Pricing.Domain;
using Tooba.Pricing.Infrastructure;
using Tooba.Pricing.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش دانهٔ نمایشی فروشگاه: عمق درخت رده، برند منتشرشده، حقیقت تجاری روی Offer و تکرارپذیری دانه.
/// این fixture یک Postgres واقعی بالا می‌آورد چون دانه فقط از قرارداد دایرکتوری ماژول‌ها نوشته می‌شود
/// و ارزش شواهد آن با DbContext درون‌حافظه‌ای قابل اثبات نیست.
/// </summary>
[Collection("PostgresSerial")]
public sealed class StorefrontDemoCatalogSeedTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <summary>
    /// Postgres واقعی را برای اجرای دانه بالا می‌آورد.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_demo_seed")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public void Demo_matrix_meets_acceptance_thresholds_with_deterministic_identifiers()
    {
        Assert.True(StorefrontDemoCatalogMatrix.TopLevelCategoryCount >= 8);
        Assert.True(StorefrontDemoCatalogMatrix.ChildCategoryCount >= 24);
        Assert.True(StorefrontDemoCatalogMatrix.ProductCount >= 72);
        Assert.True(StorefrontDemoCatalogMatrix.Brands.Count >= 8);
        Assert.All(StorefrontDemoCatalogMatrix.Families, family => Assert.True(family.Children.Count >= 3));
        Assert.All(
            StorefrontDemoCatalogMatrix.Families.SelectMany(family => family.Children),
            child => Assert.True(child.Products.Count >= 3));

        var tokens = StorefrontDemoCatalogMatrix.Families
            .SelectMany(family => family.Children)
            .Select(child => child.Token)
            .ToList();
        Assert.Equal(tokens.Count, tokens.Distinct(StringComparer.Ordinal).Count());

        var brandKeys = StorefrontDemoCatalogMatrix.Brands.Select(brand => brand.Key).ToHashSet(StringComparer.Ordinal);
        var referencedBrandKeys = StorefrontDemoCatalogMatrix.Families
            .SelectMany(family => family.Children)
            .SelectMany(child => child.Products)
            .Select(product => product.BrandKey)
            .Where(key => key is not null)
            .Select(key => key!)
            .ToHashSet(StringComparer.Ordinal);
        Assert.All(referencedBrandKeys, key => Assert.Contains(key, brandKeys));
        Assert.Equal(brandKeys.Count, referencedBrandKeys.Count);
    }

    [Fact]
    public void Mega_menu_category_payload_is_navigation_only()
    {
        var item = new StorefrontCategoryItem(
            Guid.Parse("11111111-1111-7111-8111-111111111111"),
            Guid.Parse("22222222-2222-7222-8222-222222222222"),
            "گوشی موبایل");
        var names = typeof(StorefrontCategoryItem).GetProperties().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("CategoryId", names);
        Assert.Contains("ParentCategoryId", names);
        Assert.Contains("Name", names);
        Assert.Equal(3, names.Count);

        var json = JsonSerializer.Serialize(item, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.DoesNotContain("price", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stock", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("offer", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("product", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"parentCategoryId\"", json, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task Demo_seed_is_repeatable_and_publishes_full_storefront_depth_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var connectionString = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-demo", "tenant-demo"));

        await using var catalogDb = CreateCatalogDb(connectionString, commerce);
        await using var partyDb = CreatePartyDb(connectionString, commerce);
        await using var offerDb = CreateOfferDb(connectionString, commerce);
        await using var pricingDb = CreatePricingDb(connectionString, commerce);
        await using var inventoryDb = CreateInventoryDb(connectionString, commerce);
        await catalogDb.Database.MigrateAsync();
        await partyDb.Database.MigrateAsync();
        await offerDb.Database.MigrateAsync();
        await pricingDb.Database.MigrateAsync();
        await inventoryDb.Database.MigrateAsync();

        var catalogDirectory = new CatalogDirectory(catalogDb, new OpenCatalogUseCaseGuard());
        var partyDirectory = new PartyDirectory(partyDb);
        var offerDirectory = new OfferDirectory(offerDb, new OpenOfferUseCaseGuard(), catalogDirectory, partyDirectory);
        var priceDirectory = new PriceDirectory(pricingDb, new OpenPricingUseCaseGuard(), offerDirectory);
        var inventoryDirectory = new InventoryDirectory(inventoryDb, new OpenInventoryUseCaseGuard(), offerDirectory, catalogDirectory);

        var first = await StorefrontDemoCatalogBootstrap.SeedAsync(
            catalogDb,
            catalogDirectory,
            partyDirectory,
            offerDirectory,
            priceDirectory,
            inventoryDirectory,
            CancellationToken.None);

        Assert.False(first.AlreadySeeded);
        Assert.True(first.TopLevelCategories >= 8, $"top-level categories = {first.TopLevelCategories}");
        Assert.True(first.ChildCategories >= 24, $"child categories = {first.ChildCategories}");
        Assert.True(first.PublishedProducts >= 72, $"published products = {first.PublishedProducts}");
        Assert.True(first.PublishedBrands >= 8, $"published brands = {first.PublishedBrands}");
        Assert.Equal(StorefrontDemoCatalogMatrix.ExpectedOfferCount, first.Offers);

        var offerCount = await offerDb.Offers.AsNoTracking().CountAsync(offer => offer.Status == OfferStatus.Active);
        var priceCount = await pricingDb.Prices.AsNoTracking().CountAsync(price => price.Status == PriceStatus.Active);
        var positionCount = await inventoryDb.Positions.AsNoTracking().CountAsync();
        Assert.Equal(StorefrontDemoCatalogMatrix.ExpectedOfferCount, offerCount);
        Assert.Equal(StorefrontDemoCatalogMatrix.ExpectedOfferCount, priceCount);
        Assert.Equal(StorefrontDemoCatalogMatrix.ExpectedOfferCount, positionCount);
        Assert.True(await inventoryDb.Positions.AsNoTracking().AllAsync(position => position.OnHand > 0));

        var publishedProducts = await catalogDb.Products.AsNoTracking()
            .Where(product => product.Status == CatalogPublicationStatus.Published && product.SlugSeam!.StartsWith("demo-"))
            .ToListAsync();
        Assert.Equal(StorefrontDemoCatalogMatrix.ProductCount, publishedProducts.Count);
        Assert.Equal(publishedProducts.Count, publishedProducts.Select(product => product.SlugSeam).Distinct().Count());
        Assert.All(publishedProducts, product => Assert.True(
            catalogDb.Variants.AsNoTracking().Any(variant => variant.ProductId == product.ProductId),
            $"missing variant for {product.SlugSeam}"));

        var publishedBrandIds = await catalogDb.Brands.AsNoTracking()
            .Where(brand => brand.Status == CatalogPublicationStatus.Published)
            .Select(brand => brand.BrandId)
            .ToListAsync();
        Assert.Equal(StorefrontDemoCatalogMatrix.Brands.Count, publishedBrandIds.Count);
        var brandProductCounts = publishedBrandIds.ToDictionary(
            brandId => brandId,
            brandId => publishedProducts.Count(product => product.BrandId == brandId));
        Assert.All(brandProductCounts, pair => Assert.True(pair.Value >= 1, $"brand {pair.Key:N} has no published product"));

        var categoryItems = await LoadCategoryItemsAsync(catalogDb);
        Assert.True(categoryItems.Count(item => item.ParentCategoryId is null) >= 8);
        Assert.True(categoryItems.Count(item => item.ParentCategoryId is not null) >= 24);
        foreach (var family in StorefrontDemoCatalogMatrix.Families)
        {
            var root = categoryItems.Single(item => item.ParentCategoryId is null && item.Name == family.Name);
            Assert.True(categoryItems.Count(item => item.ParentCategoryId == root.CategoryId) >= 3, family.Name);

            var descendants = StorefrontComposer.DescendantCategoryIds(categoryItems, root.CategoryId);
            var reachableProducts = await catalogDb.ProductCategories.AsNoTracking()
                .Where(link => descendants.Contains(link.CategoryId))
                .Select(link => link.ProductId)
                .Distinct()
                .CountAsync();
            Assert.True(
                reachableProducts >= family.Children.Sum(child => child.Products.Count),
                $"{family.Name} reachable products = {reachableProducts}");
        }

        var second = await StorefrontDemoCatalogBootstrap.SeedAsync(
            catalogDb,
            catalogDirectory,
            partyDirectory,
            offerDirectory,
            priceDirectory,
            inventoryDirectory,
            CancellationToken.None);

        Assert.True(second.AlreadySeeded);
        Assert.Equal(first.TopLevelCategories, second.TopLevelCategories);
        Assert.Equal(first.ChildCategories, second.ChildCategories);
        Assert.Equal(first.PublishedProducts, second.PublishedProducts);
        Assert.Equal(first.PublishedBrands, second.PublishedBrands);
        Assert.Equal(StorefrontDemoCatalogMatrix.ExpectedOfferCount, await offerDb.Offers.AsNoTracking().CountAsync());
        Assert.Equal(StorefrontDemoCatalogMatrix.ExpectedOfferCount, await pricingDb.Prices.AsNoTracking().CountAsync());
        Assert.Equal(StorefrontDemoCatalogMatrix.ExpectedOfferCount, await inventoryDb.Positions.AsNoTracking().CountAsync());
        Assert.Equal(
            StorefrontDemoCatalogMatrix.Brands.Count,
            await catalogDb.Brands.AsNoTracking().CountAsync(brand => brand.Status == CatalogPublicationStatus.Published));
    }

    /// <summary>
    /// رده‌های منتشرشده را با همان قرارداد ناوبری فروشگاه می‌خواند تا محاسبهٔ فرزندان روی دادهٔ واقعی دانه آزمون شود.
    /// </summary>
    private static async Task<List<StorefrontCategoryItem>> LoadCategoryItemsAsync(CatalogDbContext catalogDb)
    {
        var categories = await catalogDb.Categories.AsNoTracking()
            .Where(category => category.Status == CatalogPublicationStatus.Published)
            .ToListAsync();
        var names = await catalogDb.LocalizedTexts.AsNoTracking()
            .Where(text => text.OwnerKind == CatalogLocalizedOwnerKind.Category && text.FieldKey == "name")
            .ToListAsync();
        return categories
            .Select(category => new StorefrontCategoryItem(
                category.CategoryId,
                category.ParentCategoryId,
                names.FirstOrDefault(text => text.OwnerId == category.CategoryId)?.Value ?? "رده"))
            .ToList();
    }

    private static CatalogDbContext CreateCatalogDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, CatalogDbContext.Schema, typeof(CatalogDbContext));
        options.AddInterceptors(CreateOutboxInterceptor(commerce, new CatalogOutboxRegistration()));
        return new CatalogDbContext(options.Options);
    }

    private static PartyDbContext CreatePartyDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var options = new DbContextOptionsBuilder<PartyDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PartyDbContext.Schema, typeof(PartyDbContext));
        options.AddInterceptors(CreateOutboxInterceptor(commerce, new PartyOutboxRegistration()));
        return new PartyDbContext(options.Options);
    }

    private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var options = new DbContextOptionsBuilder<OfferDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));
        options.AddInterceptors(CreateOutboxInterceptor(commerce, new OfferOutboxRegistration()));
        return new OfferDbContext(options.Options);
    }

    private static PricingDbContext CreatePricingDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var options = new DbContextOptionsBuilder<PricingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PricingDbContext.Schema, typeof(PricingDbContext));
        options.AddInterceptors(CreateOutboxInterceptor(commerce, new PricingOutboxRegistration()));
        return new PricingDbContext(options.Options);
    }

    private static InventoryDbContext CreateInventoryDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, InventoryDbContext.Schema, typeof(InventoryDbContext));
        options.AddInterceptors(CreateOutboxInterceptor(commerce, new InventoryOutboxRegistration()));
        return new InventoryDbContext(options.Options);
    }

    /// <summary>
    /// Interceptor Outbox همان ماژول را می‌سازد تا رویدادهای یکپارچگی مثل زمان اجرا در تراکنش محلی نوشته شوند.
    /// </summary>
    private static OutboxSaveChangesInterceptor CreateOutboxInterceptor(
        ICurrentCommerceContext commerce,
        IOutboxModuleRegistration registration)
    {
        var modules = new[] { registration };
        return new OutboxSaveChangesInterceptor(commerce, modules, new JsonIntegrationEventSerializer(modules));
    }
}
