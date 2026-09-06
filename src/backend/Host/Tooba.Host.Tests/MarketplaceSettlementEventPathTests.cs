using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
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
using Tooba.Settlement.Application;
using Tooba.Settlement.Domain;
using Tooba.Settlement.Infrastructure;
using Tooba.Settlement.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P09-T001-R2: Marketplace event-consumer path for accrual/refund adjustment (no direct SettlementDirectory shortcut).
/// </summary>
[Collection("PostgresSerial")]
public sealed class MarketplaceSettlementEventPathTests : IAsyncLifetime
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
                .WithDatabase("tooba_mkt_settlement")
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
            await _disposeAsyncSafe();
        }
    }

    private async Task _disposeAsyncSafe()
    {
        await _container!.DisposeAsync();
    }

    [Fact]
    public void Marketplace_registers_settlement_payment_and_refund_handlers_singlestore_does_not()
    {
        var market = new ServiceCollection();
        new SettlementModule().AddServices(
            market,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tooba:Edition"] = "Marketplace",
            }).Build(),
            new HostEnvironmentStub(Environments.Development));

        var single = new ServiceCollection();
        new SettlementModule().AddServices(
            single,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tooba:Edition"] = "SingleStore",
            }).Build(),
            new HostEnvironmentStub(Environments.Development));

        Assert.Contains(market, d => d.ServiceType == typeof(IIntegrationEventHandler<PaymentSucceededIntegrationEvent>));
        Assert.Contains(market, d => d.ServiceType == typeof(IIntegrationEventHandler<RefundSucceededIntegrationEvent>));
        Assert.DoesNotContain(single, d => d.ServiceType == typeof(IIntegrationEventHandler<PaymentSucceededIntegrationEvent>));
        Assert.DoesNotContain(single, d => d.ServiceType == typeof(IIntegrationEventHandler<RefundSucceededIntegrationEvent>));
        Assert.Equal("payment.succeeded.v1", PaymentSucceededIntegrationEvent.EventTypeName);
        Assert.Equal("refund.succeeded.v1", RefundSucceededIntegrationEvent.EventTypeName);
    }

    [SkippableFact]
    public async Task Marketplace_handlers_accrue_then_adjust_idempotently_without_direct_settlement_api()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(new CommerceContext(
            new EditionContext(ToobaEdition.Marketplace, "test-mkt-settlement-r2"),
            null,
            new ConnectionReference("marketplace"),
            TraceId: "trace-mkt-r2"));

        await using var orderDb = CreateOrderDb(cs, commerce);
        await using var paymentDb = CreatePaymentDb(cs, commerce);
        await using var returnsDb = CreateReturnsDb(cs, commerce);
        await using var settlementDb = CreateSettlementDb(cs, commerce);
        await orderDb.Database.MigrateAsync();
        await paymentDb.Database.MigrateAsync();
        await returnsDb.Database.MigrateAsync();
        await settlementDb.Database.MigrateAsync();

        var seller = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var buyer = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var reservation = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var gross = 109000m;
        var now = DateTimeOffset.Parse("2026-09-06T00:00:00Z");

        var checkout = SeedCheckout(orderDb, buyer, actor, seller, gross, reservation, now);
        await orderDb.SaveChangesAsync();
        var sellerOrderId = checkout.SellerOrders.Single().SellerOrderId;

        var paymentBridge = new OrderPaymentBridge(orderDb);
        var paymentDirectory = new PaymentDirectory(
            paymentDb,
            new OpenPaymentUseCaseGuard(),
            paymentBridge,
            new PaymentGatewayRegistry([new FakePaymentGateway()]),
            new PaymentGatewayActorContext());
        var initiated = await paymentDirectory.InitiateAsync(
            new InitiatePaymentCommand(checkout.CheckoutId, actor, buyer, "idem-mkt-r2-pay", "fake"),
            CancellationToken.None);
        var verify = await paymentDirectory.VerifyAsync(
            new VerifyPaymentCommand(initiated.PaymentId, initiated.AttemptId, initiated.ProviderRequestReference, true),
            CancellationToken.None);
        Assert.True(verify.NewlySucceeded);

        var paymentHandler = new OrderPaymentSucceededHandler(orderDb, paymentBridge);
        var paymentEventId = Guid.NewGuid();
        var allocations = await paymentDb.Allocations.AsNoTracking()
            .Where(x => x.PaymentId == initiated.PaymentId)
            .ToListAsync();
        await paymentHandler.HandleAsync(
            new PaymentSucceededIntegrationEvent
            {
                PaymentId = initiated.PaymentId,
                CheckoutId = checkout.CheckoutId,
                Amount = gross,
                Currency = "IRR",
                SellerOrderIds = allocations.Select(x => x.SellerOrderId).ToArray(),
                Metadata = EventMetadataFactory.ForDomain(PaymentSucceededIntegrationEvent.EventTypeName) with
                {
                    EventId = paymentEventId,
                    Edition = ToobaEdition.Marketplace,
                    DeploymentId = "test-mkt-settlement-r2",
                },
            },
            CancellationToken.None);

        var settlementDirectory = CreateSettlementDirectory(settlementDb, orderDb, paymentDb, returnsDb);
        // Marketplace consumer path — not SettlementDirectory.AccrueFromPaymentAsync
        var paymentConsumer = new SettlementPaymentSucceededHandler(settlementDirectory);
        await paymentConsumer.HandleAsync(
            new PaymentSucceededIntegrationEvent
            {
                PaymentId = initiated.PaymentId,
                CheckoutId = checkout.CheckoutId,
                Amount = gross,
                Currency = "IRR",
                SellerOrderIds = [sellerOrderId],
                Metadata = EventMetadataFactory.ForDomain(PaymentSucceededIntegrationEvent.EventTypeName) with
                {
                    EventId = paymentEventId,
                    Edition = ToobaEdition.Marketplace,
                    DeploymentId = "test-mkt-settlement-r2",
                },
            },
            CancellationToken.None);
        // redelivery
        await paymentConsumer.HandleAsync(
            new PaymentSucceededIntegrationEvent
            {
                PaymentId = initiated.PaymentId,
                CheckoutId = checkout.CheckoutId,
                Amount = gross,
                Currency = "IRR",
                SellerOrderIds = [sellerOrderId],
                Metadata = EventMetadataFactory.ForDomain(PaymentSucceededIntegrationEvent.EventTypeName) with
                {
                    EventId = paymentEventId,
                    Edition = ToobaEdition.Marketplace,
                    DeploymentId = "test-mkt-settlement-r2",
                },
            },
            CancellationToken.None);

        var credit = await settlementDb.SettlementEntries.AsNoTracking()
            .SingleAsync(x => x.EntryType == EntryType.Credit);
        Assert.Equal(sellerOrderId, credit.SellerOrderId);
        var creditSnapshot = new { credit.EntryId, credit.GrossAmount, credit.NetAmount, credit.CommissionAmount, credit.IdempotencyKey };
        var balanceBefore = await settlementDirectory.GetBalanceAsync(seller, CancellationToken.None);
        Assert.Equal(credit.NetAmount, balanceBefore!.AvailableBalance);

        var returnRequest = ReturnRequest.Create(
            sellerOrderId,
            checkout.CheckoutId,
            seller,
            actor,
            "mkt-r2-return",
            "Damaged",
            "IRR",
            [(Guid.NewGuid(), 1, gross, reservation)],
            now);
        returnsDb.ReturnRequests.Add(returnRequest);
        await returnsDb.SaveChangesAsync();
        returnRequest = await returnsDb.ReturnRequests.SingleAsync();
        // Simulate refund completion domain outcome already persisted for event payload
        returnRequest.Approve(initiated.PaymentId, now, RefundDestination.OriginalPayment);
        returnRequest.MarkRefundProcessing(now);
        returnRequest.MarkRefundSucceeded(now);
        await returnsDb.SaveChangesAsync();

        var refundEventId = Guid.NewGuid();
        var refundConsumer = new SettlementRefundSucceededHandler(settlementDirectory);
        var refundEvent = new RefundSucceededIntegrationEvent
        {
            ReturnRequestId = returnRequest.ReturnRequestId,
            SellerOrderId = sellerOrderId,
            PaymentId = initiated.PaymentId,
            RefundAmount = returnRequest.RefundAmount,
            Currency = returnRequest.Currency,
            Metadata = EventMetadataFactory.ForDomain(RefundSucceededIntegrationEvent.EventTypeName) with
            {
                EventId = refundEventId,
                Edition = ToobaEdition.Marketplace,
                DeploymentId = "test-mkt-settlement-r2",
            },
        };
        await refundConsumer.HandleAsync(refundEvent, CancellationToken.None);
        // redelivery / duplicate event id path
        await refundConsumer.HandleAsync(refundEvent, CancellationToken.None);
        // distinct event id but same return → still one debit via idempotency key
        await refundConsumer.HandleAsync(
            new RefundSucceededIntegrationEvent
            {
                ReturnRequestId = returnRequest.ReturnRequestId,
                SellerOrderId = sellerOrderId,
                PaymentId = initiated.PaymentId,
                RefundAmount = returnRequest.RefundAmount,
                Currency = returnRequest.Currency,
                Metadata = EventMetadataFactory.ForDomain(RefundSucceededIntegrationEvent.EventTypeName) with
                {
                    EventId = Guid.NewGuid(),
                    Edition = ToobaEdition.Marketplace,
                    DeploymentId = "test-mkt-settlement-r2",
                },
            },
            CancellationToken.None);

        var credits = await settlementDb.SettlementEntries.AsNoTracking()
            .Where(x => x.EntryType == EntryType.Credit)
            .ToListAsync();
        Assert.Single(credits);
        Assert.Equal(creditSnapshot.EntryId, credits[0].EntryId);
        Assert.Equal(creditSnapshot.GrossAmount, credits[0].GrossAmount);
        Assert.Equal(creditSnapshot.NetAmount, credits[0].NetAmount);
        Assert.Equal(creditSnapshot.CommissionAmount, credits[0].CommissionAmount);
        Assert.Equal(creditSnapshot.IdempotencyKey, credits[0].IdempotencyKey);

        var debits = await settlementDb.SettlementEntries.AsNoTracking()
            .Where(x => x.EntryType == EntryType.Debit)
            .ToListAsync();
        Assert.Single(debits);
        Assert.Equal("refund", debits[0].SourceType);
        Assert.Equal(returnRequest.ReturnRequestId, debits[0].SourceId);

        var balanceAfter = await settlementDirectory.GetBalanceAsync(seller, CancellationToken.None);
        Assert.Equal(credit.NetAmount - debits[0].NetAmount, balanceAfter!.AvailableBalance);
    }

    private static SettlementDirectory CreateSettlementDirectory(
        SettlementDbContext settlementDb,
        OrderDbContext orderDb,
        PaymentDbContext paymentDb,
        ReturnsDbContext returnsDb) =>
        new(
            settlementDb,
            new OpenSettlementUseCaseGuard(),
            new SettlementOrderBridge(new OrderReturnBridge(orderDb)),
            new SettlementPaymentBridge(new PaymentSettlementBridge(paymentDb)),
            new SettlementReturnsBridge(new ReturnSettlementBridge(returnsDb)),
            new FakePayoutGateway(),
            new SettlementInstrumentation());

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
        var modules = new IOutboxModuleRegistration[] { new OrderOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        options.AddInterceptors(interceptor);
        return new OrderDbContext(options.Options);
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

    private static SettlementDbContext CreateSettlementDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new SettlementOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<SettlementDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, SettlementDbContext.Schema, typeof(SettlementDbContext));
        options.AddInterceptors(interceptor);
        return new SettlementDbContext(options.Options);
    }

    private sealed class HostEnvironmentStub : IHostEnvironment
    {
        public HostEnvironmentStub(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Tooba.Host.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
