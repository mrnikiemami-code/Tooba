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
/// پوشش foundation fulfillment: Order != Fulfillment != Shipment، handoff Paid، idempotency و چرخهٔ محموله.
/// </summary>
[Collection("PostgresSerial")]
public sealed class FulfillmentFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_fulfillment_a")
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
    /// مرز ماژول و نبود JOIN/SQL مستقیم بین Order و Fulfillment.
    /// </summary>
    [Fact]
    public void Fulfillment_is_not_order_and_modules_do_not_reference_each_other_infrastructure()
    {
        Assert.NotEqual(typeof(FulfillmentUnit), typeof(SellerOrder));
        Assert.NotEqual(typeof(Shipment), typeof(CheckoutGroup));
        Assert.Equal("fulfillment", FulfillmentDbContext.Schema);
        Assert.Equal("order", OrderDbContext.Schema);

        var root = FindRepoRoot();
        foreach (var project in new[]
                 {
                     Path.Combine(root, "src", "backend", "Modules", "Fulfillment", "Tooba.Fulfillment.Domain"),
                     Path.Combine(root, "src", "backend", "Modules", "Fulfillment", "Tooba.Fulfillment.Application"),
                 })
        {
            var csproj = File.ReadAllText(Directory.GetFiles(project, "*.csproj").Single());
            Assert.DoesNotContain("MassTransit", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authzed", csproj, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain(
            "Tooba.Order.Infrastructure",
            File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Fulfillment", "Tooba.Fulfillment.Infrastructure", "Tooba.Fulfillment.Infrastructure.csproj")),
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Tooba.Fulfillment.Infrastructure",
            File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Tooba.Order.Infrastructure.csproj")),
            StringComparison.Ordinal);
        Assert.Contains(
            "Tooba.Order.Application",
            File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Fulfillment", "Tooba.Fulfillment.Infrastructure", "Tooba.Fulfillment.Infrastructure.csproj")),
            StringComparison.Ordinal);
        Assert.Contains(
            "Tooba.Order.Application",
            File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Tooba.Order.Infrastructure.csproj")),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// سفارش Paid، dedup رویداد، snapshot آدرس، multi-shipment، dispatch و مصرف موجودی از قرارداد.
    /// </summary>
    [SkippableFact]
    public async Task Paid_order_handoff_is_idempotent_and_shipment_lifecycle_preserves_boundaries()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-fulfill", "tenant-fulfill"));

        await using var orderDb = CreateOrderDb(cs, commerce);
        await using var fulfillmentDb = CreateFulfillmentDb(cs, commerce);
        await orderDb.Database.MigrateAsync();
        await fulfillmentDb.Database.MigrateAsync();

        var buyer = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var seller = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var reservation = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var now = DateTimeOffset.Parse("2026-08-27T00:00:00Z");

        var checkout = SeedPaidCheckout(
            orderDb,
            buyer,
            actor,
            seller,
            109000m,
            reservation,
            now,
            recipientName: "Snapshot Recipient",
            postalAddress: "Original Address 1");
        await orderDb.SaveChangesAsync();

        var sellerOrderId = checkout.SellerOrders.Single().SellerOrderId;
        var lineId = (await orderDb.Lines.AsNoTracking().SingleAsync()).LineId;
        var unpaidCheckout = SeedPaidCheckout(orderDb, buyer, actor, seller, 50000m, null, now, markPaid: false);
        await orderDb.SaveChangesAsync();
        var unpaidSellerOrderId = unpaidCheckout.SellerOrders.Single().SellerOrderId;

        var inventory = new RecordingInventoryGateway();
        var bridge = new OrderFulfillmentBridge(orderDb);
        var directory = new FulfillmentDirectory(
            fulfillmentDb,
            new OpenFulfillmentUseCaseGuard(),
            bridge,
            inventory,
            new FulfillmentInstrumentation());

        var paymentId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        await directory.CreateFromPaidSellerOrdersAsync(
            paymentId,
            eventId,
            [sellerOrderId],
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.CreateFromPaidSellerOrdersAsync(
                paymentId,
                Guid.NewGuid(),
                [unpaidSellerOrderId],
                CancellationToken.None));

        await directory.CreateFromPaidSellerOrdersAsync(
            paymentId,
            eventId,
            [sellerOrderId],
            CancellationToken.None);

        Assert.Equal(1, await fulfillmentDb.Fulfillments.CountAsync());
        Assert.Equal(1, await fulfillmentDb.PaymentInbox.CountAsync());

        var created = await directory.GetBySellerOrderAsync(sellerOrderId, CancellationToken.None);
        Assert.NotNull(created);
        Assert.Equal(FulfillmentStatus.ReadyToFulfill, created!.Status);
        Assert.Equal("Snapshot Recipient", created.RecipientName);
        Assert.Equal("Original Address 1", created.PostalAddress);
        Assert.Single(created.Items);
        Assert.Equal(reservation, created.Items[0].ReservationId);

        await orderDb.Database.ExecuteSqlRawAsync(
            "UPDATE \"order\".checkouts SET recipient_name = {0}, postal_address = {1} WHERE checkout_id = {2}",
            "Mutated Recipient",
            "Mutated Address",
            checkout.CheckoutId);
        var afterMutation = await directory.GetAsync(created.FulfillmentId, CancellationToken.None);
        Assert.Equal("Snapshot Recipient", afterMutation!.RecipientName);
        Assert.Equal("Original Address 1", afterMutation.PostalAddress);

        var processing = await directory.MarkProcessingAsync(created.FulfillmentId, actor, CancellationToken.None);
        Assert.Equal(FulfillmentStatus.Processing, processing.Status);
        var packed = await directory.MarkPackedAsync(created.FulfillmentId, actor, CancellationToken.None);
        Assert.Equal(FulfillmentStatus.Packed, packed.Status);

        var partial = await directory.CreateShipmentAsync(
            created.FulfillmentId,
            actor,
            "Post Demo",
            [new ShipmentLineCommand(lineId, 1)],
            CancellationToken.None);
        Assert.Single(partial.Shipments);
        var shipmentId = partial.Shipments[0].ShipmentId;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.CreateShipmentAsync(
                created.FulfillmentId,
                actor,
                "Overflow",
                [new ShipmentLineCommand(lineId, 2)],
                CancellationToken.None));

        var tracked = await directory.AssignTrackingAsync(
            created.FulfillmentId,
            shipmentId,
            actor,
            "TRK-001",
            CancellationToken.None);
        Assert.Equal("TRK-001", tracked.Shipments[0].TrackingReference);

        var sameTracking = await directory.AssignTrackingAsync(
            created.FulfillmentId,
            shipmentId,
            actor,
            "TRK-001",
            CancellationToken.None);
        Assert.Equal("TRK-001", sameTracking.Shipments[0].TrackingReference);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.AssignTrackingAsync(
                created.FulfillmentId,
                shipmentId,
                actor,
                "TRK-002",
                CancellationToken.None));

        var dispatched = await directory.DispatchShipmentAsync(
            created.FulfillmentId,
            shipmentId,
            actor,
            CancellationToken.None);
        Assert.Equal(FulfillmentStatus.Delivered, dispatched.Status);
        Assert.Equal(ShipmentStatus.Dispatched, dispatched.Shipments[0].Status);
        Assert.NotNull(dispatched.Shipments[0].DispatchedAt);
        Assert.Contains(reservation, inventory.ConsumedReservations);

        var delivered = await directory.DeliverShipmentAsync(
            created.FulfillmentId,
            shipmentId,
            actor,
            CancellationToken.None);
        Assert.Equal(FulfillmentStatus.Delivered, delivered.Status);
        Assert.Equal(ShipmentStatus.Delivered, delivered.Shipments[0].Status);
        Assert.NotNull(delivered.Shipments[0].DeliveredAt);

        var checkoutList = await directory.ListForCheckoutAsync(checkout.CheckoutId, CancellationToken.None);
        Assert.Single(checkoutList);
        Assert.Equal(created.FulfillmentId, checkoutList[0].FulfillmentId);
    }

    /// <summary>
    /// چند محموله برای یک fulfillment و seller isolation در لیست.
    /// </summary>
    [SkippableFact]
    public async Task Multiple_shipments_and_seller_scoped_listing_work_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-multi", "tenant-multi"));

        await using var orderDb = CreateOrderDb(cs, commerce);
        await using var fulfillmentDb = CreateFulfillmentDb(cs, commerce);
        await orderDb.Database.MigrateAsync();
        await fulfillmentDb.Database.MigrateAsync();

        var buyer = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var sellerA = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var sellerB = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var now = DateTimeOffset.Parse("2026-08-27T01:00:00Z");

        var checkoutId = Guid.NewGuid();
        var orderAId = Guid.NewGuid();
        var orderBId = Guid.NewGuid();
        var lineA = OrderLine.FromCheckout(
            orderAId, Guid.NewGuid(), Guid.NewGuid(), sellerA, 2, 50000m, "IRR", true, Guid.NewGuid(), null,
            "Taxable", 0.09m, 9000m, 59000m, null);
        var lineB = OrderLine.FromCheckout(
            orderBId, Guid.NewGuid(), Guid.NewGuid(), sellerB, 1, 40000m, "IRR", true, Guid.NewGuid(), null,
            "Taxable", 0.09m, 3600m, 43600m, null);
        var soA = SellerOrder.Open(checkoutId, sellerA, $"SO-{orderAId:N}"[..20], OrderMode.OnlinePurchase, "IRR", [lineA]);
        var soB = SellerOrder.Open(checkoutId, sellerB, $"SO-{orderBId:N}"[..20], OrderMode.OnlinePurchase, "IRR", [lineB]);
        var group = CheckoutGroup.Submit(
            checkoutId, $"idem-{checkoutId:N}", Guid.NewGuid(), OrderMode.OnlinePurchase, buyer, actor,
            "IR", "IRR", SalesChannel.Marketplace, [soA, soB], now);
        orderDb.Checkouts.Add(group);
        foreach (var sellerOrder in group.SellerOrders)
        {
            sellerOrder.RecordVerifiedPayment();
        }

        await orderDb.SaveChangesAsync();

        var bridge = new OrderFulfillmentBridge(orderDb);
        var directory = new FulfillmentDirectory(
            fulfillmentDb,
            new OpenFulfillmentUseCaseGuard(),
            bridge,
            new RecordingInventoryGateway(),
            new FulfillmentInstrumentation());

        await directory.CreateFromPaidSellerOrdersAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [soA.SellerOrderId, soB.SellerOrderId],
            CancellationToken.None);

        var sellerAList = await directory.ListForSellerAsync(sellerA, CancellationToken.None);
        var sellerBList = await directory.ListForSellerAsync(sellerB, CancellationToken.None);
        Assert.Single(sellerAList);
        Assert.Single(sellerBList);
        Assert.Equal(soA.SellerOrderId, sellerAList[0].SellerOrderId);
        Assert.Equal(soB.SellerOrderId, sellerBList[0].SellerOrderId);

        var fulfillmentA = sellerAList[0];
        await directory.MarkProcessingAsync(fulfillmentA.FulfillmentId, actor, CancellationToken.None);
        await directory.MarkPackedAsync(fulfillmentA.FulfillmentId, actor, CancellationToken.None);
        var lineId = fulfillmentA.Items[0].OrderLineId;
        var first = await directory.CreateShipmentAsync(
            fulfillmentA.FulfillmentId,
            actor,
            "Carrier A",
            [new ShipmentLineCommand(lineId, 1)],
            CancellationToken.None);
        var second = await directory.CreateShipmentAsync(
            fulfillmentA.FulfillmentId,
            actor,
            "Carrier B",
            [new ShipmentLineCommand(lineId, 1)],
            CancellationToken.None);
        Assert.Equal(2, second.Shipments.Count);
        Assert.Equal(2, second.Items[0].QuantityOrdered);
    }

    private static CheckoutGroup SeedPaidCheckout(
        OrderDbContext db,
        Guid buyer,
        Guid actor,
        Guid seller,
        decimal total,
        Guid? reservationId,
        DateTimeOffset now,
        bool markPaid = true,
        string recipientName = "Test Recipient",
        string postalAddress = "Test Address")
    {
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var exclusive = decimal.Divide(total, 1.09m);
        var line = OrderLine.FromCheckout(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            seller,
            1,
            exclusive,
            "IRR",
            true,
            Guid.NewGuid(),
            reservationId,
            "Taxable",
            0.09m,
            total - exclusive,
            total,
            null);
        var sellerOrder = SellerOrder.Open(
            checkoutId,
            seller,
            $"SO-{sellerOrderId:N}"[..20],
            OrderMode.OnlinePurchase,
            "IRR",
            [line]);
        if (markPaid)
        {
            sellerOrder.RecordVerifiedPayment();
        }

        var group = CheckoutGroup.Submit(
            checkoutId,
            $"idem-{checkoutId:N}",
            Guid.NewGuid(),
            OrderMode.OnlinePurchase,
            buyer,
            actor,
            "IR",
            "IRR",
            SalesChannel.Marketplace,
            [sellerOrder],
            now,
            recipientName,
            "09120000000",
            "Tehran",
            "Tehran",
            postalAddress,
            "1234567890",
            "standard",
            "Standard Shipping");
        db.Checkouts.Add(group);
        return group;
    }

    private static OrderDbContext CreateOrderDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new Order.Infrastructure.OrderOutboxRegistration() };
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

    private sealed class RecordingInventoryGateway : IFulfillmentInventoryGateway
    {
        public List<Guid> ConsumedReservations { get; } = [];

        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            ConsumedReservations.Add(reservationId);
            return Task.CompletedTask;
        }
    }
}
