using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Order.Contracts.Returns;
using Tooba.Payment.Contracts.Returns;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Directories;
using Tooba.Returns.Infrastructure.Evaluators;
using Tooba.Returns.Infrastructure.Observability;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

/// <summary>
/// TB-277-R1: the OWNING UNIT rule at the physical write boundary.
/// <para>
/// TB-277 published the unit precision only as a detail/UI fact, so a direct <c>POST …/receipt</c> could still store
/// half a piece or a ten-thousandth of a gram. These facts prove the owning directory itself refuses such a quantity —
/// with zero inserted rows and zero money/stock calls — while a legitimate fraction of a fractional unit, a
/// trailing-zero whole piece, and an exact replay of an already stored occurrence all stay lawful.
/// </para>
/// </summary>
public sealed class ReturnsReceiptUnitPrecision277R1Tests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_whole_piece_line_refuses_a_fraction_before_any_row_is_written()
    {
        await using var harness = await SeedAsync(requested: 3m, unitDecimalPlaces: 0, orderLineAnswered: true);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.RecordAsync(("delivery-1", harness.ReturnItemId, 0.5m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptUnitQuantityInvalid, refused.Code);

        Assert.Equal(0, await harness.ReceiptCountAsync());
        Assert.Equal(0, await harness.ReceiptLineCountAsync());
        Assert.Empty(harness.Gateway.Calls);
        Assert.Empty(harness.Wallet.Calls);
        Assert.Empty(harness.Inventory.Calls);
    }

    [Fact]
    public async Task A_trailing_zero_whole_piece_is_the_same_quantity()
    {
        await using var harness = await SeedAsync(requested: 2m, unitDecimalPlaces: 0, orderLineAnswered: true);

        var snapshot = await harness.RecordAsync(
            ("delivery-1", harness.ReturnItemId, 1.000000m, ReturnItemCondition.Usable));

        Assert.Equal(1, await harness.ReceiptCountAsync());
        var line = Assert.Single(await harness.ReceiptLinesAsync());
        // The VALUE is one whole piece; the representation (1 vs 1.000000) is not a unit-precision problem.
        Assert.True(line.Quantity == 1m);
        Assert.Equal(1m, snapshot.Receiving!.Received);
    }

    [Fact]
    public async Task A_three_decimal_weight_accepts_a_legitimate_fraction()
    {
        await using var harness = await SeedAsync(requested: 1m, unitDecimalPlaces: 3, orderLineAnswered: true);

        await harness.RecordAsync(("delivery-1", harness.ReturnItemId, 0.125m, ReturnItemCondition.Usable));

        var line = Assert.Single(await harness.ReceiptLinesAsync());
        Assert.Equal(0.125m, line.Quantity);
    }

    [Fact]
    public async Task A_fraction_finer_than_the_weight_unit_is_refused()
    {
        await using var harness = await SeedAsync(requested: 1m, unitDecimalPlaces: 3, orderLineAnswered: true);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.RecordAsync(("delivery-1", harness.ReturnItemId, 0.0001m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptUnitQuantityInvalid, refused.Code);

        Assert.Equal(0, await harness.ReceiptCountAsync());
        Assert.Equal(0, await harness.ReceiptLineCountAsync());
    }

    [Fact]
    public async Task An_unanswered_unit_precision_is_refused_and_never_defaulted_to_six_decimals()
    {
        await using var harness = await SeedAsync(requested: 1m, unitDecimalPlaces: null, orderLineAnswered: true);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.RecordAsync(("delivery-1", harness.ReturnItemId, 0.5m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptUnitQuantityInvalid, refused.Code);

        // Even an integer amount is refused: the unit rule itself is unproven, not just the fraction.
        var alsoRefused = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.RecordAsync(("delivery-2", harness.ReturnItemId, 1m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptUnitQuantityInvalid, alsoRefused.Code);

        Assert.Equal(0, await harness.ReceiptCountAsync());
        Assert.Equal(0, await harness.ReceiptLineCountAsync());
    }

    [Fact]
    public async Task An_order_line_that_does_not_belong_to_this_case_is_refused()
    {
        await using var harness = await SeedAsync(requested: 1m, unitDecimalPlaces: 0, orderLineAnswered: false);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.RecordAsync(("delivery-1", harness.ReturnItemId, 1m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptUnitQuantityInvalid, refused.Code);
        Assert.Equal(0, await harness.ReceiptCountAsync());
    }

    [Fact]
    public async Task Usable_and_damaged_share_one_exact_ceiling_over_a_fractional_unit()
    {
        await using var harness = await SeedAsync(requested: 0.3m, unitDecimalPlaces: 3, orderLineAnswered: true);

        // 0.1 usable + 0.2 damaged is exactly 0.3 — the valid split TB-277's Number addition rejected.
        await harness.RecordAsync(
            ("delivery-1", harness.ReturnItemId, 0.1m, ReturnItemCondition.Usable),
            ("delivery-1", harness.ReturnItemId, 0.2m, ReturnItemCondition.Damaged));

        var summary = (await harness.SnapshotAsync()).Receiving!;
        Assert.True(summary.IsComplete);
        Assert.Equal(0.3m, summary.Received);
        Assert.Equal(0.1m, summary.Usable);
        Assert.Equal(0.2m, summary.Damaged);
        Assert.Equal(0m, summary.Remaining);

        // A second delivery of any positive amount now exceeds the requested quantity.
        var over = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.RecordAsync(("delivery-2", harness.ReturnItemId, 0.2m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptOverReceived, over.Code);
        Assert.Equal(2, await harness.ReceiptLineCountAsync());
    }

    [Fact]
    public async Task A_partial_delivery_publishes_the_exact_remaining_quantity()
    {
        await using var harness = await SeedAsync(requested: 0.3m, unitDecimalPlaces: 3, orderLineAnswered: true);

        var snapshot = await harness.RecordAsync(
            ("delivery-1", harness.ReturnItemId, 0.1m, ReturnItemCondition.Usable));

        var line = Assert.Single(snapshot.Receiving!.Lines);
        Assert.Equal(0.3m, line.Requested);
        Assert.Equal(0.1m, line.Received);
        Assert.Equal(0.2m, line.Requested - line.Received);
        Assert.Equal(0.2m, snapshot.Receiving.Remaining);
    }

    [Fact]
    public async Task An_exact_stored_replay_answers_even_when_the_unit_metadata_later_disappears()
    {
        await using var harness = await SeedAsync(requested: 1m, unitDecimalPlaces: 3, orderLineAnswered: true);

        await harness.RecordAsync(("delivery-1", harness.ReturnItemId, 0.3m, ReturnItemCondition.Usable),
            ("delivery-1", harness.ReturnItemId, 0.7m, ReturnItemCondition.Damaged));

        // The owning order read stops answering anything: a NEW write must refuse, while the already stored exact
        // occurrence is a durable fact that still replays byte-identically.
        harness.OrderReader.StopAnswering();

        var replay = await harness.RecordAsync(
            ("delivery-1", harness.ReturnItemId, 0.3m, ReturnItemCondition.Usable),
            ("delivery-1", harness.ReturnItemId, 0.7m, ReturnItemCondition.Damaged));

        Assert.Equal(1, await harness.ReceiptCountAsync());
        Assert.Equal(2, await harness.ReceiptLineCountAsync());
        Assert.Equal(1m, replay.Receiving!.Received);

        var fresh = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.RecordAsync(("delivery-2", harness.ReturnItemId, 0.1m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptUnitQuantityInvalid, fresh.Code);
        Assert.Equal(1, await harness.ReceiptCountAsync());
    }

    [Fact]
    public void The_owning_unit_rule_compares_values_not_representations()
    {
        Assert.True(OwningUnitQuantity.IsAlignedTo(1m, 0));
        Assert.True(OwningUnitQuantity.IsAlignedTo(1.000000m, 0));
        Assert.False(OwningUnitQuantity.IsAlignedTo(0.5m, 0));
        Assert.True(OwningUnitQuantity.IsAlignedTo(0.125m, 3));
        Assert.False(OwningUnitQuantity.IsAlignedTo(0.0001m, 3));
        Assert.False(OwningUnitQuantity.IsAlignedTo(1m, 7));
        Assert.False(OwningUnitQuantity.IsAlignedTo(1m, -1));
        Assert.False(OwningUnitQuantity.IsAlignedTo(0m, 3));
    }

    // ------------------------------------------------------------------ harness

    private static async Task<Harness> SeedAsync(
        decimal requested,
        int? unitDecimalPlaces,
        bool orderLineAnswered)
    {
        var db = CreateDb();
        var clock = new FixedClock(Now);
        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var orderLineId = Guid.NewGuid();
        var returnRequestId = Guid.NewGuid();

        var request = ReturnRequest.Create(
            returnRequestId,
            () => Guid.NewGuid(),
            sellerOrderId,
            Guid.NewGuid(),
            Harness.SellerPartyId,
            customer,
            $"tb277r1-{returnRequestId:N}",
            "دلیل مرجوعی",
            "IRR",
            [(orderLineId, requested, 1000m, (Guid?)null)],
            Now,
            RefundDestination.OriginalPayment);
        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var orderContext = new OrderReturnContextSnapshot(
            sellerOrderId,
            Guid.NewGuid(),
            Harness.SellerPartyId,
            customer,
            true,
            "IRR",
            [new OrderReturnLineSnapshot(
                orderLineId,
                requested,
                1000m,
                "IRR",
                null,
                true,
                7,
                null,
                null,
                QuantityDecimalPlacesSnapshot: unitDecimalPlaces)]);
        var orderReader = new SwitchableOrderReader(orderContext, orderLineAnswered);

        var gateway = new RecordingGateway();
        var wallet = new RecordingWallet();
        var inventory = new RecordingInventory();
        var directory = new ReturnDirectory(
            db,
            new OpenReturnUseCaseGuard(),
            orderReader,
            new ReturnEligibilityEvaluator(
                orderReader,
                new NoFulfillmentReader(),
                db,
                clock),
            new NoPaymentReader(),
            gateway,
            wallet,
            inventory,
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance,
            clock,
            new UuidV7IdGenerator());

        return new Harness(db, directory, returnRequestId, request.Items.Single().ReturnItemId, orderReader,
            gateway, wallet, inventory);
    }

    private static ReturnsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReturnsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ReturnsDbContext(options);
    }

    private sealed class Harness(
        ReturnsDbContext db,
        ReturnDirectory directory,
        Guid returnRequestId,
        Guid returnItemId,
        SwitchableOrderReader orderReader,
        RecordingGateway gateway,
        RecordingWallet wallet,
        RecordingInventory inventory) : IAsyncDisposable
    {
        public static Guid SellerPartyId { get; } = Guid.Parse("01a03826-97c5-7000-ad15-c9d141b1f32e");

        public Guid ReturnRequestId => returnRequestId;

        public Guid ReturnItemId => returnItemId;

        public SwitchableOrderReader OrderReader => orderReader;

        public RecordingGateway Gateway => gateway;

        public RecordingWallet Wallet => wallet;

        public RecordingInventory Inventory => inventory;

        public Task<ReturnSnapshot> RecordAsync(
            params (string OccurrenceKey, Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)[] lines)
        {
            var key = lines.Length == 0 ? "delivery" : lines[0].OccurrenceKey;
            return directory.RecordReceiptAsync(
                new RecordReturnReceiptCommand(
                    returnRequestId,
                    Guid.NewGuid(),
                    key,
                    lines.Select(x => new CustomerReceiptLine(x.ReturnItemId, x.Quantity, x.Condition)).ToArray()),
                CancellationToken.None);
        }

        public Task<ReturnSnapshot?> SnapshotAsync() =>
            directory.GetAsync(returnRequestId, CancellationToken.None);

        public Task<int> ReceiptCountAsync() =>
            db.ReturnReceipts.CountAsync(x => x.ReturnRequestId == returnRequestId);

        public Task<int> ReceiptLineCountAsync() =>
            db.ReturnReceiptLines.CountAsync(x => x.ReturnRequestId == returnRequestId);

        public async Task<IReadOnlyList<ReturnReceiptLine>> ReceiptLinesAsync() =>
            await db.ReturnReceiptLines.AsNoTracking()
                .Where(x => x.ReturnRequestId == returnRequestId)
                .ToListAsync();

        public ValueTask DisposeAsync() => db.DisposeAsync();
    }

    /// <summary>The owning Order answer, which can be switched off to prove the replay-before-new-gates order.</summary>
    private sealed class SwitchableOrderReader(OrderReturnContextSnapshot snapshot, bool answers) : IOrderReturnReader
    {
        private OrderReturnContextSnapshot? _snapshot = answers ? snapshot : null;

        public void StopAnswering() => _snapshot = null;

        public Task<OrderReturnContextSnapshot?> GetReturnContextAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_snapshot is not null && _snapshot.SellerOrderId == sellerOrderId ? _snapshot : null);
    }

    private sealed class NoFulfillmentReader : IFulfillmentReturnReader
    {
        public Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<FulfillmentReturnEligibilitySnapshot?>(null);

        public Task<IReadOnlyDictionary<Guid, DateTimeOffset?>> GetLastDeliveredAtBySellerOrderIdsAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, DateTimeOffset?>>(
                new Dictionary<Guid, DateTimeOffset?>());
    }

    private sealed class NoPaymentReader : IPaymentReturnReader
    {
        public Task<PaymentReturnSnapshot?> GetAsync(
            Guid payment,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(null);

        public Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(null);
    }

    private sealed class RecordingGateway : IPaymentRefundGateway
    {
        public List<(Guid PaymentId, decimal Amount, string Currency, string IdempotencyKey)> Calls { get; } = [];

        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls.Add((paymentId, amount, currency, idempotencyKey));
            return Task.FromResult(new GatewayRefundResult(true, $"ref-277r1-{Calls.Count}", null));
        }
    }

    private sealed class RecordingWallet : IWalletRefundCreditPort
    {
        public List<(Guid ReturnRequestId, decimal Amount, string IdempotencyKey)> Calls { get; } = [];

        public Task<WalletRefundCreditResultDto> CreditRefundAsync(
            Guid customerActorUserId,
            decimal amount,
            string currency,
            Guid returnRequestId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls.Add((returnRequestId, amount, idempotencyKey));
            return Task.FromResult(new WalletRefundCreditResultDto(amount, false));
        }
    }

    private sealed class RecordingInventory : IReturnInventoryGateway
    {
        public List<(Guid ReservationId, decimal Quantity, string IdempotencyKey)> Calls { get; } = [];

        public Task RestockConsumedReservationAsync(
            Guid reservationId,
            decimal quantity,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls.Add((reservationId, quantity, idempotencyKey));
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
