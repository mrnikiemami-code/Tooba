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
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P09-T022-R3: paid-order reservation commit clears cart TTL so expiry worker cannot release paid holds.
/// </summary>
[Collection("PostgresSerial")]
public sealed class PaidOrderReservationLifecycleTests : IAsyncLifetime
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
                .WithDatabase("tooba_paid_res_lifecycle")
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
    public void CommitForPaidOrder_clears_expires_at_and_is_idempotent()
    {
        var now = DateTimeOffset.UtcNow;
        var hold = StockReservation.Hold(Guid.NewGuid(), 1.25m, "cart", null, now, now.AddMinutes(30));
        Assert.NotNull(hold.ExpiresAt);

        hold.CommitForPaidOrder(now.AddSeconds(1));
        Assert.Null(hold.ExpiresAt);
        Assert.Equal(StockReservationStatus.Held, hold.Status);
        Assert.Equal(1.25m, hold.Quantity);

        var updated = hold.UpdatedAt;
        hold.CommitForPaidOrder(now.AddSeconds(2));
        Assert.Null(hold.ExpiresAt);
        Assert.Equal(StockReservationStatus.Held, hold.Status);
        Assert.True(hold.UpdatedAt >= updated);
    }

    [Fact]
    public void CommitForPaidOrder_rejects_released_without_resurrect()
    {
        var now = DateTimeOffset.UtcNow;
        var hold = StockReservation.Hold(Guid.NewGuid(), 2m, "cart", null, now, now.AddMinutes(10));
        hold.MoveTo(StockReservationStatus.Released, now.AddSeconds(1));

        var ex = Assert.Throws<InvalidOperationException>(() => hold.CommitForPaidOrder(now.AddSeconds(2)));
        Assert.Equal("inventory.reservation.not_active", ex.Message);
        Assert.Equal(StockReservationStatus.Released, hold.Status);
        Assert.NotNull(hold.ExpiresAt);
    }

    [Fact]
    public void CommitForPaidOrder_rejects_consumed_without_resurrect()
    {
        var now = DateTimeOffset.UtcNow;
        var hold = StockReservation.Hold(Guid.NewGuid(), 1m, "cart", null, now, now.AddMinutes(10));
        hold.MoveTo(StockReservationStatus.Consumed, now.AddSeconds(1));

        var ex = Assert.Throws<InvalidOperationException>(() => hold.CommitForPaidOrder(now.AddSeconds(2)));
        Assert.Equal("inventory.reservation.not_active", ex.Message);
        Assert.Equal(StockReservationStatus.Consumed, hold.Status);
    }

    [SkippableFact]
    public async Task Cart_style_reserve_commit_survives_expiry_worker_and_preserves_decimal_quantity()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-paid-res", "tenant-paid-res"));

        await using var catalog = CreateCatalogDb(cs, commerce);
        await using var party = CreatePartyDb(cs, commerce);
        await using var offer = CreateOfferDb(cs, commerce);
        await using var inventory = CreateInventoryDb(cs, commerce);
        await catalog.Database.MigrateAsync();
        await party.Database.MigrateAsync();
        await offer.Database.MigrateAsync();
        await inventory.Database.MigrateAsync();

        var catalogDir = new CatalogDirectory(catalog, new OpenCatalogUseCaseGuard());
        var partyDir = new PartyDirectory(party);
        var offerDir = new OfferDirectory(offer, new OpenOfferUseCaseGuard(), catalogDir, partyDir);
        var inventoryDir = new InventoryDirectory(inventory, new OpenInventoryUseCaseGuard(), offerDir, catalogDir);

        var names = new Dictionary<string, string> { ["fa-IR"] = "کالا", ["en-US"] = "Item" };
        var product = await catalogDir.CreateProductAsync(CatalogProductKind.PhysicalGood, "paid-res", null, names, CancellationToken.None);
        var sizeId = await catalogDir.CreateAttributeDefinitionAsync(
            "size-pr",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["en-US"] = "Size" },
            CancellationToken.None);
        var medium = await catalogDir.AddAttributeOptionAsync(sizeId, "m", new Dictionary<string, string> { ["en-US"] = "M" }, CancellationToken.None);
        var variant = await catalogDir.CreateVariantAsync(product.ProductId, "PAID-RES-1", [(sizeId, "ignored", medium)], CancellationToken.None);
        var seller = await partyDir.CreateOrganizationAsync("فروشنده paid-res", null, CancellationToken.None);
        var offerRef = await offerDir.CreateOfferAsync(variant.VariantId, seller.PartyId, SalesChannel.Marketplace, "PAID-RES-OFFER", CancellationToken.None);
        var location = await inventoryDir.CreateLocationAsync("WH-PR", "PaidRes WH", CancellationToken.None);
        var stock = await inventoryDir.OpenPositionAsync(offerRef.OfferId, location, CancellationToken.None);
        await inventoryDir.AdjustAsync(stock, StockAdjustmentKind.Increase, 10m, "seed", null, CancellationToken.None);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        var reserved = await inventoryDir.ReserveAsync(
            stock,
            1.25m,
            "cart-paid-res",
            "paid-res-idem",
            expiresAt,
            CancellationToken.None);
        Assert.Equal(1.25m, reserved.Quantity);
        Assert.NotNull(reserved.ExpiresAt);
        Assert.Equal(StockReservationStatus.Held, reserved.Status);

        // Simulate cart TTL already elapsed before payment commit (race window).
        await inventory.Reservations
            .Where(x => x.ReservationId == reserved.ReservationId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-2)));

        var committed = await inventoryDir.CommitReservationForPaidOrderAsync(reserved.ReservationId, CancellationToken.None);
        Assert.Null(committed.ExpiresAt);
        Assert.Equal(StockReservationStatus.Held, committed.Status);
        Assert.Equal(1.25m, committed.Quantity);

        var again = await inventoryDir.CommitReservationForPaidOrderAsync(reserved.ReservationId, CancellationToken.None);
        Assert.Null(again.ExpiresAt);
        Assert.Equal(1.25m, again.Quantity);

        var released = await inventoryDir.ReleaseExpiredHoldsAsync(DateTimeOffset.UtcNow.AddMinutes(5), 50, CancellationToken.None);
        Assert.Equal(0, released);

        var afterExpiry = await inventoryDir.FindReservationAsync(reserved.ReservationId, CancellationToken.None);
        Assert.NotNull(afterExpiry);
        Assert.Equal(StockReservationStatus.Held, afterExpiry!.Status);
        Assert.Null(afterExpiry.ExpiresAt);
        Assert.Equal(1.25m, afterExpiry.Quantity);

        var availability = await inventoryDir.GetAvailabilityAsync(offerRef.OfferId, CancellationToken.None);
        Assert.Equal(1.25m, availability!.Reserved);
    }

    [SkippableFact]
    public async Task Commit_on_released_reservation_throws_not_active()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-paid-res-2", "tenant-paid-res-2"));

        await using var catalog = CreateCatalogDb(cs, commerce);
        await using var party = CreatePartyDb(cs, commerce);
        await using var offer = CreateOfferDb(cs, commerce);
        await using var inventory = CreateInventoryDb(cs, commerce);
        await catalog.Database.MigrateAsync();
        await party.Database.MigrateAsync();
        await offer.Database.MigrateAsync();
        await inventory.Database.MigrateAsync();

        var catalogDir = new CatalogDirectory(catalog, new OpenCatalogUseCaseGuard());
        var partyDir = new PartyDirectory(party);
        var offerDir = new OfferDirectory(offer, new OpenOfferUseCaseGuard(), catalogDir, partyDir);
        var inventoryDir = new InventoryDirectory(inventory, new OpenInventoryUseCaseGuard(), offerDir, catalogDir);

        var names = new Dictionary<string, string> { ["fa-IR"] = "کالا۲", ["en-US"] = "Item2" };
        var product = await catalogDir.CreateProductAsync(CatalogProductKind.PhysicalGood, "paid-res-2", null, names, CancellationToken.None);
        var sizeId = await catalogDir.CreateAttributeDefinitionAsync(
            "size-pr2",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["en-US"] = "Size" },
            CancellationToken.None);
        var medium = await catalogDir.AddAttributeOptionAsync(sizeId, "m", new Dictionary<string, string> { ["en-US"] = "M" }, CancellationToken.None);
        var variant = await catalogDir.CreateVariantAsync(product.ProductId, "PAID-RES-2", [(sizeId, "ignored", medium)], CancellationToken.None);
        var seller = await partyDir.CreateOrganizationAsync("فروشنده paid-res-2", null, CancellationToken.None);
        var offerRef = await offerDir.CreateOfferAsync(variant.VariantId, seller.PartyId, SalesChannel.Marketplace, "PAID-RES-OFFER-2", CancellationToken.None);
        var location = await inventoryDir.CreateLocationAsync("WH-PR2", "PaidRes2 WH", CancellationToken.None);
        var stock = await inventoryDir.OpenPositionAsync(offerRef.OfferId, location, CancellationToken.None);
        await inventoryDir.AdjustAsync(stock, StockAdjustmentKind.Increase, 5m, "seed", null, CancellationToken.None);

        var reserved = await inventoryDir.ReserveAsync(
            stock,
            1m,
            "cart-paid-res-2",
            null,
            DateTimeOffset.UtcNow.AddMinutes(30),
            CancellationToken.None);
        await inventoryDir.ReleaseAsync(reserved.ReservationId, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            inventoryDir.CommitReservationForPaidOrderAsync(reserved.ReservationId, CancellationToken.None));
        Assert.Equal("inventory.reservation.not_active", ex.Message);

        var found = await inventoryDir.FindReservationAsync(reserved.ReservationId, CancellationToken.None);
        Assert.Equal(StockReservationStatus.Released, found!.Status);
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
}
