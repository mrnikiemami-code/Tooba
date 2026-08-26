using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Returns.Application;
using Tooba.Returns.Domain;
using Tooba.Returns.Infrastructure;
using Tooba.Returns.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation مرجوعی: Return != Order != Refund، eligibility، idempotency و چرخهٔ refund.
/// </summary>
[Collection("PostgresSerial")]
public sealed class ReturnFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_returns_a")
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
    /// مرز ماژول و نبود JOIN/SQL مستقیم بین Order و Returns.
    /// </summary>
    [Fact]
    public void Return_is_not_order_and_modules_do_not_reference_each_other_infrastructure()
    {
        Assert.NotEqual(typeof(ReturnRequest), typeof(SellerOrder));
        Assert.NotEqual(typeof(RefundAttempt), typeof(CustomerPayment));
        Assert.Equal("returns", ReturnsDbContext.Schema);
        Assert.Equal("order", OrderDbContext.Schema);

        var root = FindRepoRoot();
        foreach (var project in new[]
                 {
                     Path.Combine(root, "src", "backend", "Modules", "Returns", "Tooba.Returns.Domain"),
                     Path.Combine(root, "src", "backend", "Modules", "Returns", "Tooba.Returns.Application"),
                 })
        {
            var csproj = File.ReadAllText(Directory.GetFiles(project, "*.csproj").Single());
            Assert.DoesNotContain("MassTransit", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authzed", csproj, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain(
            "Tooba.Order.Infrastructure",
            File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Returns", "Tooba.Returns.Infrastructure", "Tooba.Returns.Infrastructure.csproj")),
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Tooba.Returns.Infrastructure",
            File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Tooba.Order.Infrastructure.csproj")),
            StringComparison.Ordinal);
        Assert.Contains(
            "Tooba.Order.Application",
            File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Returns", "Tooba.Returns.Infrastructure", "Tooba.Returns.Infrastructure.csproj")),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// ایجاد مرجوعی، idempotency، eligibility، approve/refund و reject.
    /// </summary>
    [SkippableFact]
    public async Task Return_lifecycle_preserves_boundaries_and_refund_idempotency()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-return", "tenant-return"));

        await using var orderDb = CreateOrderDb(cs, commerce);
        await using var fulfillmentDb = CreateFulfillmentDb(cs, commerce);
        await using var paymentDb = CreatePaymentDb(cs, commerce);
        await using var returnsDb = CreateReturnsDb(cs, commerce);
        await orderDb.Database.MigrateAsync();
        await fulfillmentDb.Database.MigrateAsync();
        await paymentDb.Database.MigrateAsync();
        await returnsDb.Database.MigrateAsync();

        var buyer = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var seller = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var reservation = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var now = DateTimeOffset.Parse("2026-08-27T00:00:00Z");

        var checkout = SeedCheckout(orderDb, buyer, actor, seller, 109000m, reservation, now);
        await orderDb.SaveChangesAsync();
        var sellerOrderId = checkout.SellerOrders.Single().SellerOrderId;
        var lineId = (await orderDb.Lines.AsNoTracking().SingleAsync()).LineId;
        var lineUnitPrice = (await orderDb.Lines.AsNoTracking().SingleAsync()).UnitPriceSnapshot;

        var paymentBridge = new OrderPaymentBridge(orderDb);
        var paymentGateways = new PaymentGatewayRegistry([new FakePaymentGateway()]);
        var paymentDirectory = new PaymentDirectory(
            paymentDb,
            new OpenPaymentUseCaseGuard(),
            paymentBridge,
            paymentGateways);
        var initiated = await paymentDirectory.InitiateAsync(
            new InitiatePaymentCommand(checkout.CheckoutId, actor, buyer, "idem-return-pay", "fake"),
            CancellationToken.None);
        var verify = await paymentDirectory.VerifyAsync(
            new VerifyPaymentCommand(initiated.PaymentId, initiated.AttemptId, initiated.ProviderRequestReference, true),
            CancellationToken.None);
        Assert.True(verify.NewlySucceeded);
        var paymentSnapshot = await paymentDirectory.GetAsync(initiated.PaymentId, actor, buyer, CancellationToken.None);
        var paymentHandler = new OrderPaymentSucceededHandler(orderDb, paymentBridge);
        var allocations = await paymentDb.Allocations.AsNoTracking()
            .Where(x => x.PaymentId == initiated.PaymentId)
            .ToListAsync();
        await paymentHandler.HandleAsync(
            new PaymentSucceededIntegrationEvent
            {
                PaymentId = initiated.PaymentId,
                CheckoutId = checkout.CheckoutId,
                Amount = paymentSnapshot!.Amount,
                Currency = paymentSnapshot.Currency,
                SellerOrderIds = allocations.Select(x => x.SellerOrderId).ToArray(),
                Metadata = EventMetadataFactory.ForDomain(PaymentSucceededIntegrationEvent.EventTypeName) with { EventId = Guid.NewGuid() },
            },
            CancellationToken.None);
        orderDb.ChangeTracker.Clear();

        var orderBridge = new OrderFulfillmentBridge(orderDb);
        var fulfillmentDirectory = new FulfillmentDirectory(
            fulfillmentDb,
            new OpenFulfillmentUseCaseGuard(),
            orderBridge,
            new RecordingInventoryGateway(),
            new FulfillmentInstrumentation());
        await fulfillmentDirectory.CreateFromPaidSellerOrdersAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [sellerOrderId],
            CancellationToken.None);

        var fulfillment = await fulfillmentDirectory.GetBySellerOrderAsync(sellerOrderId, CancellationToken.None);
        Assert.NotNull(fulfillment);
        await fulfillmentDirectory.MarkProcessingAsync(fulfillment!.FulfillmentId, actor, CancellationToken.None);
        await fulfillmentDirectory.MarkPackedAsync(fulfillment.FulfillmentId, actor, CancellationToken.None);
        var withShipment = await fulfillmentDirectory.CreateShipmentAsync(
            fulfillment.FulfillmentId,
            actor,
            "Post Demo",
            [new ShipmentLineCommand(lineId, 1)],
            CancellationToken.None);
        var shipmentId = withShipment.Shipments[0].ShipmentId;
        await fulfillmentDirectory.AssignTrackingAsync(
            fulfillment.FulfillmentId,
            shipmentId,
            actor,
            "TRK-RET-001",
            CancellationToken.None);
        await fulfillmentDirectory.DispatchShipmentAsync(
            fulfillment.FulfillmentId,
            shipmentId,
            actor,
            CancellationToken.None);
        await fulfillmentDirectory.DeliverShipmentAsync(
            fulfillment.FulfillmentId,
            shipmentId,
            actor,
            CancellationToken.None);

        var returnDirectory = new ReturnDirectory(
            returnsDb,
            new OpenReturnUseCaseGuard(),
            new OrderReturnBridge(orderDb),
            new FulfillmentReturnBridge(fulfillmentDb),
            paymentDirectory,
            new FakePaymentRefundGateway(),
            new RecordingReturnInventoryGateway(),
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance);

        var created = await returnDirectory.CreateAsync(
            new CreateReturnCommand(
                sellerOrderId,
                actor,
                "return-idem-001",
                "Damaged item",
                [new ReturnLineCommand(lineId, 1)]),
            CancellationToken.None);
        Assert.Equal(ReturnRequestStatus.Requested, created.Status);
        Assert.Equal(lineUnitPrice, created.RefundAmount);

        var replay = await returnDirectory.CreateAsync(
            new CreateReturnCommand(
                sellerOrderId,
                actor,
                "return-idem-001",
                "Damaged item",
                [new ReturnLineCommand(lineId, 1)]),
            CancellationToken.None);
        Assert.Equal(created.ReturnRequestId, replay.ReturnRequestId);
        Assert.Equal(1, await returnsDb.ReturnRequests.CountAsync());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            returnDirectory.CreateAsync(
                new CreateReturnCommand(
                    sellerOrderId,
                    actor,
                    "return-overflow",
                    null,
                    [new ReturnLineCommand(lineId, 2)]),
                CancellationToken.None));

        var rejected = await returnDirectory.RejectAsync(
            new RejectReturnCommand(created.ReturnRequestId, actor, "Not eligible"),
            CancellationToken.None);
        Assert.Equal(ReturnRequestStatus.Rejected, rejected.Status);

        var second = await returnDirectory.CreateAsync(
            new CreateReturnCommand(
                sellerOrderId,
                actor,
                "return-idem-002",
                "Second try",
                [new ReturnLineCommand(lineId, 1)]),
            CancellationToken.None);
        var approved = await returnDirectory.ApproveAsync(
            new ApproveReturnCommand(second.ReturnRequestId, actor),
            CancellationToken.None);
        Assert.Equal(ReturnRequestStatus.Completed, approved.Status);
        Assert.Single(approved.RefundAttempts);
        Assert.Equal(RefundAttemptStatus.Succeeded, approved.RefundAttempts[0].Status);
        Assert.NotNull(approved.PaymentId);

        var failGateway = new ReturnDirectory(
            returnsDb,
            new OpenReturnUseCaseGuard(),
            new OrderReturnBridge(orderDb),
            new FulfillmentReturnBridge(fulfillmentDb),
            paymentDirectory,
            new FakePaymentRefundGateway(),
            new RecordingReturnInventoryGateway(),
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            failGateway.CreateAsync(
                new CreateReturnCommand(
                    sellerOrderId,
                    actor,
                    "return-idem-003",
                    null,
                    [new ReturnLineCommand(lineId, 1)]),
                CancellationToken.None));
    }

    private static CheckoutGroup SeedCheckout(
        OrderDbContext db,
        Guid buyer,
        Guid actor,
        Guid seller,
        decimal total,
        Guid? reservationId,
        DateTimeOffset now)
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
            now);
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

    private static PaymentDbContext CreatePaymentDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new PaymentOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PaymentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PaymentDbContext.Schema, typeof(PaymentDbContext));
        options.AddInterceptors(interceptor);
        return new PaymentDbContext(options.Options);
    }

    private static ReturnsDbContext CreateReturnsDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new ReturnsOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<ReturnsDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, ReturnsDbContext.Schema, typeof(ReturnsDbContext));
        options.AddInterceptors(interceptor);
        return new ReturnsDbContext(options.Options);
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
        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class RecordingReturnInventoryGateway : IReturnInventoryGateway
    {
        public List<(Guid ReservationId, int Quantity)> Restocked { get; } = [];

        public Task RestockConsumedReservationAsync(Guid reservationId, int quantity, CancellationToken cancellationToken)
        {
            Restocked.Add((reservationId, quantity));
            return Task.CompletedTask;
        }
    }
}
