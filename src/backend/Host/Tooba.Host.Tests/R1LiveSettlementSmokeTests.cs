using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Order.Infrastructure;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Infrastructure;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Returns.Infrastructure;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Settlement.Domain;
using Tooba.Settlement.Infrastructure;
using Tooba.Settlement.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// runtime smoke against local tooba_alpha for TB-P09-T001-R1 settlement accrual/adjust.
/// Opt-in: TOOBA_R1_LIVE=1.
/// </summary>
public sealed class R1LiveSettlementSmokeTests
{
    private const string AlphaCs =
        "Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha";

    /// <summary>
    /// Accrue credit for delivered Paid order; used before Host return + AdjustFromRefund.
    /// </summary>
    [Fact]
    public async Task R1_live_accrue_from_payment()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("TOOBA_R1_LIVE"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var paymentId = Guid.Parse("88ddad77-9a7f-4e51-8ed9-12e45ecbf791");
        var sellerOrderId = Guid.Parse("01a0451c-fac1-7000-9371-f9d9ebb05855");
        var eventId = Guid.Parse("b82ae2b9-94a7-4756-bcfa-edb14c4f09b5");

        var commerce = new FixedCommerceContext();
        commerce.Assign(new CommerceContext(
            new EditionContext(ToobaEdition.SingleStore, "store-alpha"),
            null,
            new ConnectionReference("tenant-alpha"),
            TraceId: "r1-live-accrue"));

        await using var orderDb = CreateOrderDb(AlphaCs, commerce);
        await using var paymentDb = CreatePaymentDb(AlphaCs, commerce);
        await using var returnsDb = CreateReturnsDb(AlphaCs, commerce);
        await using var settlementDb = CreateSettlementDb(AlphaCs, commerce);
        var settlement = CreateSettlementDirectory(settlementDb, orderDb, paymentDb, returnsDb);

        await settlement.AccrueFromPaymentAsync(paymentId, eventId, [sellerOrderId], CancellationToken.None);

        var credits = await settlementDb.SettlementEntries.AsNoTracking()
            .Where(x => x.SellerOrderId == sellerOrderId && x.EntryType == EntryType.Credit)
            .ToListAsync();
        Assert.Single(credits);
        Assert.Equal(paymentId, credits[0].SourceId);
        Console.WriteLine($"R1_ACCRUE entryId={credits[0].EntryId} net={credits[0].NetAmount} gross={credits[0].GrossAmount} idem={credits[0].IdempotencyKey}");
    }

    /// <summary>
    /// After Host approve return: post AdjustFromRefund twice and assert one debit + credit immutable.
    /// </summary>
    [Fact]
    public async Task R1_live_adjust_from_refund_idempotent()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("TOOBA_R1_LIVE"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var returnRequestIdRaw = Environment.GetEnvironmentVariable("TOOBA_R1_RETURN_ID");
        Assert.False(string.IsNullOrWhiteSpace(returnRequestIdRaw), "TOOBA_R1_RETURN_ID required");
        var returnRequestId = Guid.Parse(returnRequestIdRaw!);
        var sellerOrderId = Guid.Parse("01a0451c-fac1-7000-9371-f9d9ebb05855");
        var sellerPartyId = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");

        var commerce = new FixedCommerceContext();
        commerce.Assign(new CommerceContext(
            new EditionContext(ToobaEdition.SingleStore, "store-alpha"),
            null,
            new ConnectionReference("tenant-alpha"),
            TraceId: "r1-live-adjust"));

        await using var orderDb = CreateOrderDb(AlphaCs, commerce);
        await using var paymentDb = CreatePaymentDb(AlphaCs, commerce);
        await using var returnsDb = CreateReturnsDb(AlphaCs, commerce);
        await using var settlementDb = CreateSettlementDb(AlphaCs, commerce);
        var settlement = CreateSettlementDirectory(settlementDb, orderDb, paymentDb, returnsDb);

        var creditBefore = await settlementDb.SettlementEntries.AsNoTracking()
            .SingleAsync(x => x.SellerOrderId == sellerOrderId && x.EntryType == EntryType.Credit);
        var creditSnapshot = new
        {
            creditBefore.EntryId,
            creditBefore.GrossAmount,
            creditBefore.NetAmount,
            creditBefore.CommissionAmount,
            creditBefore.IdempotencyKey,
        };

        var ret = await returnsDb.ReturnRequests.AsNoTracking()
            .SingleAsync(x => x.ReturnRequestId == returnRequestId);

        await settlement.AdjustFromRefundAsync(
            returnRequestId,
            ret.RefundAmount,
            ret.Currency,
            Guid.NewGuid(),
            CancellationToken.None);
        await settlement.AdjustFromRefundAsync(
            returnRequestId,
            ret.RefundAmount,
            ret.Currency,
            Guid.NewGuid(),
            CancellationToken.None);

        var credits = await settlementDb.SettlementEntries.AsNoTracking()
            .Where(x => x.SellerOrderId == sellerOrderId && x.EntryType == EntryType.Credit)
            .ToListAsync();
        Assert.Single(credits);
        Assert.Equal(creditSnapshot.EntryId, credits[0].EntryId);
        Assert.Equal(creditSnapshot.GrossAmount, credits[0].GrossAmount);
        Assert.Equal(creditSnapshot.NetAmount, credits[0].NetAmount);
        Assert.Equal(creditSnapshot.CommissionAmount, credits[0].CommissionAmount);
        Assert.Equal(creditSnapshot.IdempotencyKey, credits[0].IdempotencyKey);

        var debits = await settlementDb.SettlementEntries.AsNoTracking()
            .Where(x => x.SellerOrderId == sellerOrderId && x.EntryType == EntryType.Debit)
            .ToListAsync();
        Assert.Single(debits);
        Assert.Equal("refund", debits[0].SourceType);
        Assert.Equal(returnRequestId, debits[0].SourceId);

        var balance = await settlement.GetBalanceAsync(sellerPartyId, CancellationToken.None);
        Assert.NotNull(balance);
        Console.WriteLine(
            $"R1_ADJUST creditId={credits[0].EntryId} debitId={debits[0].EntryId} debitNet={debits[0].NetAmount} available={balance!.AvailableBalance}");
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
}
