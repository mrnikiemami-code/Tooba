using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Seller;
using Tooba.Inventory.Domain;
using Tooba.Inventory.Infrastructure;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Order.Infrastructure;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Pricing.Infrastructure;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.Tax.Infrastructure;
using Tooba.Tax.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// مالکیت نوشتن Offer/قیمت/موجودی فروشنده: خودی مجاز، خارجی fail-closed.
/// </summary>
[Collection("PostgresSerial")]
public sealed class SellerOfferSaleWriteTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_seller_sale_write")
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

    /// <summary>
    /// مسیرهای Host برای ایجاد Offer و نوشتن قیمت/موجودی با کنترل مالکیت ثبت شده‌اند.
    /// </summary>
    [Fact]
    public void Host_registers_seller_offer_price_inventory_write_routes()
    {
        var root = FindRepoRoot();
        var endpoints = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Seller", "SellerPanelEndpoints.cs"));
        var composer = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Seller", "SellerPanelComposer.cs"));
        var admin = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Admin", "ProductWorkspaceEndpoints.cs"));

        Assert.Contains("MapPost(\"/offers\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("/offers/{offerId:guid}/price", endpoints, StringComparison.Ordinal);
        Assert.Contains("/offers/{offerId:guid}/inventory", endpoints, StringComparison.Ordinal);
        Assert.Contains("RequireAuthorizedAsync", endpoints, StringComparison.Ordinal);
        Assert.Contains("RequireOwnedOfferAsync", composer, StringComparison.Ordinal);
        Assert.Contains("IPriceDirectory", composer, StringComparison.Ordinal);
        Assert.Contains("IInventoryDirectory", composer, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/\", CreateAsync)", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Product.Price", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Product.Stock", composer, StringComparison.Ordinal);
    }

    /// <summary>
    /// فروشندهٔ خودی Offer/قیمت/موجودی می‌سازد؛ فروشندهٔ خارجی همان شناسه‌ها را نمی‌نویسد.
    /// </summary>
    [SkippableFact]
    public async Task Own_seller_may_create_offer_price_inventory_foreign_denied()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-seller-sale", "tenant-seller-sale"));

        await using var catalog = CreateCatalogDb(cs, commerce);
        await using var party = CreatePartyDb(cs, commerce);
        await using var offer = CreateOfferDb(cs, commerce);
        await using var pricing = CreatePricingDb(cs, commerce);
        await using var inventory = CreateInventoryDb(cs, commerce);
        await using var orders = CreateOrderDb(cs, commerce);
        await using var tax = CreateTaxDb(cs, commerce);
        await catalog.Database.MigrateAsync();
        await party.Database.MigrateAsync();
        await offer.Database.MigrateAsync();
        await pricing.Database.MigrateAsync();
        await inventory.Database.MigrateAsync();
        await orders.Database.MigrateAsync();
        await tax.Database.MigrateAsync();

        var catalogDir = new CatalogDirectory(catalog, new OpenCatalogUseCaseGuard());
        var partyDir = new PartyDirectory(party);
        var offerDir = new OfferDirectory(offer, new OpenOfferUseCaseGuard(), catalogDir, partyDir);
        var priceDir = new PriceDirectory(pricing, new OpenPricingUseCaseGuard(), offerDir);
        var inventoryDir = new InventoryDirectory(inventory, new OpenInventoryUseCaseGuard(), offerDir, catalogDir);
        var taxDir = new TaxDirectory(tax, new OpenTaxUseCaseGuard());

        var composer = new SellerPanelComposer(
            offer,
            catalog,
            pricing,
            inventory,
            orders,
            partyDir,
            offerDir,
            priceDir,
            inventoryDir,
            taxDir,
            tax,
            new FakeAccessControlDirectory(),
            catalogDir);

        var names = new Dictionary<string, string> { ["fa-IR"] = "کالای فروش", ["en-US"] = "Sale item" };
        var product = await catalogDir.CreateProductAsync(
            CatalogProductKind.PhysicalGood, "sale-item", null, names, CancellationToken.None);
        await catalogDir.PublishProductAsync(product.ProductId, CancellationToken.None);
        var colorId = await catalogDir.CreateAttributeDefinitionAsync(
            "color-sale",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ", ["en-US"] = "Color" },
            CancellationToken.None);
        var black = await catalogDir.AddAttributeOptionAsync(
            colorId,
            "black",
            new Dictionary<string, string> { ["fa-IR"] = "سیاه", ["en-US"] = "Black" },
            CancellationToken.None);
        var variant = await catalogDir.CreateVariantAsync(
            product.ProductId,
            "SALE-DEFAULT",
            [(colorId, "ignored", black)],
            CancellationToken.None);

        var sellerA = await partyDir.CreateOrganizationAsync("فروشنده الف", null, CancellationToken.None);
        var sellerB = await partyDir.CreateOrganizationAsync("فروشنده ب", null, CancellationToken.None);

        var createdA = await composer.CreateOfferAsync(
            sellerA.PartyId,
            new SellerOfferCreateRequest(variant.VariantId, "SKU-A-1", nameof(OfferStatus.Active)),
            CancellationToken.None);
        Assert.Equal(sellerA.PartyId, createdA.SellerPartyId);
        Assert.Equal(nameof(OfferStatus.Active), createdA.Status);
        Assert.True(createdA.CatalogReadOnly);

        var createdB = await composer.CreateOfferAsync(
            sellerB.PartyId,
            new SellerOfferCreateRequest(variant.VariantId, "SKU-B-1", nameof(OfferStatus.Active)),
            CancellationToken.None);
        Assert.Equal(sellerB.PartyId, createdB.SellerPartyId);

        var duplicateOwn = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            composer.CreateOfferAsync(
                sellerA.PartyId,
                new SellerOfferCreateRequest(variant.VariantId, "SKU-A-2", nameof(OfferStatus.Active)),
                CancellationToken.None));
        Assert.Equal(400, duplicateOwn.StatusCode);
        Assert.Equal("seller.offer.create.rejected", duplicateOwn.ErrorCode);

        var createShape = typeof(SellerOfferCreateRequest).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("SellerPartyId", createShape);
        Assert.Contains("CatalogVariantId", createShape);

        var priced = await composer.SetOfferPriceAsync(
            sellerA.PartyId,
            createdA.OfferId,
            new SellerOfferPriceWriteRequest(125000m, null, null),
            CancellationToken.None);
        Assert.Equal(125000m, priced.Amount);
        Assert.Equal(SellerPanelComposer.DefaultCurrency, priced.Currency);

        var foreignPrice = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            composer.SetOfferPriceAsync(
                sellerB.PartyId,
                createdA.OfferId,
                new SellerOfferPriceWriteRequest(1m, null, null),
                CancellationToken.None));
        Assert.Equal(404, foreignPrice.StatusCode);
        Assert.Equal("seller.offer.missing", foreignPrice.ErrorCode);

        var stocked = await composer.SetOfferInventoryAsync(
            sellerA.PartyId,
            createdA.OfferId,
            new SellerOfferInventoryWriteRequest(9, "test-seed"),
            CancellationToken.None);
        Assert.Equal(9, stocked.OnHand);
        Assert.Equal(9, stocked.AvailableUnits);

        var foreignInventory = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            composer.SetOfferInventoryAsync(
                sellerB.PartyId,
                createdA.OfferId,
                new SellerOfferInventoryWriteRequest(99, "hack"),
                CancellationToken.None));
        Assert.Equal(404, foreignInventory.StatusCode);
        Assert.Equal("seller.offer.missing", foreignInventory.ErrorCode);

        var afterForeign = await composer.GetOfferAsync(sellerA.PartyId, createdA.OfferId, CancellationToken.None);
        Assert.NotNull(afterForeign);
        Assert.Equal(125000m, afterForeign!.Amount);
        Assert.Equal(9, afterForeign.OnHand);
    }

    private static CatalogDbContext CreateCatalogDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new CatalogOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, CatalogDbContext.Schema, typeof(CatalogDbContext));
        options.AddInterceptors(interceptor);
        return new CatalogDbContext(options.Options);
    }

    private static PartyDbContext CreatePartyDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new PartyOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PartyDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PartyDbContext.Schema, typeof(PartyDbContext));
        options.AddInterceptors(interceptor);
        return new PartyDbContext(options.Options);
    }

    private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new OfferOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<OfferDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));
        options.AddInterceptors(interceptor);
        return new OfferDbContext(options.Options);
    }

    private static PricingDbContext CreatePricingDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new PricingOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PricingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PricingDbContext.Schema, typeof(PricingDbContext));
        options.AddInterceptors(interceptor);
        return new PricingDbContext(options.Options);
    }

    private static InventoryDbContext CreateInventoryDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new InventoryOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, InventoryDbContext.Schema, typeof(InventoryDbContext));
        options.AddInterceptors(interceptor);
        return new InventoryDbContext(options.Options);
    }

    private static OrderDbContext CreateOrderDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new OrderOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        options.AddInterceptors(interceptor);
        return new OrderDbContext(options.Options);
    }

    private static TaxDbContext CreateTaxDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new TaxOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<TaxDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, TaxDbContext.Schema, typeof(TaxDbContext));
        options.AddInterceptors(interceptor);
        return new TaxDbContext(options.Options);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
