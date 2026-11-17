using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Order.Contracts.Fulfillment;
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
using Tooba.Returns.Infrastructure.Queries;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

/// <summary>
/// TB-278 — the customer's authoritative line eligibility and the owning-unit quantum at CREATION.
/// <para>
/// The read is bound to the owning customer before any fact is exposed, answers per-line delivered/occupied/remaining
/// quantities from the SAME evaluator the create command runs, and blocks an unanswered unit precision instead of
/// guessing one. The create command refuses a quantity that does not land on the owning unit's quantum (for example
/// half of a whole piece) before any row is written, while the exact replay of an already stored intent is still
/// answered — a committed occurrence is never re-described as invalid.
/// </para>
/// </summary>
public sealed class ReturnsCustomerSelection278Tests
{
    private static readonly DateTimeOffset DeliveredAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

    // ------------------------------------------------------------------ A: the customer eligibility read

    [Fact]
    public async Task Eligibility_is_bound_to_the_owning_customer_and_reports_per_line_remaining_and_unit()
    {
        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var variantA = Guid.NewGuid();
        var variantB = Guid.NewGuid();
        var order = BuildOrder(sellerOrderId, customer, [(lineA, 3m, 0), (lineB, 2m, 3)]);
        await using var db = CreateDb();

        // ONE ordinary active case already occupies 1 of line A: the customer read must report 2 remaining, not 3.
        await SeedActiveRequestAsync(db, order, lineA, 1m);

        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var engine = BuildEligibilityEngine(
            db,
            clock,
            order,
            new FixedGridEnrichment([(lineA, variantA, "pcs", 0), (lineB, variantB, "g", 3)]),
            new FixedCatalogTitles(new Dictionary<Guid, string> { [variantA] = "جوراب", [variantB] = "قهوه" }));

        var snapshot = await engine.QueryAsync(customer, sellerOrderId, CancellationToken.None);

        Assert.True(snapshot.Eligible);
        Assert.Equal(ReturnEligibilityReasonCodes.Eligible, snapshot.ReasonCode);
        Assert.Equal("IRR", snapshot.Currency);
        Assert.Equal(2, snapshot.Lines.Count);

        var a = snapshot.Lines.Single(x => x.OrderLineId == lineA);
        Assert.Equal(3m, a.DeliveredQuantity);
        Assert.Equal(1m, a.AlreadyReturnedQuantity);
        Assert.Equal(2m, a.RemainingReturnableQuantity);
        Assert.Equal(0, a.UnitDecimalPlaces);
        Assert.Equal("pcs", a.UnitLabel);
        Assert.Equal("جوراب", a.ProductLabel);
        Assert.Null(a.BlockerCode);
        Assert.True(a.Selectable);
        Assert.NotNull(a.ReturnableUntil);

        var b = snapshot.Lines.Single(x => x.OrderLineId == lineB);
        Assert.Equal(2m, b.RemainingReturnableQuantity);
        Assert.Equal(3, b.UnitDecimalPlaces);
        Assert.Equal("قهوه", b.ProductLabel);
        Assert.True(b.Selectable);
    }

    [Fact]
    public async Task A_foreign_customer_is_refused_before_any_line_or_product_fact_is_exposed()
    {
        var owner = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var order = BuildOrder(sellerOrderId, owner, [(lineId, 2m, 0)]);
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var enrichment = new FixedGridEnrichment([(lineId, Guid.NewGuid(), "pcs", 0)]);
        var engine = BuildEligibilityEngine(
            db,
            clock,
            order,
            enrichment,
            new FixedCatalogTitles(new Dictionary<Guid, string>()));

        var refused = await Assert.ThrowsAsync<ContractOperationException>(
            () => engine.QueryAsync(stranger, sellerOrderId, CancellationToken.None));

        Assert.Equal(ReturnsErrorCodes.NotOwner, refused.Code);
        // Nothing was read from the line/product side for a foreign scope.
        Assert.Equal(0, enrichment.Calls);
    }

    [Fact]
    public async Task An_unknown_seller_order_is_a_typed_missing_refusal()
    {
        var customer = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var engine = BuildEligibilityEngine(
            db,
            clock,
            BuildOrder(Guid.NewGuid(), customer, [(Guid.NewGuid(), 1m, 0)]),
            new FixedGridEnrichment([]),
            new FixedCatalogTitles(new Dictionary<Guid, string>()));

        var refused = await Assert.ThrowsAsync<ContractOperationException>(
            () => engine.QueryAsync(customer, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(ReturnsErrorCodes.Missing, refused.Code);
    }

    [Fact]
    public async Task An_unanswered_owning_precision_blocks_the_line_instead_of_guessing_a_scale()
    {
        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var order = BuildOrder(sellerOrderId, customer, [(lineId, 2m, 0)]);
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var engine = BuildEligibilityEngine(
            db,
            clock,
            order,
            // The owning enrichment did not answer this line at all.
            new FixedGridEnrichment([]),
            new FixedCatalogTitles(new Dictionary<Guid, string>()));

        var snapshot = await engine.QueryAsync(customer, sellerOrderId, CancellationToken.None);
        var line = snapshot.Lines.Single(x => x.OrderLineId == lineId);

        Assert.Null(line.UnitDecimalPlaces);
        Assert.Null(line.ProductLabel);
        Assert.Equal(ReturnEligibilityReasonCodes.UnitPrecisionUnavailable, line.BlockerCode);
        Assert.False(line.Selectable);
    }

    [Fact]
    public async Task An_expired_line_reports_the_window_blocker_and_its_last_returnable_instant()
    {
        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var order = BuildOrder(sellerOrderId, customer, [(lineId, 2m, 0)], windowDays: 7);
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(30));
        var engine = BuildEligibilityEngine(
            db,
            clock,
            order,
            new FixedGridEnrichment([(lineId, Guid.NewGuid(), "pcs", 0)]),
            new FixedCatalogTitles(new Dictionary<Guid, string>()));

        var snapshot = await engine.QueryAsync(customer, sellerOrderId, CancellationToken.None);
        var line = snapshot.Lines.Single(x => x.OrderLineId == lineId);

        Assert.Equal(ReturnEligibilityReasonCodes.WindowExpired, line.BlockerCode);
        Assert.Equal(DeliveredAt.AddDays(7), line.ReturnableUntil);
        Assert.Equal(0m, line.RemainingReturnableQuantity);
        Assert.False(line.Selectable);
        Assert.False(snapshot.Eligible);
    }

    // ------------------------------------------------------------------ B: the creation quantum guard

    [Fact]
    public async Task A_whole_piece_line_refuses_a_fraction_before_any_row_is_written()
    {
        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var order = BuildOrder(sellerOrderId, customer, [(lineId, 3m, 0)]);
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(db, clock, order);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, "tb278-half-piece", [new ReturnLineCommand(lineId, 0.5m)]),
            CancellationToken.None));

        Assert.Equal(ReturnsErrorCodes.QuantityUnitInvalid, refused.Code);
        Assert.Equal(0, await db.ReturnRequests.CountAsync());
        Assert.Equal(0, await db.ReturnItems.CountAsync());
    }

    [Fact]
    public async Task A_gram_line_accepts_its_own_quantum_and_still_refuses_a_finer_fraction()
    {
        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var order = BuildOrder(sellerOrderId, customer, [(lineId, 1m, 3)]);
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(db, clock, order);

        var tooFine = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, "tb278-too-fine", [new ReturnLineCommand(lineId, 0.0001m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.QuantityUnitInvalid, tooFine.Code);
        Assert.Equal(0, await db.ReturnRequests.CountAsync());

        var accepted = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb278-gram", [new ReturnLineCommand(lineId, 0.125m)]),
            CancellationToken.None);
        Assert.Equal(0.125m, accepted.Items.Single().Quantity);
    }

    [Fact]
    public async Task An_unanswered_owning_precision_never_grants_a_fraction_but_keeps_whole_pieces_lawful()
    {
        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var order = BuildOrder(sellerOrderId, customer, [(lineId, 2m, null)]);
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(db, clock, order);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, "tb278-unknown-fraction", [new ReturnLineCommand(lineId, 0.5m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.QuantityUnitInvalid, refused.Code);
        Assert.Equal(0, await db.ReturnRequests.CountAsync());

        var whole = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb278-unknown-whole", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);
        Assert.Equal(1m, whole.Items.Single().Quantity);
    }

    [Fact]
    public async Task An_exact_replay_is_answered_before_the_quantum_gate_reads_metadata_again()
    {
        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var order = BuildOrder(sellerOrderId, customer, [(lineId, 1m, 2)]);
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var reader = new SwitchableOrderReader(order);
        var directory = BuildDirectory(db, clock, reader);

        var first = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb278-replay", [new ReturnLineCommand(lineId, 0.5m)]),
            CancellationToken.None);
        Assert.Equal(0.5m, first.Items.Single().Quantity);

        // The owning metadata now answers a whole-piece unit for the SAME already-committed occurrence. The exact
        // replay must still answer that stored case; re-deciding would describe a committed intent as invalid.
        reader.Current = BuildOrder(
            sellerOrderId,
            customer,
            [(lineId, 1m, 0)],
            checkoutId: order.CheckoutId,
            sellerPartyId: order.SellerPartyId);
        var replay = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb278-replay", [new ReturnLineCommand(lineId, 0.5m)]),
            CancellationToken.None);

        Assert.Equal(first.ReturnRequestId, replay.ReturnRequestId);
        Assert.Equal(1, await db.ReturnRequests.CountAsync());
    }

    // ------------------------------------------------------------------ fixtures

    private static CreateReturnCommand Command(
        Guid sellerOrderId,
        Guid actorUserId,
        string idempotencyKey,
        IReadOnlyList<ReturnLineCommand> items) =>
        new(sellerOrderId, actorUserId, idempotencyKey, "دلیل مرجوعی", items, RefundDestination.OriginalPayment);

    private static OrderReturnContextSnapshot BuildOrder(
        Guid sellerOrderId,
        Guid customerUserId,
        (Guid LineId, decimal Quantity, int? DecimalPlaces)[] lines,
        int windowDays = 7,
        Guid? checkoutId = null,
        Guid? sellerPartyId = null) =>
        new(
            sellerOrderId,
            checkoutId ?? Guid.NewGuid(),
            sellerPartyId ?? Guid.NewGuid(),
            customerUserId,
            true,
            "IRR",
            lines.Select(x => new OrderReturnLineSnapshot(
                x.LineId,
                x.Quantity,
                1000m,
                "IRR",
                null,
                true,
                windowDays,
                null,
                null,
                x.DecimalPlaces)).ToArray());

    private static async Task SeedActiveRequestAsync(
        ReturnsDbContext db,
        OrderReturnContextSnapshot order,
        Guid lineId,
        decimal quantity)
    {
        var request = ReturnRequest.Create(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            order.SellerOrderId,
            order.CheckoutId,
            order.SellerPartyId,
            order.PlacedByUserId,
            "tb278-seed",
            "مرجوعی موجود",
            order.Currency,
            [(lineId, quantity, 1000m, (Guid?)null)],
            DeliveredAt.AddHours(1),
            RefundDestination.OriginalPayment);
        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static CustomerReturnEligibilityQueryEngine BuildEligibilityEngine(
        ReturnsDbContext db,
        MutableClock clock,
        OrderReturnContextSnapshot order,
        IOrderGridEnrichmentReader enrichment,
        ICatalogVariantLookup catalog)
    {
        var orderReader = new SwitchableOrderReader(order);
        var evaluator = new ReturnEligibilityEvaluator(
            orderReader,
            new MapFulfillmentReader(orderReader),
            db,
            clock);
        return new CustomerReturnEligibilityQueryEngine(orderReader, evaluator, enrichment, catalog);
    }

    private static ReturnDirectory BuildDirectory(
        ReturnsDbContext db,
        MutableClock clock,
        OrderReturnContextSnapshot order) =>
        BuildDirectory(db, clock, new SwitchableOrderReader(order));

    private static ReturnDirectory BuildDirectory(
        ReturnsDbContext db,
        MutableClock clock,
        IOrderReturnReader reader)
    {
        var fulfillment = new MapFulfillmentReader(reader);
        var eligibility = new ReturnEligibilityEvaluator(reader, fulfillment, db, clock);
        return new ReturnDirectory(
            db,
            new OpenReturnUseCaseGuard(),
            reader,
            eligibility,
            new FixedPaymentReader(Guid.NewGuid()),
            new RecordingRefundGateway(),
            new RecordingWallet(),
            new RecordingInventory(),
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance,
            clock,
            new UuidV7IdGenerator());
    }

    private static ReturnsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReturnsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ReturnsDbContext(options);
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class SwitchableOrderReader(OrderReturnContextSnapshot current) : IOrderReturnReader
    {
        public OrderReturnContextSnapshot Current { get; set; } = current;

        public Task<OrderReturnContextSnapshot?> GetReturnContextAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderReturnContextSnapshot?>(
                Current.SellerOrderId == sellerOrderId ? Current : null);
    }

    private sealed class MapFulfillmentReader : IFulfillmentReturnReader
    {
        private readonly IOrderReturnReader _reader;

        public MapFulfillmentReader(IOrderReturnReader reader) => _reader = reader;

        public async Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken)
        {
            var order = await _reader.GetReturnContextAsync(sellerOrderId, cancellationToken);
            if (order is null)
            {
                return null;
            }

            var delivered = order.Lines.ToDictionary(x => x.OrderLineId, x => x.Quantity);
            return new FulfillmentReturnEligibilitySnapshot(
                sellerOrderId,
                delivered,
                DeliveredAt,
                delivered.Keys.ToDictionary(x => x, _ => DeliveredAt),
                delivered.Select(x => new LineDeliverySlice(x.Key, x.Value, DeliveredAt)).ToArray());
        }

        public Task<IReadOnlyDictionary<Guid, DateTimeOffset?>> GetLastDeliveredAtBySellerOrderIdsAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, DateTimeOffset?>>(
                sellerOrderIds.ToDictionary(x => x, _ => (DateTimeOffset?)DeliveredAt));
    }

    private sealed class FixedGridEnrichment(
        IReadOnlyList<(Guid LineId, Guid VariantId, string? UnitCode, int DecimalPlaces)> lines)
        : IOrderGridEnrichmentReader
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyDictionary<Guid, OrderGridLineSnapshot>> GetLinesAsync(
            IReadOnlyList<Guid> orderLineIds,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<IReadOnlyDictionary<Guid, OrderGridLineSnapshot>>(
                lines
                    .Where(x => orderLineIds.Contains(x.LineId))
                    .ToDictionary(
                        x => x.LineId,
                        x => new OrderGridLineSnapshot(
                            x.LineId, x.VariantId, x.UnitCode, x.DecimalPlaces, null, true, 7)));
        }

        public Task<IReadOnlyDictionary<Guid, string>> GetOrderNumbersAsync(
            IReadOnlyList<Guid> sellerOrderIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> SearchSellerOrderIdsByOrderNumberAsync(
            string term, int take, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> FilterSellerOrderIdsByOrderNumberAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, string>> GetRecipientNamesAsync(
            IReadOnlyList<Guid> checkoutIds, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> SearchCheckoutIdsByRecipientAsync(
            string term, int take, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> FilterCheckoutIdsByRecipientAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FixedCatalogTitles(IReadOnlyDictionary<Guid, string> titles) : ICatalogVariantLookup
    {
        public Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(
            IReadOnlyList<Guid> variantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                titles.Where(x => variantIds.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value));

        public Task<CatalogVariantLookupResult?> FindVariantAsync(
            Guid variantId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
            IReadOnlyList<Guid> variantIds, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> GetPublishedVariantIdsAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FixedPaymentReader(Guid paymentId) : IPaymentReturnReader
    {
        public Task<PaymentReturnSnapshot?> GetAsync(
            Guid payment,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                new PaymentReturnSnapshot(paymentId, Guid.NewGuid(), 1000m, "IRR", "Succeeded"));

        public Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                new PaymentReturnSnapshot(paymentId, checkoutId, 1000m, "IRR", "Succeeded"));
    }

    private sealed class RecordingRefundGateway : IPaymentRefundGateway
    {
        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new GatewayRefundResult(true, "ref-278", null));
    }

    private sealed class RecordingWallet : IWalletRefundCreditPort
    {
        public Task<WalletRefundCreditResultDto> CreditRefundAsync(
            Guid customerActorUserId,
            decimal amount,
            string currency,
            Guid returnRequestId,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new WalletRefundCreditResultDto(amount, false));
    }

    private sealed class RecordingInventory : IReturnInventoryGateway
    {
        public Task RestockConsumedReservationAsync(
            Guid reservationId,
            decimal quantity,
            string idempotencyKey,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
