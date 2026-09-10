using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Fulfillment.Infrastructure;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P09-T021: بسته تجمیعی — ایجاد، قفل عضو، ارکستراسیون ارسال/تحویل، لغو سفارش.
/// </summary>
[Collection("PostgresSerial")]
public sealed class ConsolidatedPackageTests : IAsyncLifetime
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
                .WithDatabase("tooba_consolidated_pkg")
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
    public void Domain_create_requires_two_distinct_sellers()
    {
        var now = DateTimeOffset.UtcNow;
        var sellerA = Guid.NewGuid();
        var sellerB = Guid.NewGuid();
        var package = ConsolidatedPackage.Create(
            Guid.NewGuid(),
            [
                (Guid.NewGuid(), sellerA, Guid.NewGuid()),
                (Guid.NewGuid(), sellerB, Guid.NewGuid()),
            ],
            "post",
            "پست",
            "TRK-CENTRAL",
            null,
            Guid.NewGuid(),
            now);
        Assert.Equal(ConsolidatedPackageStatus.Created, package.Status);
        Assert.StartsWith("MP-", package.PackageNumber, StringComparison.Ordinal);
        Assert.Equal(2, package.Members.Count);

        var singleSeller = Assert.Throws<InvalidOperationException>(() =>
            ConsolidatedPackage.Create(
                Guid.NewGuid(),
                [
                    (Guid.NewGuid(), sellerA, Guid.NewGuid()),
                    (Guid.NewGuid(), sellerA, Guid.NewGuid()),
                ],
                "post",
                "پست",
                null,
                null,
                null,
                now));
        Assert.Equal("fulfillment.package.requires_multi_seller", singleSeller.Message);
    }

    [Fact]
    public void Domain_cancel_releases_membership_and_blocks_after_dispatch()
    {
        var now = DateTimeOffset.UtcNow;
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        var package = ConsolidatedPackage.Create(
            Guid.NewGuid(),
            [(s1, Guid.NewGuid(), Guid.NewGuid()), (s2, Guid.NewGuid(), Guid.NewGuid())],
            "post",
            "پست",
            null,
            null,
            null,
            now);
        package.Cancel(now.AddMinutes(1));
        Assert.Equal(ConsolidatedPackageStatus.Cancelled, package.Status);
        Assert.All(package.Members, m => Assert.NotNull(m.ReleasedAt));

        var again = ConsolidatedPackage.Create(
            Guid.NewGuid(),
            [(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())],
            "tipax",
            "تیپاکس",
            null,
            null,
            null,
            now);
        again.MarkDispatched(
            now,
            new Dictionary<Guid, ShipmentStatus>
            {
                [again.Members[0].ShipmentId] = ShipmentStatus.Dispatched,
                [again.Members[1].ShipmentId] = ShipmentStatus.Dispatched,
            });
        var ex = Assert.Throws<InvalidOperationException>(() => again.Cancel(now));
        Assert.Equal("fulfillment.package.cancel_after_dispatch", ex.Message);
    }

    [SkippableFact]
    public async Task Create_requires_two_sellers_and_rejects_single_seller_and_duplicate_membership()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var (directory, fulfillmentDb, checkoutId, actor, ready) = await SeedTwoSellerReadyShipmentsAsync();

        var single = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.CreateConsolidatedPackageAsync(
                checkoutId,
                [ready[0].ShipmentId],
                "post",
                null,
                null,
                actor,
                CancellationToken.None));
        Assert.Equal("fulfillment.package.requires_multi_seller", single.Message);

        var sameSellerOnly = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.CreateConsolidatedPackageAsync(
                checkoutId,
                [ready[0].ShipmentId, ready[0].ShipmentId],
                "post",
                null,
                null,
                actor,
                CancellationToken.None));
        Assert.Equal("fulfillment.package.duplicate_shipment", sameSellerOnly.Message);

        var created = await directory.CreateConsolidatedPackageAsync(
            checkoutId,
            [ready[0].ShipmentId, ready[1].ShipmentId],
            "post",
            "TRK-MP-1",
            "note",
            actor,
            CancellationToken.None);
        Assert.Equal(ConsolidatedPackageStatus.Created, created.Status);
        Assert.Equal(2, created.Members.Count);
        Assert.StartsWith("MP-", created.PackageNumber, StringComparison.Ordinal);

        var duplicate = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.CreateConsolidatedPackageAsync(
                checkoutId,
                [ready[0].ShipmentId, ready[1].ShipmentId],
                "post",
                null,
                null,
                actor,
                CancellationToken.None));
        Assert.Equal("fulfillment.package.shipment_already_member", duplicate.Message);

        Assert.True(await directory.IsShipmentLockedByPackageAsync(ready[0].ShipmentId, CancellationToken.None));
        Assert.Equal(1, await fulfillmentDb.ConsolidatedPackages.CountAsync());
    }

    [SkippableFact]
    public async Task Concurrent_create_rejects_double_active_membership()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var (directory, fulfillmentDb, checkoutId, actor, ready) = await SeedTwoSellerReadyShipmentsAsync(dbSuffix: "race");
        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-pkg-race", "tenant-pkg-race"));
        var dbB = CreateFulfillmentDb(cs, commerce);
        var directoryB = new FulfillmentDirectory(
            dbB,
            new OpenFulfillmentUseCaseGuard(),
            new OrderFulfillmentBridge(CreateOrderDb(cs, commerce)),
            new NoopInventoryGateway(),
            new FulfillmentInstrumentation());

        var shipmentIds = new[] { ready[0].ShipmentId, ready[1].ShipmentId };
        var results = await Task.WhenAll(
            TryCreateAsync(directory, checkoutId, shipmentIds, actor),
            TryCreateAsync(directoryB, checkoutId, shipmentIds, actor));

        Assert.Contains(results, x => x.Ok);
        Assert.Contains(results, x => !x.Ok);
        Assert.Contains(
            results.Where(x => !x.Ok).Select(x => x.Error),
            e => e is "fulfillment.package.shipment_already_member"
                or "fulfillment.package.duplicate_shipment"
                or "23505");
        Assert.Equal(1, await fulfillmentDb.ConsolidatedPackages.CountAsync(x => x.Status == ConsolidatedPackageStatus.Created));
    }

    private static async Task<(bool Ok, string? Error)> TryCreateAsync(
        FulfillmentDirectory directory,
        Guid checkoutId,
        IReadOnlyList<Guid> shipmentIds,
        Guid actor)
    {
        try
        {
            await directory.CreateConsolidatedPackageAsync(
                checkoutId,
                shipmentIds,
                "post",
                null,
                null,
                actor,
                CancellationToken.None);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    [SkippableFact]
    public async Task Cancel_releases_lock_and_allows_rebuild()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var (directory, _, checkoutId, actor, ready) = await SeedTwoSellerReadyShipmentsAsync();
        var created = await directory.CreateConsolidatedPackageAsync(
            checkoutId,
            [ready[0].ShipmentId, ready[1].ShipmentId],
            "post",
            null,
            null,
            actor,
            CancellationToken.None);

        var cancelled = await directory.CancelConsolidatedPackageAsync(
            created.ConsolidatedPackageId,
            actor,
            CancellationToken.None);
        Assert.Equal(ConsolidatedPackageStatus.Cancelled, cancelled.Status);
        Assert.False(await directory.IsShipmentLockedByPackageAsync(ready[0].ShipmentId, CancellationToken.None));

        var rebuilt = await directory.CreateConsolidatedPackageAsync(
            checkoutId,
            [ready[0].ShipmentId, ready[1].ShipmentId],
            "tipax",
            null,
            null,
            actor,
            CancellationToken.None);
        Assert.Equal(ConsolidatedPackageStatus.Created, rebuilt.Status);
        Assert.NotEqual(created.ConsolidatedPackageId, rebuilt.ConsolidatedPackageId);

        var all = await directory.GetPackagesForCheckoutAsync(checkoutId, CancellationToken.None);
        Assert.Equal(2, all.Count);
        Assert.Contains(all, x => x.Status == ConsolidatedPackageStatus.Cancelled);
        Assert.Contains(all, x => x.Status == ConsolidatedPackageStatus.Created);
    }

    [SkippableFact]
    public async Task Direct_member_dispatch_blocked_while_locked_and_package_dispatch_orchestrates()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var (directory, _, checkoutId, actor, ready) = await SeedTwoSellerReadyShipmentsAsync();
        foreach (var member in ready)
        {
            await directory.AssignTrackingAsync(
                member.FulfillmentId,
                member.ShipmentId,
                actor,
                $"TRK-{member.ShipmentId:N}"[..20],
                CancellationToken.None);
        }

        var created = await directory.CreateConsolidatedPackageAsync(
            checkoutId,
            [ready[0].ShipmentId, ready[1].ShipmentId],
            "post",
            "TRK-CENTRAL",
            null,
            actor,
            CancellationToken.None);

        var blocked = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.DispatchShipmentAsync(
                ready[0].FulfillmentId,
                ready[0].ShipmentId,
                actor,
                CancellationToken.None));
        Assert.Equal("fulfillment.shipment.locked_by_consolidated_package", blocked.Message);

        var dispatched = await directory.DispatchConsolidatedPackageAsync(
            created.ConsolidatedPackageId,
            actor,
            CancellationToken.None);
        Assert.Equal(ConsolidatedPackageStatus.Dispatched, dispatched.Status);

        foreach (var member in ready)
        {
            var snap = await directory.GetAsync(member.FulfillmentId, CancellationToken.None);
            var shipment = Assert.Single(snap!.Shipments, x => x.ShipmentId == member.ShipmentId);
            Assert.Equal(ShipmentStatus.Dispatched, shipment.Status);
        }

        var idempotent = await directory.DispatchConsolidatedPackageAsync(
            created.ConsolidatedPackageId,
            actor,
            CancellationToken.None);
        Assert.Equal(ConsolidatedPackageStatus.Dispatched, idempotent.Status);
    }

    [SkippableFact]
    public async Task Deliver_orchestrates_members_and_order_cancel_voids_package()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var (directory, _, checkoutId, actor, ready) = await SeedTwoSellerReadyShipmentsAsync();
        foreach (var member in ready)
        {
            await directory.AssignTrackingAsync(
                member.FulfillmentId,
                member.ShipmentId,
                actor,
                $"TRK-D-{member.ShipmentId:N}"[..22],
                CancellationToken.None);
        }

        var created = await directory.CreateConsolidatedPackageAsync(
            checkoutId,
            [ready[0].ShipmentId, ready[1].ShipmentId],
            "post",
            null,
            null,
            actor,
            CancellationToken.None);
        await directory.DispatchConsolidatedPackageAsync(created.ConsolidatedPackageId, actor, CancellationToken.None);

        var deliverBlocked = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.DeliverShipmentAsync(
                ready[0].FulfillmentId,
                ready[0].ShipmentId,
                actor,
                CancellationToken.None));
        Assert.Equal("fulfillment.shipment.locked_by_consolidated_package", deliverBlocked.Message);

        var delivered = await directory.DeliverConsolidatedPackageAsync(
            created.ConsolidatedPackageId,
            actor,
            CancellationToken.None);
        Assert.Equal(ConsolidatedPackageStatus.Delivered, delivered.Status);
        foreach (var member in ready)
        {
            var snap = await directory.GetAsync(member.FulfillmentId, CancellationToken.None);
            var shipment = Assert.Single(snap!.Shipments, x => x.ShipmentId == member.ShipmentId);
            Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        }

        var (directory2, _, checkoutId2, actor2, ready2) = await SeedTwoSellerReadyShipmentsAsync(dbSuffix: "cancel");
        var preDispatch = await directory2.CreateConsolidatedPackageAsync(
            checkoutId2,
            [ready2[0].ShipmentId, ready2[1].ShipmentId],
            "post",
            null,
            null,
            actor2,
            CancellationToken.None);
        await directory2.AbortForCheckoutCancelAsync(checkoutId2, CancellationToken.None);
        var packages = await directory2.GetPackagesForCheckoutAsync(checkoutId2, CancellationToken.None);
        var voided = Assert.Single(packages, x => x.ConsolidatedPackageId == preDispatch.ConsolidatedPackageId);
        Assert.Equal(ConsolidatedPackageStatus.Cancelled, voided.Status);
        Assert.False(await directory2.IsShipmentLockedByPackageAsync(ready2[0].ShipmentId, CancellationToken.None));
        foreach (var member in ready2)
        {
            var snap = await directory2.GetAsync(member.FulfillmentId, CancellationToken.None);
            Assert.Equal(FulfillmentStatus.Cancelled, snap!.Status);
            Assert.Equal(ShipmentStatus.Cancelled, snap.Shipments.Single(x => x.ShipmentId == member.ShipmentId).Status);
        }
    }

    private async Task<(
        FulfillmentDirectory Directory,
        FulfillmentDbContext FulfillmentDb,
        Guid CheckoutId,
        Guid Actor,
        IReadOnlyList<(Guid FulfillmentId, Guid ShipmentId)> Ready)> SeedTwoSellerReadyShipmentsAsync(
        string dbSuffix = "main")
    {
        var cs = _container!.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore($"store-pkg-{dbSuffix}", $"tenant-pkg-{dbSuffix}"));

        var orderDb = CreateOrderDb(cs, commerce);
        var fulfillmentDb = CreateFulfillmentDb(cs, commerce);
        await orderDb.Database.MigrateAsync();
        await fulfillmentDb.Database.MigrateAsync();

        var buyer = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var sellerA = Guid.NewGuid();
        var sellerB = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var checkoutId = Guid.NewGuid();
        var orderAId = Guid.NewGuid();
        var orderBId = Guid.NewGuid();
        var lineA = OrderLine.FromCheckout(
            orderAId, Guid.NewGuid(), Guid.NewGuid(), sellerA, 1, 50000m, "IRR", true, Guid.NewGuid(), Guid.NewGuid(),
            "Taxable", 0.09m, 9000m, 59000m, null);
        var lineB = OrderLine.FromCheckout(
            orderBId, Guid.NewGuid(), Guid.NewGuid(), sellerB, 1, 40000m, "IRR", true, Guid.NewGuid(), Guid.NewGuid(),
            "Taxable", 0.09m, 3600m, 43600m, null);
        var soA = SellerOrder.Open(checkoutId, sellerA, $"SO-{orderAId:N}"[..20], OrderMode.OnlinePurchase, "IRR", [lineA]);
        var soB = SellerOrder.Open(checkoutId, sellerB, $"SO-{orderBId:N}"[..20], OrderMode.OnlinePurchase, "IRR", [lineB]);
        var group = CheckoutGroup.Submit(
            checkoutId, $"idem-{checkoutId:N}", Guid.NewGuid(), OrderMode.OnlinePurchase, buyer, actor,
            "IR", "IRR", SalesChannel.Marketplace, [soA, soB], now);
        foreach (var sellerOrder in group.SellerOrders)
        {
            sellerOrder.RecordVerifiedPayment();
        }

        orderDb.Checkouts.Add(group);
        await orderDb.SaveChangesAsync();

        var directory = new FulfillmentDirectory(
            fulfillmentDb,
            new OpenFulfillmentUseCaseGuard(),
            new OrderFulfillmentBridge(orderDb),
            new NoopInventoryGateway(),
            new FulfillmentInstrumentation());

        await directory.CreateFromPaidSellerOrdersAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [soA.SellerOrderId, soB.SellerOrderId],
            CancellationToken.None);

        var ready = new List<(Guid FulfillmentId, Guid ShipmentId)>();
        foreach (var sellerOrderId in new[] { soA.SellerOrderId, soB.SellerOrderId })
        {
            var unit = await directory.GetBySellerOrderAsync(sellerOrderId, CancellationToken.None);
            Assert.NotNull(unit);
            await directory.MarkProcessingAsync(unit!.FulfillmentId, actor, CancellationToken.None);
            await directory.MarkPackedAsync(unit.FulfillmentId, actor, CancellationToken.None);
            var shipped = await directory.CreateShipmentAsync(
                unit.FulfillmentId,
                actor,
                "Carrier Demo",
                [new ShipmentLineCommand(unit.Items[0].OrderLineId, 1)],
                CancellationToken.None);
            ready.Add((unit.FulfillmentId, shipped.Shipments.Single().ShipmentId));
        }

        return (directory, fulfillmentDb, checkoutId, actor, ready);
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

    private static FulfillmentDbContext CreateFulfillmentDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new FulfillmentOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, FulfillmentDbContext.Schema, typeof(FulfillmentDbContext));
        options.AddInterceptors(interceptor);
        return new FulfillmentDbContext(options.Options);
    }

    private sealed class NoopInventoryGateway : IFulfillmentInventoryGateway
    {
        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
