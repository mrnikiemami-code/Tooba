using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Notification.Application;
using Tooba.Notification.Domain;
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
using Tooba.Wallet.Application;
using Tooba.Wallet.Domain;
using Tooba.Wallet.Infrastructure;
using Tooba.Wallet.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش TB-P06-T028: بدهکار کیف پول، Verify، بدون PSP، refund-to-wallet و idempotency.
/// </summary>
[Collection("PostgresSerial")]
public sealed class WalletCheckoutRefundTests : IAsyncLifetime
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
                .WithDatabase("tooba_wallet_checkout")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch
        {
            _dockerAvailable = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    /// <summary>قرارداد درگاه wallet و destination typed در Returns.</summary>
    [Fact]
    public void Wallet_gateway_and_refund_destination_contracts_exist()
    {
        Assert.Equal("wallet", WalletPaymentGateway.ProviderCodeValue);
        Assert.True(Enum.IsDefined(RefundDestination.OriginalPayment));
        Assert.True(Enum.IsDefined(RefundDestination.Wallet));
        var domain = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "backend", "Modules", "Wallet", "Tooba.Wallet.Domain", "WalletDomain.cs"));
        Assert.Contains("PostOrderPaymentDebit", domain, StringComparison.Ordinal);
        Assert.Contains("PostRefundCredit", domain, StringComparison.Ordinal);
        Assert.Contains("SpendForOrderPaymentAsync", File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "backend", "Modules", "Wallet", "Tooba.Wallet.Application", "WalletContracts.cs")), StringComparison.Ordinal);
        Assert.Contains("wallet-quote", File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontEndpoints.cs")), StringComparison.Ordinal);
        Assert.Contains("WALLET_MIXED_TENDER", File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontPaymentComposer.cs")), StringComparison.Ordinal);
        Assert.Contains("WalletPaymentSucceeded", File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "backend", "Modules", "Notification", "Tooba.Notification.Application", "NotificationContracts.cs")), StringComparison.Ordinal);
        Assert.Contains("WalletRefundCredited", File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "backend", "Modules", "Notification", "Tooba.Notification.Application", "NotificationContracts.cs")), StringComparison.Ordinal);
        Assert.Contains("refund_destination", File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "backend", "Modules", "Returns", "Tooba.Returns.Infrastructure", "Persistence", "Migrations",
            "20260827200000_AddRefundDestination.cs")), StringComparison.Ordinal);
    }

    /// <summary>بدهکار کامل، duplicate، insufficient، currency، Paid state، بدون sandbox، refund once، partial، foreign deny، seed، notification.</summary>
    [SkippableFact]
    public async Task Wallet_spend_verify_and_refund_are_ledger_safe()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-wallet", "tenant-wallet"));

        await using var walletDb = CreateWalletDb(cs, commerce);
        await using var orderDb = CreateOrderDb(cs, commerce);
        await using var paymentDb = CreatePaymentDb(cs, commerce);
        await walletDb.Database.MigrateAsync();
        await orderDb.Database.MigrateAsync();
        await paymentDb.Database.MigrateAsync();

        var notifications = new RecordingNotifications();
        var wallets = new WalletDirectory(walletDb, notifications);
        var actor = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        var stranger = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
        var buyer = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var seller = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var admin = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");
        var now = DateTimeOffset.Parse("2026-08-27T12:00:00Z");

        await wallets.AdjustWalletForAdminAsync(
            actor, admin, new AdminWalletAdjustmentCommand(500_000m, "Credit", "test top-up", "wallet-test-topup-v1"), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wallets.SpendForOrderPaymentAsync(actor, 600_000m, "IRR", Guid.NewGuid(), $"wallet-order-debit:{Guid.NewGuid():D}", CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wallets.SpendForOrderPaymentAsync(actor, 10_000m, "USD", Guid.NewGuid(), $"wallet-order-debit:{Guid.NewGuid():D}", CancellationToken.None));

        var paymentId = Guid.Parse("01900000-0000-7000-8000-000000000101");
        var spend = await wallets.SpendForOrderPaymentAsync(
            actor, 50_000m, "IRR", paymentId, $"wallet-order-debit:{paymentId:D}", CancellationToken.None);
        Assert.False(spend.IdempotentReplay);
        Assert.Equal(nameof(LedgerEntryType.OrderPaymentDebit), spend.Entry.Type);
        Assert.Equal("payment", spend.Entry.SourceType);
        Assert.Equal(paymentId, spend.Entry.SourceId);

        var replay = await wallets.SpendForOrderPaymentAsync(
            actor, 50_000m, "IRR", paymentId, $"wallet-order-debit:{paymentId:D}", CancellationToken.None);
        Assert.True(replay.IdempotentReplay);
        Assert.Equal(spend.Entry.EntryId, replay.Entry.EntryId);
        Assert.Equal(1, await walletDb.LedgerEntries.CountAsync(x => x.Type == LedgerEntryType.OrderPaymentDebit));

        Assert.Contains(notifications.Commands, c => c.Type == NotificationCopy.WalletPaymentSucceeded);
        var firstPayNote = notifications.Commands.First(c => c.Type == NotificationCopy.WalletPaymentSucceeded);
        var secondCreate = await notifications.CreateIfAbsentAsync(firstPayNote, CancellationToken.None);
        Assert.Null(secondCreate);
        Assert.Equal(1, notifications.Commands.Count(c => c.SourceEventId == firstPayNote.SourceEventId));

        await wallets.AdjustWalletForAdminAsync(
            actor, admin, new AdminWalletAdjustmentCommand(100_000m, "Credit", "concurrent pool", "wallet-test-concurrent-credit"), CancellationToken.None);
        var before = (await wallets.GetOrCreateSummaryForCustomerAsync(actor, CancellationToken.None)).Balance;
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var left = wallets.SpendForOrderPaymentAsync(actor, before, "IRR", p1, $"wallet-order-debit:{p1:D}", CancellationToken.None);
        var right = wallets.SpendForOrderPaymentAsync(actor, before, "IRR", p2, $"wallet-order-debit:{p2:D}", CancellationToken.None);
        var outcomes = await Task.WhenAll(Wrap(left), Wrap(right));
        Assert.Equal(1, outcomes.Count(x => x));
        var afterConcurrent = (await wallets.GetOrCreateSummaryForCustomerAsync(actor, CancellationToken.None)).Balance;
        Assert.True(afterConcurrent >= 0m);
        Assert.True(afterConcurrent <= before);

        await wallets.AdjustWalletForAdminAsync(
            actor, admin, new AdminWalletAdjustmentCommand(200_000m, "Credit", "pay path", "wallet-test-pay-credit"), CancellationToken.None);

        var checkout = SeedCheckout(orderDb, buyer, actor, seller, 80_000m, Guid.NewGuid(), now);
        await orderDb.SaveChangesAsync();
        var paymentBridge = new OrderPaymentBridge(orderDb);
        var actorCtx = new PaymentGatewayActorContext();
        var gateways = new PaymentGatewayRegistry([new FakePaymentGateway(), new WalletPaymentGateway(wallets, actorCtx)]);
        var payments = new PaymentDirectory(paymentDb, new OpenPaymentUseCaseGuard(), paymentBridge, gateways, actorCtx);

        var initiated = await payments.InitiateAsync(
            new InitiatePaymentCommand(checkout.CheckoutId, actor, buyer, "idem-wallet-pay", "wallet"),
            CancellationToken.None);
        Assert.Equal("wallet", initiated.ProviderCode);
        Assert.DoesNotContain("sandbox", initiated.RedirectUrl ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/payment/result", initiated.RedirectUrl ?? string.Empty, StringComparison.Ordinal);

        var verified = await payments.VerifyAsync(
            new VerifyPaymentCommand(initiated.PaymentId, initiated.AttemptId, initiated.ProviderRequestReference, true),
            CancellationToken.None);
        Assert.Equal(PaymentStatus.Succeeded, verified.Status);
        Assert.True(verified.NewlySucceeded);

        var duplicateVerify = await payments.VerifyAsync(
            new VerifyPaymentCommand(initiated.PaymentId, initiated.AttemptId, initiated.ProviderRequestReference, true),
            CancellationToken.None);
        Assert.False(duplicateVerify.NewlySucceeded);

        var snapshot = await payments.GetAsync(initiated.PaymentId, actor, buyer, CancellationToken.None);
        var handler = new OrderPaymentSucceededHandler(orderDb, paymentBridge);
        var allocations = await paymentDb.Allocations.AsNoTracking().Where(x => x.PaymentId == initiated.PaymentId).ToListAsync();
        await handler.HandleAsync(
            new PaymentSucceededIntegrationEvent
            {
                PaymentId = initiated.PaymentId,
                CheckoutId = checkout.CheckoutId,
                Amount = snapshot!.Amount,
                Currency = snapshot.Currency,
                SellerOrderIds = allocations.Select(x => x.SellerOrderId).ToArray(),
                ProviderTransactionReference = $"wallet:{initiated.PaymentId:D}",
                Metadata = EventMetadataFactory.ForDomain(PaymentSucceededIntegrationEvent.EventTypeName) with { EventId = Guid.NewGuid() },
            },
            CancellationToken.None);
        orderDb.ChangeTracker.Clear();
        var paidOrder = await orderDb.SellerOrders.AsNoTracking().SingleAsync(x => x.CheckoutId == checkout.CheckoutId);
        Assert.Equal(SellerOrderStatus.Paid, paidOrder.Status);

        var quote = await wallets.QuoteForPayableAsync(actor, 9_999_999m, "IRR", CancellationToken.None);
        Assert.False(quote.CanPayFullyWithWallet);
        Assert.True(quote.RemainingPayable > 0);

        var returnRequestId = Guid.Parse("01900000-0000-7000-8000-000000000201");
        var credit = await wallets.CreditRefundAsync(
            actor, 30_000m, "IRR", returnRequestId, $"wallet-refund-credit:{returnRequestId:D}", CancellationToken.None);
        Assert.False(credit.IdempotentReplay);
        Assert.Equal(nameof(LedgerEntryType.RefundCredit), credit.Entry.Type);
        var creditReplay = await wallets.CreditRefundAsync(
            actor, 30_000m, "IRR", returnRequestId, $"wallet-refund-credit:{returnRequestId:D}", CancellationToken.None);
        Assert.True(creditReplay.IdempotentReplay);
        Assert.Equal(1, await walletDb.LedgerEntries.CountAsync(x => x.Type == LedgerEntryType.RefundCredit && x.SourceId == returnRequestId));
        Assert.Contains(notifications.Commands, c => c.Type == NotificationCopy.WalletRefundCredited);

        var partialId = Guid.Parse("01900000-0000-7000-8000-000000000202");
        var partial = await wallets.CreditRefundAsync(
            actor, 5_000m, "IRR", partialId, $"wallet-refund-credit:{partialId:D}", CancellationToken.None);
        Assert.Equal(5_000m, partial.Entry.Amount);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wallets.SpendForOrderPaymentAsync(stranger, 10_000m, "IRR", Guid.NewGuid(), $"wallet-order-debit:{Guid.NewGuid():D}", CancellationToken.None));

        Assert.True(WalletDemoIds.AccountId != Guid.Empty);
        Assert.Contains("wallet-seed-admin-credit-v1", File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "backend", "Modules", "Wallet", "Tooba.Wallet.Infrastructure", "WalletDevelopmentSeed.cs")), StringComparison.Ordinal);
        Assert.Contains("wallet-refund-credit:", File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "backend", "Modules", "Returns", "Tooba.Returns.Infrastructure", "ReturnDirectory.cs")), StringComparison.Ordinal);
    }

    private static async Task<bool> Wrap(Task task)
    {
        try
        {
            await task;
            return true;
        }
        catch
        {
            return false;
        }
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

    private static WalletDbContext CreateWalletDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new WalletOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<WalletDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, WalletDbContext.Schema, typeof(WalletDbContext));
        options.AddInterceptors(interceptor);
        return new WalletDbContext(options.Options);
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

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class RecordingNotifications : INotificationDirectory
    {
        public List<CreateNotificationCommand> Commands { get; } = [];
        private readonly ConcurrentDictionary<string, byte> _seen = new(StringComparer.Ordinal);

        public Task<UserNotification?> CreateIfAbsentAsync(CreateNotificationCommand command, CancellationToken cancellationToken)
        {
            if (!_seen.TryAdd(command.SourceEventId, 1))
                return Task.FromResult<UserNotification?>(null);
            Commands.Add(command);
            return Task.FromResult<UserNotification?>(null);
        }

        public Task<NotificationListPage> ListAsync(NotificationRecipientQuery query, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<long> UnreadCountAsync(NotificationRecipientKind recipientKind, Guid recipientPartyId, Guid? recipientActorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<bool> MarkReadAsync(Guid notificationId, NotificationRecipientKind recipientKind, Guid recipientPartyId, Guid? recipientActorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<int> MarkAllReadAsync(NotificationRecipientKind recipientKind, Guid recipientPartyId, Guid? recipientActorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<bool> SoftDeleteAsync(Guid notificationId, NotificationRecipientKind recipientKind, Guid recipientPartyId, Guid? recipientActorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
