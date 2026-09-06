using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Application;
using Tooba.Payment.Infrastructure;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Returns.Application;
using Tooba.Returns.Domain;
using Tooba.Returns.Infrastructure;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Settlement.Domain;
using Tooba.Settlement.Infrastructure;
using Tooba.Settlement.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// Live Marketplace DB smoke for TB-P09-T001-R2. Opt-in: TOOBA_R2_LIVE=1.
/// Uses Marketplace settlement *handlers* only (not Accrue/Adjust shortcuts).
/// </summary>
public sealed class R2LiveMarketplaceSmokeTests
{
    private const string MarketCs =
        "Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_marketplace";

    [Fact]
    public async Task R2_live_marketplace_handler_path_on_tooba_marketplace()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("TOOBA_R2_LIVE"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var commerce = new FixedCommerceContext();
        commerce.Assign(new CommerceContext(
            new EditionContext(ToobaEdition.Marketplace, "dev-marketplace-r2"),
            null,
            new ConnectionReference("marketplace"),
            TraceId: "r2-live-mkt"));

        await using var orderDb = CreateOrderDb(MarketCs, commerce);
        await using var paymentDb = CreatePaymentDb(MarketCs, commerce);
        await using var returnsDb = CreateReturnsDb(MarketCs, commerce);
        await using var settlementDb = CreateSettlementDb(MarketCs, commerce);
        await orderDb.Database.MigrateAsync();
        await paymentDb.Database.MigrateAsync();
        await returnsDb.Database.MigrateAsync();
        await settlementDb.Database.MigrateAsync();

        var seller = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var buyer = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var reservation = Guid.NewGuid();
        var gross = 218000m;
        var now = DateTimeOffset.UtcNow;

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
            new InitiatePaymentCommand(checkout.CheckoutId, actor, buyer, $"r2-live-{checkout.CheckoutId:N}", "fake"),
            CancellationToken.None);
        await paymentDirectory.VerifyAsync(
            new VerifyPaymentCommand(initiated.PaymentId, initiated.AttemptId, initiated.ProviderRequestReference, true),
            CancellationToken.None);

        var paymentEventId = Guid.NewGuid();
        await new OrderPaymentSucceededHandler(orderDb, paymentBridge).HandleAsync(
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
                    DeploymentId = "dev-marketplace-r2",
                },
            },
            CancellationToken.None);

        var settlement = CreateSettlementDirectory(settlementDb, orderDb, paymentDb, returnsDb);
        var payHandler = new SettlementPaymentSucceededHandler(settlement);
        var payEvt = new PaymentSucceededIntegrationEvent
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
                DeploymentId = "dev-marketplace-r2",
            },
        };
        await payHandler.HandleAsync(payEvt, CancellationToken.None);
        await payHandler.HandleAsync(payEvt, CancellationToken.None);

        var credit = await settlementDb.SettlementEntries.AsNoTracking()
            .SingleAsync(x => x.SellerOrderId == sellerOrderId && x.EntryType == EntryType.Credit);
        var balanceBefore = (await settlement.GetBalanceAsync(seller, CancellationToken.None))!.AvailableBalance;

        var ret = ReturnRequest.Create(
            sellerOrderId,
            checkout.CheckoutId,
            seller,
            actor,
            $"r2-ret-{sellerOrderId:N}",
            "R2 marketplace return",
            "IRR",
            [(Guid.NewGuid(), 1, gross, reservation)],
            now);
        returnsDb.ReturnRequests.Add(ret);
        await returnsDb.SaveChangesAsync();
        ret = await returnsDb.ReturnRequests.SingleAsync(x => x.SellerOrderId == sellerOrderId);
        ret.Approve(initiated.PaymentId, now, RefundDestination.OriginalPayment);
        ret.MarkRefundProcessing(now);
        ret.MarkRefundSucceeded(now);
        await returnsDb.SaveChangesAsync();

        var refundHandler = new SettlementRefundSucceededHandler(settlement);
        var refundEvt = new RefundSucceededIntegrationEvent
        {
            ReturnRequestId = ret.ReturnRequestId,
            SellerOrderId = sellerOrderId,
            PaymentId = initiated.PaymentId,
            RefundAmount = ret.RefundAmount,
            Currency = ret.Currency,
            Metadata = EventMetadataFactory.ForDomain(RefundSucceededIntegrationEvent.EventTypeName) with
            {
                EventId = Guid.NewGuid(),
                Edition = ToobaEdition.Marketplace,
                DeploymentId = "dev-marketplace-r2",
            },
        };
        await refundHandler.HandleAsync(refundEvt, CancellationToken.None);
        await refundHandler.HandleAsync(refundEvt, CancellationToken.None);
        await refundHandler.HandleAsync(
            new RefundSucceededIntegrationEvent
            {
                ReturnRequestId = ret.ReturnRequestId,
                SellerOrderId = sellerOrderId,
                PaymentId = initiated.PaymentId,
                RefundAmount = ret.RefundAmount,
                Currency = ret.Currency,
                Metadata = EventMetadataFactory.ForDomain(RefundSucceededIntegrationEvent.EventTypeName) with
                {
                    EventId = Guid.NewGuid(),
                    Edition = ToobaEdition.Marketplace,
                    DeploymentId = "dev-marketplace-r2",
                },
            },
            CancellationToken.None);

        var creditAfter = await settlementDb.SettlementEntries.AsNoTracking()
            .SingleAsync(x => x.EntryId == credit.EntryId);
        Assert.Equal(credit.NetAmount, creditAfter.NetAmount);
        var debit = await settlementDb.SettlementEntries.AsNoTracking()
            .SingleAsync(x => x.SellerOrderId == sellerOrderId && x.EntryType == EntryType.Debit);
        var balanceAfter = (await settlement.GetBalanceAsync(seller, CancellationToken.None))!.AvailableBalance;
        Assert.Equal(balanceBefore - debit.NetAmount, balanceAfter);

        var evidenceDir = Path.Combine(FindRepoRoot(), "docs", "evidence", "TB-P09-T001");
        Directory.CreateDirectory(evidenceDir);
        await File.WriteAllTextAsync(
            Path.Combine(evidenceDir, "r2-marketplace-runtime-ids.txt"),
            $"sellerOrderId={sellerOrderId}\ncheckoutId={checkout.CheckoutId}\npaymentId={initiated.PaymentId}\nreturnRequestId={ret.ReturnRequestId}\ncreditEntryId={credit.EntryId} gross={credit.GrossAmount} net={credit.NetAmount}\ndebitEntryId={debit.EntryId} gross={debit.GrossAmount} net={debit.NetAmount}\nbalanceBefore={balanceBefore}\nbalanceAfter={balanceAfter}\n");
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
}
