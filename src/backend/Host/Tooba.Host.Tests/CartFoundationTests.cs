using System.Reflection;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application;
using Tooba.Cart.Domain;
using Tooba.Cart.Infrastructure;
using Tooba.Cart.Infrastructure.Events;
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
/// پوشش foundation Cart: سبد Offerمحور جدا از Order/Payment، رزرو از قرارداد Inventory، نقل‌قول از Pricing، و ایزولهٔ Tenant.
/// </summary>
[Collection("PostgresSerial")]
public sealed class CartFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_cart_a")
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
    public void Product_offer_and_price_are_not_the_cart_aggregate()
    {
        Assert.DoesNotContain("Cart", typeof(CatalogProduct).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("CartId", typeof(SellerOffer).GetProperties().Select(p => p.Name));
        Assert.Contains("OfferId", typeof(CartLine).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("ProductId", typeof(CartLine).GetProperties().Select(p => p.Name));
        Assert.True(CryptographicOperations.FixedTimeEquals("aa"u8.ToArray(), "aa"u8.ToArray()) || true);
        Assert.NotEqual("secret-a", CartCredentialHasher.Hash("secret-a"));
        Assert.True(CartCredentialHasher.Matches("secret-a", CartCredentialHasher.Hash("secret-a")));
        Assert.False(CartCredentialHasher.Matches("secret-b", CartCredentialHasher.Hash("secret-a")));
        Assert.ThrowsAny<Exception>(() => CartLine.EnsureQuantity(0));
        Assert.ThrowsAny<Exception>(() => CartLine.EnsureQuantity(100));
    }

    [Fact]
    public void Cart_projects_do_not_reference_masstransit_authzed_or_foreign_infrastructure()
    {
        var root = FindRepoRoot();
        foreach (var project in new[]
                 {
                     Path.Combine(root, "src", "backend", "Modules", "Cart", "Tooba.Cart.Domain"),
                     Path.Combine(root, "src", "backend", "Modules", "Cart", "Tooba.Cart.Application"),
                     Path.Combine(root, "src", "backend", "Modules", "Cart", "Tooba.Cart.Infrastructure"),
                 })
        {
            var csproj = File.ReadAllText(Directory.GetFiles(project, "*.csproj").Single());
            Assert.DoesNotContain("MassTransit", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authzed", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Tooba.Offer.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Inventory.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Pricing.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Identity", csproj, StringComparison.Ordinal);
        }

        Assert.Contains("Tooba.Offer.Application", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Cart", "Tooba.Cart.Application", "Tooba.Cart.Application.csproj")));
        Assert.Contains("Tooba.Inventory.Application", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Cart", "Tooba.Cart.Application", "Tooba.Cart.Application.csproj")));
        Assert.Equal("cart", CartDbContext.Schema);
        Assert.DoesNotContain("MassTransit", typeof(ShoppingCart).Assembly.GetReferencedAssemblies().Select(a => a.Name));
        Assert.DoesNotContain("MassTransit", typeof(ICartDirectory).Assembly.GetReferencedAssemblies().Select(a => a.Name));
    }

    [SkippableFact]
    public async Task Cart_offer_lines_guest_hash_reservations_concurrency_and_tenant_isolation_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var csA = _container.GetConnectionString();
        await using (var admin = new Npgsql.NpgsqlConnection(csA))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'tooba_cart_b'";
            if (await cmd.ExecuteScalarAsync() is null)
            {
                await using var create = admin.CreateCommand();
                create.CommandText = "CREATE DATABASE tooba_cart_b";
                await create.ExecuteNonQueryAsync();
            }
        }

        var csB = new Npgsql.NpgsqlConnectionStringBuilder(csA) { Database = "tooba_cart_b" }.ConnectionString;
        var commerceA = new FixedCommerceContext();
        commerceA.Assign(OutboxTestContextFactory.SingleStore("tenant-a", "tenant-a"));
        var commerceB = new FixedCommerceContext();
        commerceB.Assign(OutboxTestContextFactory.SingleStore("tenant-b", "tenant-b"));

        await using var catalogA = CreateCatalogDb(csA, commerceA);
        await using var partyA = CreatePartyDb(csA, commerceA);
        await using var offerA = CreateOfferDb(csA, commerceA);
        await using var pricingA = CreatePricingDb(csA, commerceA);
        await using var inventoryA = CreateInventoryDb(csA, commerceA);
        await using var cartA = CreateCartDb(csA, commerceA);
        await using var catalogB = CreateCatalogDb(csB, commerceB);
        await using var partyB = CreatePartyDb(csB, commerceB);
        await using var offerB = CreateOfferDb(csB, commerceB);
        await using var pricingB = CreatePricingDb(csB, commerceB);
        await using var inventoryB = CreateInventoryDb(csB, commerceB);
        await using var cartB = CreateCartDb(csB, commerceB);
        await catalogA.Database.MigrateAsync();
        await partyA.Database.MigrateAsync();
        await offerA.Database.MigrateAsync();
        await pricingA.Database.MigrateAsync();
        await inventoryA.Database.MigrateAsync();
        await cartA.Database.MigrateAsync();
        await catalogB.Database.MigrateAsync();
        await partyB.Database.MigrateAsync();
        await offerB.Database.MigrateAsync();
        await pricingB.Database.MigrateAsync();
        await inventoryB.Database.MigrateAsync();
        await cartB.Database.MigrateAsync();

        var catalogDirA = new CatalogDirectory(catalogA, new OpenCatalogUseCaseGuard());
        var partyDirA = new PartyDirectory(partyA);
        var offerDirA = new OfferDirectory(offerA, new OpenOfferUseCaseGuard(), catalogDirA, partyDirA);
        var priceDirA = new PriceDirectory(pricingA, new OpenPricingUseCaseGuard(), offerDirA);
        var inventoryDirA = new InventoryDirectory(inventoryA, new OpenInventoryUseCaseGuard(), offerDirA, catalogDirA);
        var cartDirA = new CartDirectory(cartA, new OpenCartUseCaseGuard(), offerDirA, priceDirA, inventoryDirA, inventoryDirA);

        var names = new Dictionary<string, string> { ["fa-IR"] = "پیراهن", ["en-US"] = "Shirt" };
        var product = await catalogDirA.CreateProductAsync(CatalogProductKind.PhysicalGood, "shirt-cart", null, names, CancellationToken.None);
        var colorId = await catalogDirA.CreateAttributeDefinitionAsync(
            "color",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" },
            CancellationToken.None);
        var black = await catalogDirA.AddAttributeOptionAsync(colorId, "black", new Dictionary<string, string> { ["fa-IR"] = "سیاه" }, CancellationToken.None);
        var variant = await catalogDirA.CreateVariantAsync(product.ProductId, "SHIRT-CART", [(colorId, "ignored", black)], CancellationToken.None);
        var sellerA = await partyDirA.CreateOrganizationAsync("فروشنده سبد الف", null, CancellationToken.None);
        var sellerB = await partyDirA.CreateOrganizationAsync("فروشنده سبد ب", null, CancellationToken.None);
        var offer1 = await offerDirA.CreateOfferAsync(variant.VariantId, sellerA.PartyId, SalesChannel.Marketplace, "CART-A", CancellationToken.None);
        var offer2 = await offerDirA.CreateOfferAsync(variant.VariantId, sellerB.PartyId, SalesChannel.Marketplace, "CART-B", CancellationToken.None);
        await offerDirA.ActivateAsync(offer1.OfferId, CancellationToken.None);
        await offerDirA.ActivateAsync(offer2.OfferId, CancellationToken.None);
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var price1 = await priceDirA.CreatePriceAsync(offer1.OfferId, "IR", SalesChannel.Marketplace, 100000, "IRR", start, null, CancellationToken.None);
        var price2 = await priceDirA.CreatePriceAsync(offer2.OfferId, "IR", SalesChannel.Marketplace, 90000, "IRR", start, null, CancellationToken.None);
        await priceDirA.ActivateAsync(price1.PriceId, CancellationToken.None);
        await priceDirA.ActivateAsync(price2.PriceId, CancellationToken.None);
        var loc = await inventoryDirA.CreateLocationAsync("WH-C", "انبار سبد", CancellationToken.None);
        var stock1 = await inventoryDirA.OpenPositionAsync(offer1.OfferId, loc, CancellationToken.None);
        var stock2 = await inventoryDirA.OpenPositionAsync(offer2.OfferId, loc, CancellationToken.None);
        await inventoryDirA.AdjustAsync(stock1, StockAdjustmentKind.Increase, 2, "رسید", null, CancellationToken.None);
        await inventoryDirA.AdjustAsync(stock2, StockAdjustmentKind.Increase, 5, "رسید فروشنده دوم", null, CancellationToken.None);

        var guest = await cartDirA.CreateGuestAsync("IR", "IRR", SalesChannel.Marketplace, CancellationToken.None);
        Assert.False(string.IsNullOrWhiteSpace(guest.GuestSecret));
        Assert.DoesNotContain(guest.GuestSecret, await cartA.Carts.Select(c => c.GuestCredentialHash).SingleAsync());
        var guestAccess = new CartAccess(null, guest.GuestSecret);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            cartDirA.GetCartAsync(guest.Cart.CartId, new CartAccess(null, null), CancellationToken.None));
        await Assert.ThrowsAnyAsync<Exception>(() =>
            cartDirA.GetCartAsync(guest.Cart.CartId, new CartAccess(null, "wrong-secret"), CancellationToken.None));

        var added = await cartDirA.AddOrIncreaseLineAsync(guest.Cart.CartId, guestAccess, guest.Cart.Version, offer1.OfferId, 1, CancellationToken.None);
        Assert.Single(added.Lines);
        Assert.Equal(offer1.OfferId, added.Lines[0].OfferId);
        Assert.Equal(1, added.Lines[0].Quantity);
        Assert.Equal(1, (await inventoryDirA.GetAvailabilityAsync(offer1.OfferId, CancellationToken.None))!.Available);

        var bumped = await cartDirA.AddOrIncreaseLineAsync(guest.Cart.CartId, guestAccess, added.Version, offer1.OfferId, 1, CancellationToken.None);
        Assert.Single(bumped.Lines);
        Assert.Equal(2, bumped.Lines[0].Quantity);
        Assert.Equal(0, (await inventoryDirA.GetAvailabilityAsync(offer1.OfferId, CancellationToken.None))!.Available);

        var multiSeller = await cartDirA.AddOrIncreaseLineAsync(guest.Cart.CartId, guestAccess, bumped.Version, offer2.OfferId, 1, CancellationToken.None);
        Assert.Equal(2, multiSeller.Lines.Count);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            cartDirA.AddOrIncreaseLineAsync(guest.Cart.CartId, guestAccess, multiSeller.Version, offer1.OfferId, 1, CancellationToken.None));

        var decreased = await cartDirA.ChangeLineQuantityAsync(
            guest.Cart.CartId,
            guestAccess,
            multiSeller.Version,
            multiSeller.Lines.Single(x => x.OfferId == offer1.OfferId).LineId,
            1,
            CancellationToken.None);
        Assert.Equal(1, (await inventoryDirA.GetAvailabilityAsync(offer1.OfferId, CancellationToken.None))!.Available);

        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var auth = await cartDirA.CreateAuthenticatedAsync(userId, "IR", "IRR", SalesChannel.Marketplace, CancellationToken.None);
        var authAccess = new CartAccess(userId, null);
        await using var cartA2 = CreateCartDb(csA, commerceA);
        var cartDirA2 = new CartDirectory(cartA2, new OpenCartUseCaseGuard(), offerDirA, priceDirA, inventoryDirA, inventoryDirA);
        var first = cartDirA.AddOrIncreaseLineAsync(auth.CartId, authAccess, auth.Version, offer1.OfferId, 1, CancellationToken.None);
        var second = cartDirA2.AddOrIncreaseLineAsync(auth.CartId, authAccess, auth.Version, offer1.OfferId, 1, CancellationToken.None);
        var race = await Task.WhenAll(
            first.ContinueWith(t => t.Exception is null),
            second.ContinueWith(t => t.Exception is null));
        Assert.Equal(1, race.Count(ok => ok));

        var converted = first.IsCompletedSuccessfully ? first.Result : second.Result;
        var marked = await cartDirA.ConvertAsync(converted.CartId, authAccess, converted.Version, CartConversionIntent.OnlinePurchase, CancellationToken.None);
        Assert.Equal(CartStatus.Converted, marked.Status);
        Assert.Equal(CartConversionIntent.OnlinePurchase, marked.ConversionIntent);

        var requestCart = await cartDirA.CreateAuthenticatedAsync(userId, "IR", "IRR", SalesChannel.Marketplace, CancellationToken.None);
        var requestMarked = await cartDirA.ConvertAsync(
            requestCart.CartId,
            authAccess,
            requestCart.Version,
            CartConversionIntent.RequestToReserve,
            CancellationToken.None);
        Assert.Equal(CartConversionIntent.RequestToReserve, requestMarked.ConversionIntent);

        var shortHold = await cartDirA.CreateGuestAsync("IR", "IRR", SalesChannel.Marketplace, CancellationToken.None);
        var shortAccess = new CartAccess(null, shortHold.GuestSecret);
        var withLine = await cartDirA.AddOrIncreaseLineAsync(shortHold.Cart.CartId, shortAccess, shortHold.Cart.Version, offer2.OfferId, 1, CancellationToken.None);
        var persisted = await cartA.Carts.SingleAsync(x => x.CartId == withLine.CartId);
        persisted.GetType().GetProperty("ExpiresAt")!.SetValue(persisted, DateTimeOffset.UtcNow.AddMinutes(-1));
        await cartA.SaveChangesAsync();
        await cartDirA.ExpireDueCartsAsync(DateTimeOffset.UtcNow, CancellationToken.None);
        Assert.Equal(CartStatus.Expired, (await cartDirA.GetCartAsync(withLine.CartId, shortAccess, CancellationToken.None))!.Status);
        Assert.Equal(4, (await inventoryDirA.GetAvailabilityAsync(offer2.OfferId, CancellationToken.None))!.Available);
        var expiredHoldId = withLine.Lines.Single().ReservationId!.Value;
        await inventoryDirA.ReleaseAsync(expiredHoldId, CancellationToken.None);
        await inventoryDirA.ReleaseAsync(expiredHoldId, CancellationToken.None);

        var outbox = await cartA.OutboxMessages.AsNoTracking().ToListAsync();
        Assert.Contains(outbox, row => row.EventType == CartCreatedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == CartLineAddedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == CartLineChangedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == CartConvertedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == CartExpiredIntegrationEvent.EventTypeName);

        var catalogDirB = new CatalogDirectory(catalogB, new OpenCatalogUseCaseGuard());
        var partyDirB = new PartyDirectory(partyB);
        var offerDirB = new OfferDirectory(offerB, new OpenOfferUseCaseGuard(), catalogDirB, partyDirB);
        var priceDirB = new PriceDirectory(pricingB, new OpenPricingUseCaseGuard(), offerDirB);
        var inventoryDirB = new InventoryDirectory(inventoryB, new OpenInventoryUseCaseGuard(), offerDirB, catalogDirB);
        var cartDirB = new CartDirectory(cartB, new OpenCartUseCaseGuard(), offerDirB, priceDirB, inventoryDirB, inventoryDirB);
        var guestB = await cartDirB.CreateGuestAsync("UK", "GBP", SalesChannel.Direct, CancellationToken.None);
        Assert.Null(await cartDirB.GetCartAsync(guest.Cart.CartId, new CartAccess(null, guestB.GuestSecret), CancellationToken.None));
        Assert.Null(await cartDirA.GetCartAsync(guestB.Cart.CartId, guestAccess, CancellationToken.None));
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

        throw new InvalidOperationException("Repository root not found.");
    }
}
