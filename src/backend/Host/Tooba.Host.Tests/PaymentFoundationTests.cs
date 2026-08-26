using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Host;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation پرداخت: سفارش با پرداخت یکی نیست، مبلغ از کلاینت نیست، و متن callback حقیقت Verify نیست.
/// </summary>
[Collection("PostgresSerial")]
public sealed class PaymentFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_payment_a")
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
    /// مرز ماژول، نبود مبلغ در فرمان کلاینت، و نبود دادهٔ کارت.
    /// </summary>
    [Fact]
    public void Payment_is_not_order_and_client_cannot_choose_amount_or_store_cards()
    {
        Assert.NotEqual(typeof(CustomerPayment), typeof(CheckoutGroup));
        Assert.DoesNotContain("Amount", typeof(InitiatePaymentCommand).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("PAN", File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Payment", "Tooba.Payment.Domain", "PaymentDomain.cs")), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CVV", File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Payment", "Tooba.Payment.Domain", "PaymentDomain.cs")), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("payment", PaymentDbContext.Schema);

        var root = FindRepoRoot();
        foreach (var project in new[]
                 {
                     Path.Combine(root, "src", "backend", "Modules", "Payment", "Tooba.Payment.Domain"),
                     Path.Combine(root, "src", "backend", "Modules", "Payment", "Tooba.Payment.Application"),
                 })
        {
            var csproj = File.ReadAllText(Directory.GetFiles(project, "*.csproj").Single());
            Assert.DoesNotContain("MassTransit", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authzed", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Stripe", csproj, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain(
            "Tooba.Order.Infrastructure",
            File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Payment", "Tooba.Payment.Infrastructure", "Tooba.Payment.Infrastructure.csproj")),
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Tooba.Payment.Infrastructure",
            File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Tooba.Order.Infrastructure.csproj")),
            StringComparison.Ordinal);
        Assert.Contains(
            "Tooba.Payment.Application",
            File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Tooba.Order.Infrastructure.csproj")),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// شروع، Verify، callback دروغین، idempotency، تخصیص چندفروشنده، رزرو بدون پرداخت، و تصویر Paid سفارش.
    /// </summary>
    [SkippableFact]
    public async Task Payment_verifies_provider_evidence_and_projects_order_without_trusting_callback_text()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var csA = _container.GetConnectionString();
        await using (var admin = new Npgsql.NpgsqlConnection(csA))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'tooba_payment_b'";
            if (await cmd.ExecuteScalarAsync() is null)
            {
                await using var create = admin.CreateCommand();
                create.CommandText = "CREATE DATABASE tooba_payment_b";
                await create.ExecuteNonQueryAsync();
            }
        }

        var csB = new Npgsql.NpgsqlConnectionStringBuilder(csA) { Database = "tooba_payment_b" }.ConnectionString;
        var commerceA = new FixedCommerceContext();
        commerceA.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
        var commerceB = new FixedCommerceContext();
        commerceB.Assign(OutboxTestContextFactory.SingleStore("tenant-pay-b", "tenant-pay-b"));

        await using var paymentA = CreatePaymentDb(csA, commerceA);
        await using var orderA = CreateOrderDb(csA, commerceA);
        await using var paymentB = CreatePaymentDb(csB, commerceB);
        await paymentA.Database.MigrateAsync();
        await orderA.Database.MigrateAsync();
        await paymentB.Database.MigrateAsync();

        var actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var buyer = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var seller1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var seller2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var now = DateTimeOffset.Parse("2026-06-01T00:00:00Z");

        var online = SeedCheckout(orderA, OrderMode.OnlinePurchase, buyer, actor, seller1, seller2, 109000m, 98100m, now);
        await orderA.SaveChangesAsync();
        var reserve = SeedCheckout(orderA, OrderMode.RequestToReserve, buyer, actor, seller1, seller2, 109000m, 98100m, now);
        await orderA.SaveChangesAsync();

        var bridge = new OrderPaymentBridge(orderA);
        var gateways = new PaymentGatewayRegistry([new FakePaymentGateway(), new FakeFailingPaymentGateway()]);
        var directory = new PaymentDirectory(paymentA, new OpenPaymentUseCaseGuard(), bridge, gateways);

        var reserveEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.InitiateAsync(new InitiatePaymentCommand(reserve.CheckoutId, actor, buyer, "idem-reserve", "fake"), CancellationToken.None));
        Assert.Contains("رزرو", reserveEx.Message, StringComparison.Ordinal);

        var initiated = await directory.InitiateAsync(
            new InitiatePaymentCommand(online.CheckoutId, actor, buyer, "idem-online", "fake"),
            CancellationToken.None);
        Assert.Equal(PaymentStatus.Pending, initiated.Status);
        Assert.NotEqual(PaymentStatus.Succeeded, initiated.Status);
        Assert.Equal(207100m, initiated.Amount);
        Assert.Equal("IRR", initiated.Currency);
        Assert.Contains("/payment/sandbox", initiated.RedirectUrl ?? string.Empty, StringComparison.Ordinal);

        var replay = await directory.InitiateAsync(
            new InitiatePaymentCommand(online.CheckoutId, actor, buyer, "idem-online", "fake"),
            CancellationToken.None);
        Assert.Equal(initiated.PaymentId, replay.PaymentId);
        Assert.Equal(initiated.RedirectUrl, replay.RedirectUrl);

        var snapshot = await directory.GetAsync(initiated.PaymentId, actor, buyer, CancellationToken.None);
        Assert.Equal(2, snapshot!.Allocations.Count);
        Assert.Equal(snapshot.Amount, snapshot.Allocations.Sum(x => x.AllocatedAmount));

        var beforePay = await orderA.SellerOrders.AsNoTracking().Where(x => x.CheckoutId == online.CheckoutId).ToListAsync();
        Assert.All(beforePay, x => Assert.Equal(SellerOrderStatus.PendingPayment, x.Status));

        var callbackLie = await directory.VerifyAsync(
            new VerifyPaymentCommand(initiated.PaymentId, initiated.AttemptId, initiated.ProviderRequestReference, true),
            CancellationToken.None);
        Assert.Equal(PaymentStatus.Succeeded, callbackLie.Status);
        Assert.True(callbackLie.NewlySucceeded);

        var duplicate = await directory.VerifyAsync(
            new VerifyPaymentCommand(initiated.PaymentId, initiated.AttemptId, initiated.ProviderRequestReference, true),
            CancellationToken.None);
        Assert.False(duplicate.NewlySucceeded);

        var afterVerify = await orderA.SellerOrders.AsNoTracking().Where(x => x.CheckoutId == online.CheckoutId).ToListAsync();
        Assert.All(afterVerify, x => Assert.Equal(SellerOrderStatus.PendingPayment, x.Status));

        await DispatchPaymentOutboxAsync(csA, CancellationToken.None);
        await using var orderReload = CreateOrderDb(csA, commerceA);
        var paid = await orderReload.SellerOrders.AsNoTracking().Where(x => x.CheckoutId == online.CheckoutId).ToListAsync();
        Assert.All(paid, x => Assert.Equal(SellerOrderStatus.Paid, x.Status));
        Assert.Equal(1, await orderReload.PaymentInbox.CountAsync());

        foreach (var row in paymentA.OutboxMessages.Where(x => x.EventType == PaymentSucceededIntegrationEvent.EventTypeName))
        {
            row.ProcessedAt = null;
            row.LockedUntil = null;
            row.NextAttemptAt = null;
        }

        await paymentA.SaveChangesAsync();
        await DispatchPaymentOutboxAsync(csA, CancellationToken.None);
        Assert.Equal(1, await orderReload.PaymentInbox.AsNoTracking().CountAsync());
        var stillPaid = await orderReload.SellerOrders.AsNoTracking().Where(x => x.CheckoutId == online.CheckoutId).ToListAsync();
        Assert.All(stillPaid, x => Assert.Equal(SellerOrderStatus.Paid, x.Status));

        var failCheckout = SeedCheckout(orderA, OrderMode.OnlinePurchase, buyer, actor, seller1, seller2, 50000m, 40000m, now);
        await orderA.SaveChangesAsync();
        var failedInit = await directory.InitiateAsync(
            new InitiatePaymentCommand(failCheckout.CheckoutId, actor, buyer, "idem-fail", "fake-fail"),
            CancellationToken.None);
        var failed = await directory.VerifyAsync(
            new VerifyPaymentCommand(failedInit.PaymentId, failedInit.AttemptId, failedInit.ProviderRequestReference, true),
            CancellationToken.None);
        Assert.Equal(PaymentStatus.Failed, failed.Status);
        Assert.DoesNotContain(
            await paymentA.OutboxMessages.AsNoTracking().ToListAsync(),
            row => row.EventType.Contains("succeeded", StringComparison.OrdinalIgnoreCase)
                && row.Payload.Contains(failedInit.PaymentId.ToString(), StringComparison.OrdinalIgnoreCase));

        var isolated = new PaymentDirectory(paymentB, new OpenPaymentUseCaseGuard(), bridge, gateways);
        Assert.Null(await isolated.GetAsync(initiated.PaymentId, actor, buyer, CancellationToken.None));

        var mismatch = new StubPayableReader
        {
            Snapshot = new PayableCheckoutSnapshot(
                Guid.NewGuid(),
                OrderPaymentMode.OnlinePurchase,
                "IRR",
                [new PayableSellerOrderSnapshot(Guid.NewGuid(), 10m, "USD")]),
        };
        var mismatchDir = new PaymentDirectory(paymentA, new OpenPaymentUseCaseGuard(), mismatch, gateways);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mismatchDir.InitiateAsync(new InitiatePaymentCommand(mismatch.Snapshot.CheckoutId, actor, buyer, "idem-fx", "fake"), CancellationToken.None));

        var amountLie = SeedCheckout(orderA, OrderMode.OnlinePurchase, buyer, actor, seller1, seller2, 30000m, 20000m, now);
        await orderA.SaveChangesAsync();
        var lieInit = await directory.InitiateAsync(
            new InitiatePaymentCommand(amountLie.CheckoutId, actor, buyer, "idem-amount-lie", "fake"),
            CancellationToken.None);
        var lieVerify = await directory.VerifyAsync(
            new VerifyPaymentCommand(lieInit.PaymentId, lieInit.AttemptId, lieInit.ProviderRequestReference, true),
            CancellationToken.None);
        Assert.True(lieVerify.NewlySucceeded);
        var liePaymentId = lieInit.PaymentId.ToString();
        var succeededRow = (await paymentA.OutboxMessages.AsNoTracking().ToListAsync())
            .Single(x => x.EventType == PaymentSucceededIntegrationEvent.EventTypeName
                && x.Payload.Contains(liePaymentId, StringComparison.Ordinal));
        var trackedRow = await paymentA.OutboxMessages.SingleAsync(x => x.Id == succeededRow.Id);
        var payload = JsonNode.Parse(trackedRow.Payload)!;
        payload["amount"] = 1;
        trackedRow.Payload = payload.ToJsonString();
        await paymentA.SaveChangesAsync();
        await DispatchPaymentOutboxAsync(csA, CancellationToken.None);
        var notPaid = await orderA.SellerOrders.AsNoTracking().Where(x => x.CheckoutId == amountLie.CheckoutId).ToListAsync();
        Assert.All(notPaid, x => Assert.Equal(SellerOrderStatus.PendingPayment, x.Status));
        Assert.DoesNotContain(await orderA.PaymentInbox.AsNoTracking().ToListAsync(), x => x.PaymentId == lieInit.PaymentId);
    }

    private static CheckoutGroup SeedCheckout(
        OrderDbContext db,
        OrderMode mode,
        Guid buyer,
        Guid actor,
        Guid sellerA,
        Guid sellerB,
        decimal totalA,
        decimal totalB,
        DateTimeOffset now)
    {
        var checkoutId = Guid.NewGuid();
        var orderAId = Guid.NewGuid();
        var orderBId = Guid.NewGuid();
        var exclusiveA = decimal.Divide(totalA, 1.09m);
        var exclusiveB = decimal.Divide(totalB, 1.09m);
        var lineA = OrderLine.FromCheckout(orderAId, Guid.NewGuid(), Guid.NewGuid(), sellerA, 1, exclusiveA, "IRR", true, Guid.NewGuid(), null, "Taxable", 0.09m, totalA - exclusiveA, totalA, null);
        var lineB = OrderLine.FromCheckout(orderBId, Guid.NewGuid(), Guid.NewGuid(), sellerB, 1, exclusiveB, "IRR", true, Guid.NewGuid(), null, "Taxable", 0.09m, totalB - exclusiveB, totalB, null);
        var soA = SellerOrder.Open(checkoutId, sellerA, $"SO-{orderAId:N}"[..20], mode, "IRR", [lineA]);
        var soB = SellerOrder.Open(checkoutId, sellerB, $"SO-{orderBId:N}"[..20], mode, "IRR", [lineB]);
        var group = CheckoutGroup.Submit(checkoutId, $"idem-{checkoutId:N}", Guid.NewGuid(), mode, buyer, actor, "IR", "IRR", SalesChannel.Marketplace, [soA, soB], now);
        db.Checkouts.Add(group);
        return group;
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

    private static async Task DispatchPaymentOutboxAsync(string connectionString, CancellationToken cancellationToken)
    {
        var platform = OutboxTestPlatform.TwoTenants(connectionString, connectionString);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<ToobaPlatformOptions>>(Options.Create(platform));
        services.AddSingleton(PlatformOptionsValidator.BuildRegistry(platform));
        services.AddSingleton<IDatabaseConnectionResolver, DatabaseConnectionResolver>();
        services.AddSingleton<IOutboxModuleRegistration, PaymentOutboxRegistration>();
        services.AddSingleton<IIntegrationEventSerializer, JsonIntegrationEventSerializer>();
        services.AddSingleton<IOutboxDispatcherStore, NpgsqlOutboxDispatcherStore>();
        services.AddSingleton<IOutboxPollTargetSource, ConfiguredOutboxPollTargetSource>();
        services.AddSingleton<WorkerCommerceContextFactory>();
        services.AddSingleton<IOptions<OutboxHostOptions>>(Options.Create(new OutboxHostOptions
        {
            Enabled = true,
            BatchSize = 20,
            MaxAttempts = 5,
            RetryBaseDelaySeconds = 1,
            LockSeconds = 30,
            PollIntervalSeconds = 60,
        }));
        services.AddSingleton<BackgroundWorkerRegistry>();
        services.AddSingleton<OutboxDispatcher>();
        services.AddScoped<HttpCommerceContextAccessor>();
        services.AddScoped<ICurrentCommerceContext>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddScoped<ICurrentEdition>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddScoped<ICommerceContextAssigner>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddHttpContextAccessor();
        services.AddScoped<IIntegrationEventPublisher, InProcessIntegrationEventPublisher>();
        services.AddScoped<IPayableCheckoutReader, OrderPaymentBridge>();
        services.AddScoped<IOrderPaymentProjection, OrderPaymentBridge>();
        services.AddScoped<IIntegrationEventHandler<PaymentSucceededIntegrationEvent>, OrderPaymentSucceededHandler>();
        services.AddDbContext<OrderDbContext>((sp, options) =>
        {
            var cs = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, cs, OrderDbContext.Schema, typeof(OrderDbContext));
        });
        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<OutboxDispatcher>().DispatchOnceAsync(cancellationToken);
    }

    private sealed class StubPayableReader : IPayableCheckoutReader
    {
        public PayableCheckoutSnapshot? Snapshot { get; set; }

        public Task<PayableCheckoutSnapshot?> GetPayableAsync(Guid checkoutId, Guid actorUserId, Guid? buyerPartyId, CancellationToken cancellationToken) =>
            Task.FromResult(Snapshot is not null && Snapshot.CheckoutId == checkoutId ? Snapshot : null);
    }

    private sealed class NoopProjection : IOrderPaymentProjection
    {
        public Task ApplyVerifiedSuccessAsync(Guid checkoutId, Guid paymentId, IReadOnlyList<Guid> sellerOrderIds, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
