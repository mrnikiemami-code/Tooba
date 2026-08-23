using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
using Tooba.Inventory.Infrastructure;
using Tooba.Inventory.Infrastructure.Events;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation Inventory: موجودی جدا از Product/Offer/Price، چندمحل، رزرو همزمان امن، و ایزولهٔ Tenant.
/// </summary>
[Collection("PostgresSerial")]
public sealed class InventoryFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_inventory_a")
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
    public void Product_and_offer_have_no_stock_fields()
    {
        var product = typeof(CatalogProduct).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        var offer = typeof(SellerOffer).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Stock", product);
        Assert.DoesNotContain("Quantity", product);
        Assert.DoesNotContain("OnHand", product);
        Assert.DoesNotContain("Inventory", product);
        Assert.DoesNotContain("Stock", offer);
        Assert.DoesNotContain("Quantity", offer);
        Assert.DoesNotContain("OnHand", offer);
        Assert.DoesNotContain("Reserved", offer);
        Assert.Contains("OnHand", typeof(StockPosition).GetProperties().Select(p => p.Name));
        Assert.Contains("Reserved", typeof(StockPosition).GetProperties().Select(p => p.Name));
    }

    [Fact]
    public void Inventory_projects_do_not_reference_masstransit_authzed_or_foreign_infrastructure()
    {
        var root = FindRepoRoot();
        foreach (var project in new[]
                 {
                     Path.Combine(root, "src", "backend", "Modules", "Inventory", "Tooba.Inventory.Domain"),
                     Path.Combine(root, "src", "backend", "Modules", "Inventory", "Tooba.Inventory.Application"),
                     Path.Combine(root, "src", "backend", "Modules", "Inventory", "Tooba.Inventory.Infrastructure"),
                 })
        {
            var csproj = File.ReadAllText(Directory.GetFiles(project, "*.csproj").Single());
            Assert.DoesNotContain("MassTransit", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authzed", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Tooba.Offer.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Catalog.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Identity", csproj, StringComparison.Ordinal);
        }

        Assert.Contains("Tooba.Offer.Application", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Inventory", "Tooba.Inventory.Infrastructure", "Tooba.Inventory.Infrastructure.csproj")));
        Assert.Equal("inventory", InventoryDbContext.Schema);
        Assert.DoesNotContain("MassTransit", typeof(StockPosition).Assembly.GetReferencedAssemblies().Select(a => a.Name));
        Assert.DoesNotContain("MassTransit", typeof(IInventoryDirectory).Assembly.GetReferencedAssemblies().Select(a => a.Name));
    }

    [Fact]
    public void Negative_and_over_reserved_states_are_rejected()
    {
        Assert.ThrowsAny<Exception>(() => StockPosition.EnsureLegal(-1, 0));
        Assert.ThrowsAny<Exception>(() => StockPosition.EnsureLegal(1, -1));
        Assert.ThrowsAny<Exception>(() => StockPosition.EnsureLegal(1, 2));
        StockPosition.EnsureLegal(5, 5);
    }

    [SkippableFact]
    public async Task Inventory_reservation_concurrency_locations_sellers_and_tenant_isolation_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var csA = _container.GetConnectionString();
        await using (var admin = new Npgsql.NpgsqlConnection(csA))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'tooba_inventory_b'";
            if (await cmd.ExecuteScalarAsync() is null)
            {
                await using var create = admin.CreateCommand();
                create.CommandText = "CREATE DATABASE tooba_inventory_b";
                await create.ExecuteNonQueryAsync();
            }
        }

        var csB = new Npgsql.NpgsqlConnectionStringBuilder(csA) { Database = "tooba_inventory_b" }.ConnectionString;
        var commerceA = new FixedCommerceContext();
        commerceA.Assign(OutboxTestContextFactory.SingleStore("tenant-a", "tenant-a"));
        var commerceB = new FixedCommerceContext();
        commerceB.Assign(OutboxTestContextFactory.SingleStore("tenant-b", "tenant-b"));

        await using var catalogA = CreateCatalogDb(csA, commerceA);
        await using var partyA = CreatePartyDb(csA, commerceA);
        await using var offerA = CreateOfferDb(csA, commerceA);
        await using var inventoryA = CreateInventoryDb(csA, commerceA);
        await using var catalogB = CreateCatalogDb(csB, commerceB);
        await using var partyB = CreatePartyDb(csB, commerceB);
        await using var offerB = CreateOfferDb(csB, commerceB);
        await using var inventoryB = CreateInventoryDb(csB, commerceB);
        await catalogA.Database.MigrateAsync();
        await partyA.Database.MigrateAsync();
        await offerA.Database.MigrateAsync();
        await inventoryA.Database.MigrateAsync();
        await catalogB.Database.MigrateAsync();
        await partyB.Database.MigrateAsync();
        await offerB.Database.MigrateAsync();
        await inventoryB.Database.MigrateAsync();

        var catalogDirA = new CatalogDirectory(catalogA, new OpenCatalogUseCaseGuard());
        var partyDirA = new PartyDirectory(partyA);
        var offerDirA = new OfferDirectory(offerA, new OpenOfferUseCaseGuard(), catalogDirA, partyDirA);
        var inventoryDirA = new InventoryDirectory(inventoryA, new OpenInventoryUseCaseGuard(), offerDirA, catalogDirA);
        var catalogDirB = new CatalogDirectory(catalogB, new OpenCatalogUseCaseGuard());
        var partyDirB = new PartyDirectory(partyB);
        var offerDirB = new OfferDirectory(offerB, new OpenOfferUseCaseGuard(), catalogDirB, partyDirB);
        var inventoryDirB = new InventoryDirectory(inventoryB, new OpenInventoryUseCaseGuard(), offerDirB, catalogDirB);

        var names = new Dictionary<string, string> { ["fa-IR"] = "پیراهن", ["en-US"] = "Shirt" };
        var product = await catalogDirA.CreateProductAsync(CatalogProductKind.PhysicalGood, "shirt-inv", null, names, CancellationToken.None);
        var colorId = await catalogDirA.CreateAttributeDefinitionAsync(
            "color",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" },
            CancellationToken.None);
        var black = await catalogDirA.AddAttributeOptionAsync(colorId, "black", new Dictionary<string, string> { ["fa-IR"] = "سیاه" }, CancellationToken.None);
        var variant = await catalogDirA.CreateVariantAsync(product.ProductId, "SHIRT-INV", [(colorId, "ignored", black)], CancellationToken.None);
        var sellerA = await partyDirA.CreateOrganizationAsync("فروشنده موجودی الف", null, CancellationToken.None);
        var sellerB = await partyDirA.CreateOrganizationAsync("فروشنده موجودی ب", null, CancellationToken.None);
        var offer1 = await offerDirA.CreateOfferAsync(variant.VariantId, sellerA.PartyId, SalesChannel.Marketplace, "INV-A", CancellationToken.None);
        var offer2 = await offerDirA.CreateOfferAsync(variant.VariantId, sellerB.PartyId, SalesChannel.Marketplace, "INV-B", CancellationToken.None);

        var loc1 = await inventoryDirA.CreateLocationAsync("WH-1", "انبار یک", CancellationToken.None);
        var loc2 = await inventoryDirA.CreateLocationAsync("WH-2", "انبار دو", CancellationToken.None);
        var stock1 = await inventoryDirA.OpenPositionAsync(offer1.OfferId, loc1, CancellationToken.None);
        var stock1b = await inventoryDirA.OpenPositionAsync(offer1.OfferId, loc2, CancellationToken.None);
        var stock2 = await inventoryDirA.OpenPositionAsync(offer2.OfferId, loc1, CancellationToken.None);
        await inventoryDirA.AdjustAsync(stock1, StockAdjustmentKind.Increase, 1, "رسید", null, CancellationToken.None);
        await inventoryDirA.AdjustAsync(stock1b, StockAdjustmentKind.Increase, 4, "رسید محل دوم", null, CancellationToken.None);
        await inventoryDirA.AdjustAsync(stock2, StockAdjustmentKind.Increase, 9, "رسید فروشنده دیگر", null, CancellationToken.None);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            inventoryDirA.AdjustAsync(stock1, StockAdjustmentKind.Decrease, 50, "کاهش غیرمجاز", null, CancellationToken.None));

        await using var inventoryA2 = CreateInventoryDb(csA, commerceA);
        var inventoryDirA2 = new InventoryDirectory(inventoryA2, new OpenInventoryUseCaseGuard(), offerDirA, catalogDirA);
        var first = inventoryDirA.ReserveAsync(stock1, 1, "cart-1", null, null, CancellationToken.None);
        var second = inventoryDirA2.ReserveAsync(stock1, 1, "cart-2", null, null, CancellationToken.None);
        var results = await Task.WhenAll(
            first.ContinueWith(t => t.Exception is null),
            second.ContinueWith(t => t.Exception is null));
        Assert.Equal(1, results.Count(ok => ok));

        var held = first.IsCompletedSuccessfully ? first.Result : second.Result;
        var snapshot = await inventoryDirA.GetAvailabilityAsync(offer1.OfferId, CancellationToken.None);
        Assert.NotNull(snapshot);
        Assert.Equal(5, snapshot!.OnHand);
        Assert.Equal(1, snapshot.Reserved);
        Assert.Equal(4, snapshot.Available);
        Assert.Equal(2, snapshot.Locations.Count);

        var seller2Avail = await inventoryDirA.GetAvailabilityAsync(offer2.OfferId, CancellationToken.None);
        Assert.Equal(9, seller2Avail!.Available);

        await inventoryDirA.ReleaseAsync(held.ReservationId, CancellationToken.None);
        Assert.Equal(5, (await inventoryDirA.GetAvailabilityAsync(offer1.OfferId, CancellationToken.None))!.Available);
        var again = await inventoryDirA.ReserveAsync(stock1, 1, "cart-3", "idem-1", null, CancellationToken.None);
        var dup = await inventoryDirA.ReserveAsync(stock1, 1, "cart-3", "idem-1", null, CancellationToken.None);
        Assert.Equal(again.ReservationId, dup.ReservationId);
        await inventoryDirA.ConsumeAsync(again.ReservationId, CancellationToken.None);
        Assert.Equal(4, (await inventoryDirA.GetAvailabilityAsync(offer1.OfferId, CancellationToken.None))!.OnHand);
        Assert.Equal(0, (await inventoryDirA.GetAvailabilityAsync(offer1.OfferId, CancellationToken.None))!.Reserved);

        var outbox = await inventoryA.OutboxMessages.AsNoTracking().ToListAsync();
        Assert.Contains(outbox, row => row.EventType == InventoryAdjustedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == InventoryReservedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == InventoryReleasedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == InventoryReservationConsumedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == InventoryAvailabilityChangedIntegrationEvent.EventTypeName);

        var productB = await catalogDirB.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "other-inv",
            null,
            new Dictionary<string, string> { ["en-US"] = "Other" },
            CancellationToken.None);
        var sizeId = await catalogDirB.CreateAttributeDefinitionAsync(
            "size",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["en-US"] = "Size" },
            CancellationToken.None);
        var m = await catalogDirB.AddAttributeOptionAsync(sizeId, "m", new Dictionary<string, string> { ["en-US"] = "M" }, CancellationToken.None);
        var variantB = await catalogDirB.CreateVariantAsync(productB.ProductId, "OTHER-INV", [(sizeId, "ignored", m)], CancellationToken.None);
        var sellerTenantB = await partyDirB.CreateOrganizationAsync("Tenant B Seller", null, CancellationToken.None);
        var offerBRef = await offerDirB.CreateOfferAsync(variantB.VariantId, sellerTenantB.PartyId, SalesChannel.Direct, "B-INV", CancellationToken.None);
        var locB = await inventoryDirB.CreateLocationAsync("WH-B", "Tenant B WH", CancellationToken.None);
        var stockB = await inventoryDirB.OpenPositionAsync(offerBRef.OfferId, locB, CancellationToken.None);
        await inventoryDirB.AdjustAsync(stockB, StockAdjustmentKind.Increase, 2, "seed", null, CancellationToken.None);
        Assert.Null(await inventoryDirB.GetAvailabilityAsync(offer1.OfferId, CancellationToken.None));
        Assert.Null(await inventoryDirA.GetAvailabilityAsync(offerBRef.OfferId, CancellationToken.None));
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
