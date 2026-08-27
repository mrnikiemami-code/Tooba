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
using Tooba.Returns.Domain;
using Tooba.Returns.Application;
using Tooba.Returns.Infrastructure;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Settlement.Application;
using Tooba.Settlement.Domain;
using Tooba.Settlement.Infrastructure;
using Tooba.Settlement.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation تسویه: accrual، commission، refund adjustment، payout safety و مرز ماژول.
/// </summary>
[Collection("PostgresSerial")]
public sealed class SettlementFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_settlement_a")
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
    /// مرز ماژول Settlement و schema اختصاصی.
    /// </summary>
    [Fact]
    public void Settlement_module_boundary_static_checks()
    {
        Assert.Equal("settlement", SettlementDbContext.Schema);
        Assert.NotEqual(typeof(SettlementEntry), typeof(CustomerPayment));

        var root = FindRepoRoot();
        var infraCsproj = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Settlement", "Tooba.Settlement.Infrastructure", "Tooba.Settlement.Infrastructure.csproj"));
        Assert.DoesNotContain("Tooba.Order.Infrastructure", infraCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Infrastructure", infraCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Order.Application", infraCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Payment.Application", infraCsproj, StringComparison.Ordinal);
    }

    /// <summary>
    /// accrual، duplicate inbox، commission ۱۰٪، refund adjustment و payout safety.
    /// </summary>
    [SkippableFact]
    public async Task Settlement_lifecycle_applies_commission_refund_and_payout_safety()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(new CommerceContext(
            new EditionContext(ToobaEdition.Marketplace, "test-settlement"),
            null,
            new ConnectionReference("marketplace"),
            TraceId: "trace-settlement"));

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
        var now = DateTimeOffset.Parse("2026-08-27T00:00:00Z");

        var checkout = SeedCheckout(orderDb, buyer, actor, seller, gross, reservation, now);
        await orderDb.SaveChangesAsync();
        var sellerOrderId = checkout.SellerOrders.Single().SellerOrderId;

        var paymentBridge = new OrderPaymentBridge(orderDb);
        var paymentDirectory = new PaymentDirectory(
            paymentDb,
            new OpenPaymentUseCaseGuard(),
            paymentBridge,
            new PaymentGatewayRegistry([new FakePaymentGateway()]));
        var initiated = await paymentDirectory.InitiateAsync(
            new InitiatePaymentCommand(checkout.CheckoutId, actor, buyer, "idem-settlement-pay", "fake"),
            CancellationToken.None);
        var verify = await paymentDirectory.VerifyAsync(
            new VerifyPaymentCommand(initiated.PaymentId, initiated.AttemptId, initiated.ProviderRequestReference, true),
            CancellationToken.None);
        Assert.True(verify.NewlySucceeded);

        var paymentHandler = new OrderPaymentSucceededHandler(orderDb, paymentBridge);
        var eventId = Guid.NewGuid();
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
                Metadata = EventMetadataFactory.ForDomain(PaymentSucceededIntegrationEvent.EventTypeName) with { EventId = eventId },
            },
            CancellationToken.None);

        var settlementDirectory = CreateSettlementDirectory(settlementDb, orderDb, paymentDb, returnsDb);
        await settlementDirectory.AccrueFromPaymentAsync(
            initiated.PaymentId,
            eventId,
            [sellerOrderId],
            CancellationToken.None);

        var entry = await settlementDb.SettlementEntries.SingleAsync();
        Assert.Equal(EntryType.Credit, entry.EntryType);
        Assert.Equal(0.10m, entry.CommissionPolicySnapshot.Rate);
        Assert.Equal(decimal.Round(gross * 0.10m, 4, MidpointRounding.AwayFromZero), entry.CommissionAmount);
        Assert.Equal(gross - entry.CommissionAmount, entry.NetAmount);

        await settlementDirectory.AccrueFromPaymentAsync(
            initiated.PaymentId,
            eventId,
            [sellerOrderId],
            CancellationToken.None);
        Assert.Equal(1, await settlementDb.SettlementEntries.CountAsync());
        Assert.Equal(1, await settlementDb.PaymentInbox.CountAsync());

        var duplicateEventId = Guid.NewGuid();
        await settlementDirectory.AccrueFromPaymentAsync(
            initiated.PaymentId,
            duplicateEventId,
            [sellerOrderId],
            CancellationToken.None);
        Assert.Equal(1, await settlementDb.SettlementEntries.CountAsync());
        Assert.Equal(2, await settlementDb.PaymentInbox.CountAsync());

        var balance = await settlementDirectory.GetBalanceAsync(seller, CancellationToken.None);
        Assert.NotNull(balance);
        Assert.Equal(entry.NetAmount, balance!.AvailableBalance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            settlementDirectory.RequestPayoutAsync(
                new RequestPayoutCommand(seller, balance.AvailableBalance + 1m, "payout-over", actor),
                CancellationToken.None));

        var payout = await settlementDirectory.RequestPayoutAsync(
            new RequestPayoutCommand(seller, balance.AvailableBalance, "payout-001", actor),
            CancellationToken.None);
        Assert.Equal(PayoutStatus.Pending, payout.Status);

        var processed = await settlementDirectory.ProcessPayoutAsync(
            new ProcessPayoutCommand(payout.PayoutRequestId, actor),
            CancellationToken.None);
        Assert.Equal(PayoutStatus.Succeeded, processed.Status);

        var refundEventId = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            settlementDirectory.AdjustFromRefundAsync(
                Guid.NewGuid(),
                gross,
                "IRR",
                refundEventId,
                CancellationToken.None));

        var returnRequestId = Guid.NewGuid();
        returnsDb.ReturnRequests.Add(ReturnRequest.Create(
            sellerOrderId,
            checkout.CheckoutId,
            seller,
            actor,
            "refund-settlement-idem",
            "Damaged",
            "IRR",
            [(Guid.NewGuid(), 1, gross, reservation)],
            now));
        await returnsDb.SaveChangesAsync();
        var returnRequest = await returnsDb.ReturnRequests.SingleAsync();

        await settlementDirectory.AdjustFromRefundAsync(
            returnRequest.ReturnRequestId,
            returnRequest.RefundAmount,
            returnRequest.Currency,
            refundEventId,
            CancellationToken.None);

        var debit = await settlementDb.SettlementEntries.SingleAsync(x => x.EntryType == EntryType.Debit);
        Assert.Equal(returnRequest.RefundAmount - debit.CommissionAmount, debit.NetAmount);

        await settlementDirectory.AdjustFromRefundAsync(
            returnRequest.ReturnRequestId,
            returnRequest.RefundAmount,
            returnRequest.Currency,
            Guid.NewGuid(),
            CancellationToken.None);
        Assert.Equal(1, await settlementDb.SettlementEntries.CountAsync(x => x.EntryType == EntryType.Debit));
    }

    /// <summary>
    /// handlerهای marketplace فقط وقتی Edition=Marketplace ثبت می‌شوند.
    /// </summary>
    [Fact]
    public void Settlement_handlers_are_marketplace_gated_in_module_registration()
    {
        var marketplaceServices = new ServiceCollection();
        new SettlementModule().AddServices(
            marketplaceServices,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tooba:Edition"] = "Marketplace",
            }).Build(),
            new HostEnvironmentStub(Environments.Development));

        var singleStoreServices = new ServiceCollection();
        new SettlementModule().AddServices(
            singleStoreServices,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tooba:Edition"] = "SingleStore",
            }).Build(),
            new HostEnvironmentStub(Environments.Development));

        Assert.Contains(
            marketplaceServices,
            d => d.ServiceType == typeof(IIntegrationEventHandler<PaymentSucceededIntegrationEvent>));
        Assert.DoesNotContain(
            singleStoreServices,
            d => d.ServiceType == typeof(IIntegrationEventHandler<PaymentSucceededIntegrationEvent>));
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

    private sealed class HostEnvironmentStub : IHostEnvironment
    {
        public HostEnvironmentStub(string environmentName) => EnvironmentName = environmentName;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Tooba.Host.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
