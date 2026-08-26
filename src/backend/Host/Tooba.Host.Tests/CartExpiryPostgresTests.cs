using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application;
using Tooba.Cart.Domain;
using Tooba.Cart.Infrastructure;
using Tooba.Cart.Infrastructure.Persistence;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
using Tooba.Inventory.Infrastructure;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Pricing.Application;
using Tooba.Pricing.Infrastructure;
using Tooba.Pricing.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// تست PostgreSQL برای انقضای batch-wise سبد، SKIP LOCKED و idempotency.
/// </summary>
[Collection("PostgresSerial")]
public sealed class CartExpiryPostgresTests : IAsyncLifetime
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
                .WithDatabase("tooba_cart_expiry")
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

    [SkippableFact]
    public async Task Duplicate_expiry_trigger_is_idempotent()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var stack = await CreateStackAsync(_container.GetConnectionString());
        var guest = await stack.Carts.CreateGuestAsync("IR", "IRR", SalesChannel.Marketplace, CancellationToken.None);
        var access = new CartAccess(null, guest.GuestSecret);
        var withLine = await stack.Carts.AddOrIncreaseLineAsync(
            guest.Cart.CartId,
            access,
            guest.Cart.Version,
            stack.OfferId,
            1,
            CancellationToken.None);
        await ForceExpiredAsync(stack.CartDb, withLine.CartId);

        var first = await stack.Carts.ExpireDueCartsAsync(DateTimeOffset.UtcNow, 10, CancellationToken.None);
        var second = await stack.Carts.ExpireDueCartsAsync(DateTimeOffset.UtcNow, 10, CancellationToken.None);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(CartStatus.Expired, (await stack.CartDb.Carts.SingleAsync(x => x.CartId == withLine.CartId)).Status);
    }

    [SkippableFact]
    public async Task Batch_expiry_processes_all_due_carts_once()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var stack = await CreateStackAsync(_container.GetConnectionString());
        for (var i = 0; i < 2; i++)
        {
            var guest = await stack.Carts.CreateGuestAsync("IR", "IRR", SalesChannel.Marketplace, CancellationToken.None);
            var access = new CartAccess(null, guest.GuestSecret);
            var withLine = await stack.Carts.AddOrIncreaseLineAsync(
                guest.Cart.CartId,
                access,
                guest.Cart.Version,
                stack.OfferId,
                1,
                CancellationToken.None);
            await ForceExpiredAsync(stack.CartDb, withLine.CartId);
        }

        Assert.Equal(2, await stack.Carts.ExpireDueCartsAsync(DateTimeOffset.UtcNow, 10, CancellationToken.None));
        Assert.Equal(0, await stack.Carts.ExpireDueCartsAsync(DateTimeOffset.UtcNow, 10, CancellationToken.None));
        Assert.Equal(2, await stack.CartDb.Carts.CountAsync(x => x.Status == CartStatus.Expired));
    }

    private static async Task ForceExpiredAsync(CartDbContext cartDb, Guid cartId)
    {
        var cart = await cartDb.Carts.SingleAsync(x => x.CartId == cartId);
        cart.GetType().GetProperty("ExpiresAt")!.SetValue(cart, DateTimeOffset.UtcNow.AddMinutes(-5));
        await cartDb.SaveChangesAsync();
    }

    private static async Task<CartExpiryStack> CreateStackAsync(string connectionString)
    {
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-expiry", "tenant-expiry"));

        var catalogDb = CreateCatalogDb(connectionString, commerce);
        var partyDb = CreatePartyDb(connectionString, commerce);
        var offerDb = CreateOfferDb(connectionString, commerce);
        var pricingDb = CreatePricingDb(connectionString, commerce);
        var inventoryDb = CreateInventoryDb(connectionString, commerce);
        var cartDb = CreateCartDb(connectionString, commerce);
        await catalogDb.Database.MigrateAsync();
        await partyDb.Database.MigrateAsync();
        await offerDb.Database.MigrateAsync();
        await pricingDb.Database.MigrateAsync();
        await inventoryDb.Database.MigrateAsync();
        await cartDb.Database.MigrateAsync();

        var catalogDir = new CatalogDirectory(catalogDb, new OpenCatalogUseCaseGuard());
        var partyDir = new PartyDirectory(partyDb);
        var offerDir = new OfferDirectory(offerDb, new OpenOfferUseCaseGuard(), catalogDir, partyDir);
        var priceDir = new PriceDirectory(pricingDb, new OpenPricingUseCaseGuard(), offerDir);
        var inventoryDir = new InventoryDirectory(inventoryDb, new OpenInventoryUseCaseGuard(), offerDir, catalogDir);
        var cartDir = new CartDirectory(cartDb, new OpenCartUseCaseGuard(), offerDir, priceDir, inventoryDir, inventoryDir);

        var names = new Dictionary<string, string> { ["fa-IR"] = "کالای انقضا", ["en-US"] = "Expiry Item" };
        var product = await catalogDir.CreateProductAsync(CatalogProductKind.PhysicalGood, "expiry-item", null, names, CancellationToken.None);
        var colorId = await catalogDir.CreateAttributeDefinitionAsync(
            "color-exp",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" },
            CancellationToken.None);
        var black = await catalogDir.AddAttributeOptionAsync(colorId, "black", new Dictionary<string, string> { ["fa-IR"] = "سیاه" }, CancellationToken.None);
        var variant = await catalogDir.CreateVariantAsync(product.ProductId, "EXP-V1", [(colorId, "ignored", black)], CancellationToken.None);
        var seller = await partyDir.CreateOrganizationAsync("فروشنده انقضا", null, CancellationToken.None);
        var offer = await offerDir.CreateOfferAsync(variant.VariantId, seller.PartyId, SalesChannel.Marketplace, "EXP-1", CancellationToken.None);
        await offerDir.ActivateAsync(offer.OfferId, CancellationToken.None);
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var price = await priceDir.CreatePriceAsync(offer.OfferId, "IR", SalesChannel.Marketplace, 100000, "IRR", start, null, CancellationToken.None);
        await priceDir.ActivateAsync(price.PriceId, CancellationToken.None);
        var loc = await inventoryDir.CreateLocationAsync("WH-EXP", "انبار", CancellationToken.None);
        var stock = await inventoryDir.OpenPositionAsync(offer.OfferId, loc, CancellationToken.None);
        await inventoryDir.AdjustAsync(stock, StockAdjustmentKind.Increase, 10, "seed", null, CancellationToken.None);

        return new CartExpiryStack(cartDb, cartDir, offer.OfferId);
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

    private static CartDbContext CreateCartDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new CartOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<CartDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, CartDbContext.Schema, typeof(CartDbContext));
        options.AddInterceptors(interceptor);
        return new CartDbContext(options.Options);
    }

    private sealed record CartExpiryStack(CartDbContext CartDb, ICartDirectory Carts, Guid OfferId);
}
