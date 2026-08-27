using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Notification.Application;
using Tooba.Notification.Domain;
using Tooba.Notification.Infrastructure;
using Tooba.Notification.Infrastructure.Persistence;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Application;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation Notification: schema، idempotency، mark-read و ایزولهٔ فروشنده.
/// </summary>
[Collection("PostgresSerial")]
public sealed class NotificationFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_notification")
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

    /// <summary>مرز schema، قرارداد دایرکتوری و مسیرهای allowlist.</summary>
    [Fact]
    public void Notification_module_boundary_static_checks()
    {
        Assert.Equal("notification", NotificationDbContext.Schema);
        Assert.Equal(NotificationRecipientKind.Customer, (NotificationRecipientKind)1);
        Assert.Equal(NotificationRecipientKind.Seller, (NotificationRecipientKind)2);
        Assert.NotNull(typeof(INotificationDirectory).GetMethod(nameof(INotificationDirectory.CreateIfAbsentAsync)));
        Assert.NotNull(typeof(INotificationDirectory).GetMethod(nameof(INotificationDirectory.MarkAllReadAsync)));
        Assert.Contains(ToobaModuleComposition.Modules, module => module is NotificationModule);
        Assert.Equal("/customer-panel/orders/aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0001", NotificationTargetRoutes.CustomerOrder(Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0001")));
        Assert.Throws<InvalidOperationException>(() => NotificationTargetRoutes.RequireAllowed("javascript:alert(1)"));
        Assert.Throws<InvalidOperationException>(() => NotificationTargetRoutes.RequireAllowed("/admin/secret"));

        var endpoints = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Notifications", "NotificationEndpoints.cs"));
        Assert.Contains("/v1/customer/notifications", endpoints, StringComparison.Ordinal);
        Assert.Contains("/v1/seller/notifications", endpoints, StringComparison.Ordinal);
        Assert.Contains("SellerPanelAccess.RequireAuthorizedAsync", endpoints, StringComparison.Ordinal);
    }

    /// <summary>ایجاد با SourceEventId تکراری، mark-read idempotent و ایزولهٔ فروشنده.</summary>
    [SkippableFact]
    public async Task Idempotent_create_mark_read_and_cross_seller_isolation()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var directory = new NotificationDirectory(db, new NotificationInstrumentation());

        var sellerA = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
        var sellerB = Guid.Parse("01a030d1-40db-7000-b90c-a0705133f0eb");
        var customerActor = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccc0003");
        var sourceEventId = "evt-payment-1:seller:" + sellerA.ToString("D");

        var created = await directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Seller,
                sellerA,
                null,
                NotificationCopy.OrderPaidSeller,
                new { sellerOrderId = Guid.NewGuid() },
                NotificationTargetRoutes.SellerOrder(Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddd0004")),
                sourceEventId,
                "payment.succeeded.v1"),
            CancellationToken.None);
        Assert.NotNull(created);

        var duplicate = await directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Seller,
                sellerA,
                null,
                NotificationCopy.OrderPaidSeller,
                new { sellerOrderId = Guid.NewGuid() },
                NotificationTargetRoutes.SellerOrder(Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddd0004")),
                sourceEventId,
                "payment.succeeded.v1"),
            CancellationToken.None);
        Assert.Null(duplicate);

        var sellerAList = await directory.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Seller, sellerA, null, 0, 20, "fa"),
            CancellationToken.None);
        Assert.Equal(1, sellerAList.TotalCount);
        Assert.Equal(1, sellerAList.UnreadCount);
        Assert.Equal("سفارش جدید پرداخت‌شده", sellerAList.Items[0].Title);

        var sellerBList = await directory.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Seller, sellerB, null, 0, 20, "fa"),
            CancellationToken.None);
        Assert.Equal(0, sellerBList.TotalCount);

        var foreignMark = await directory.MarkReadAsync(
            created!.NotificationId,
            NotificationRecipientKind.Seller,
            sellerB,
            null,
            CancellationToken.None);
        Assert.False(foreignMark);

        var firstRead = await directory.MarkReadAsync(
            created.NotificationId,
            NotificationRecipientKind.Seller,
            sellerA,
            null,
            CancellationToken.None);
        Assert.True(firstRead);
        var secondRead = await directory.MarkReadAsync(
            created.NotificationId,
            NotificationRecipientKind.Seller,
            sellerA,
            null,
            CancellationToken.None);
        Assert.True(secondRead);
        Assert.Equal(
            0,
            await directory.UnreadCountAsync(NotificationRecipientKind.Seller, sellerA, null, CancellationToken.None));

        await directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Customer,
                customerActor,
                customerActor,
                NotificationCopy.PaymentSucceeded,
                new { amount = 1000m, currency = "IRR" },
                NotificationTargetRoutes.CustomerOrder(Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeee0005")),
                "evt-payment-1:customer",
                "payment.succeeded.v1"),
            CancellationToken.None);
        await directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Customer,
                customerActor,
                customerActor,
                NotificationCopy.ShipmentDispatched,
                new { shipmentId = Guid.NewGuid() },
                NotificationTargetRoutes.CustomerOrder(Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeee0005")),
                "evt-ship-1:customer",
                "shipment.dispatched.v1"),
            CancellationToken.None);

        Assert.Equal(
            2,
            await directory.UnreadCountAsync(
                NotificationRecipientKind.Customer,
                customerActor,
                customerActor,
                CancellationToken.None));
        var marked = await directory.MarkAllReadAsync(
            NotificationRecipientKind.Customer,
            customerActor,
            customerActor,
            CancellationToken.None);
        Assert.Equal(2, marked);
        Assert.Equal(
            0,
            await directory.UnreadCountAsync(
                NotificationRecipientKind.Customer,
                customerActor,
                customerActor,
                CancellationToken.None));
        var markedAgain = await directory.MarkAllReadAsync(
            NotificationRecipientKind.Customer,
            customerActor,
            customerActor,
            CancellationToken.None);
        Assert.Equal(0, markedAgain);

        var customerList = await directory.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Customer, customerActor, customerActor, 0, 20, "en"),
            CancellationToken.None);
        Assert.Equal(2, customerList.TotalCount);
        var dismissId = customerList.Items[0].NotificationId;
        Assert.True(await directory.SoftDeleteAsync(
            dismissId,
            NotificationRecipientKind.Customer,
            customerActor,
            customerActor,
            CancellationToken.None));
        Assert.Equal(
            1,
            (await directory.ListAsync(
                new NotificationRecipientQuery(NotificationRecipientKind.Customer, customerActor, customerActor, 0, 20, "en"),
                CancellationToken.None)).TotalCount);
    }

    /// <summary>مصرف‌کننده payment.succeeded از Order reader گیرنده می‌سازد و تکراری را سرکوب می‌کند.</summary>
    [SkippableFact]
    public async Task Payment_succeeded_handler_projects_customer_and_seller_idempotently()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(new CommerceContext(
            new EditionContext(ToobaEdition.Marketplace, "test-notification"),
            null,
            new ConnectionReference("marketplace"),
            TraceId: "trace-notification"));

        await using var orderDb = CreateOrderDb(cs, commerce);
        await using var notificationDb = CreateDb(cs);
        await orderDb.Database.MigrateAsync();
        await notificationDb.Database.MigrateAsync();

        var seller = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var foreignSeller = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var buyer = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var now = DateTimeOffset.Parse("2026-08-27T12:00:00Z");
        var checkout = SeedCheckout(orderDb, buyer, actor, seller, 109000m, now);
        await orderDb.SaveChangesAsync();
        var sellerOrderId = checkout.SellerOrders.Single().SellerOrderId;

        var directory = new NotificationDirectory(notificationDb, new NotificationInstrumentation());
        var projector = new NotificationProjector(directory, new OrderNotificationBridge(orderDb));
        var handler = new NotificationPaymentSucceededHandler(projector);
        var eventId = Guid.Parse("01a030d1-40eb-7000-8000-000000000001");
        var integration = new PaymentSucceededIntegrationEvent
        {
            PaymentId = Guid.NewGuid(),
            CheckoutId = checkout.CheckoutId,
            Amount = 109000m,
            Currency = "IRR",
            SellerOrderIds = [sellerOrderId],
            Metadata = EventMetadataFactory.ForDomain(PaymentSucceededIntegrationEvent.EventTypeName) with { EventId = eventId },
        };

        await handler.HandleAsync(integration, CancellationToken.None);
        await handler.HandleAsync(integration, CancellationToken.None);

        Assert.Equal(2, await notificationDb.Notifications.CountAsync());
        Assert.Equal(
            1,
            await directory.UnreadCountAsync(NotificationRecipientKind.Customer, actor, actor, CancellationToken.None));
        Assert.Equal(
            1,
            await directory.UnreadCountAsync(NotificationRecipientKind.Seller, seller, null, CancellationToken.None));
        Assert.Equal(
            0,
            await directory.UnreadCountAsync(NotificationRecipientKind.Seller, foreignSeller, null, CancellationToken.None));
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

    private static CheckoutGroup SeedCheckout(
        OrderDbContext db,
        Guid buyer,
        Guid actor,
        Guid seller,
        decimal total,
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
            Guid.NewGuid(),
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

    private static NotificationDbContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            connectionString,
            NotificationDbContext.Schema,
            typeof(NotificationDbContext));
        return new NotificationDbContext(options.Options);
    }

    private static string RepoRoot()
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
