using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Order.Contracts.Fulfillment;
using Xunit;
using Xunit.Abstractions;
using AppModels = Tooba.Fulfillment.Application.Fulfillments.Models;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-258 on REAL storage: the post-dispatch exception path is proved against the ONE canonical PostgreSQL database
/// through the production command seam (<see cref="FulfillmentDirectory"/>), never an in-memory double.
/// </summary>
/// <remarks>
/// Gated by <c>TB258_CONNECTION</c>: with no target nothing runs and nothing is claimed. Every row is created with
/// the production domain/application paths only (paid-order unit → processing → packing → parcel), is
/// task-labelled (<c>TB258-…</c>), and its exact identities are appended to <c>TB258_EVIDENCE_DIR</c> when supplied.
/// No user order, parcel or receipt row is read or written, and nothing is deleted.
/// </remarks>
public sealed class PostDispatchExceptionRelational258Tests
{
    private readonly ITestOutputHelper _output;

    public PostDispatchExceptionRelational258Tests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable("TB258_CONNECTION") is { Length: > 0 } raw ? raw : null;

    // ------------------------------------------------------------------ A: representable physical return

    [Fact]
    public async Task A_dispatched_parcel_returns_to_origin_and_a_resend_reaches_the_customer_without_erasing_history()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateDispatchedFixtureAsync("A");
        await using var lease = fixture.NewDirectory();

        await lease.Directory.RecordNonDeliveryAsync(
            fixture.FulfillmentId,
            fixture.ShipmentId,
            fixture.ActorUserId,
            "customer_unavailable",
            "TB258-A",
            CancellationToken.None);
        await lease.Directory.StartReturnAsync(
            fixture.FulfillmentId, fixture.ShipmentId, fixture.ActorUserId, CancellationToken.None);

        var receipt = await lease.Directory.RecordReturnReceiptAsync(
            fixture.FulfillmentId,
            fixture.ShipmentId,
            fixture.ActorUserId,
            [
                new AppModels.ReturnReceiptLineCommand(fixture.LineAId, 1m, ReturnReceiptCondition.Usable),
                new AppModels.ReturnReceiptLineCommand(fixture.LineBId, 1m, ReturnReceiptCondition.Damaged),
            ],
            "TB258-A receipt",
            CancellationToken.None);
        var receivedShipment = receipt.Shipments.Single(x => x.ShipmentId == fixture.ShipmentId);
        Assert.Equal(ShipmentStatus.Returned, receivedShipment.Status);
        Assert.NotNull(receivedShipment.ReturnReceipt);
        Assert.Equal(1m, receivedShipment.ReturnReceipt!.UsableQuantity);
        Assert.Equal(1m, receipt.Items.Single(x => x.OrderLineId == fixture.LineAId).ResendableQuantity);
        // Damaged goods never become resend capacity.
        Assert.Equal(0m, receipt.Items.Single(x => x.OrderLineId == fixture.LineBId).ResendableQuantity);

        var resend = await lease.Directory.CreateResendShipmentAsync(
            fixture.FulfillmentId,
            fixture.ActorUserId,
            fixture.ShipmentId,
            [new AppModels.ShipmentLineCommand(fixture.LineAId, 1m)],
            CancellationToken.None,
            $"tb258-A-resend-{fixture.ShipmentId:N}");
        await lease.Directory.AssignTrackingAsync(
            fixture.FulfillmentId,
            resend.CreatedShipmentId,
            fixture.ActorUserId,
            $"TRK-TB258-A-R1-{fixture.ShipmentId:N}",
            CancellationToken.None);
        await lease.Directory.DispatchShipmentAsync(
            fixture.FulfillmentId, resend.CreatedShipmentId, fixture.ActorUserId, CancellationToken.None);
        var delivered = await lease.Directory.DeliverShipmentAsync(
            fixture.FulfillmentId, resend.CreatedShipmentId, fixture.ActorUserId, CancellationToken.None);

        var original = delivered.Shipments.Single(x => x.ShipmentId == fixture.ShipmentId);
        var resent = delivered.Shipments.Single(x => x.ShipmentId == resend.CreatedShipmentId);
        Assert.Equal(ShipmentStatus.Returned, original.Status);
        Assert.Equal(fixture.ShipmentId, resent.ResendOfShipmentId);
        Assert.Equal(ShipmentStatus.Delivered, resent.Status);
        Assert.Equal(fixture.ShippingGroupId, resent.ShippingGroupId);
        Assert.Equal("post", resent.ShippingMethodCode);
        // Lifetime dispatched quantity is what physically left the warehouse the first time.
        Assert.Equal(2m, delivered.Items.Single(x => x.OrderLineId == fixture.LineAId).QuantityShipped);
        Assert.Equal(1m, delivered.Items.Single(x => x.OrderLineId == fixture.LineAId).DeliveredQuantity);
        Assert.Equal(0m, delivered.Items.Single(x => x.OrderLineId == fixture.LineAId).ResendableQuantity);
        // One unit of A never reached the customer, so the whole unit is NOT delivered.
        Assert.NotEqual(FulfillmentStatus.Delivered, delivered.Status);

        // Real rows: one receipt with two lines, one resend pointing at the returned original.
        Assert.Equal(1, await RawScalarAsync(
            fixture.Target,
            "select count(*) from fulfillment.shipment_return_receipts where shipment_id = @id",
            ("id", fixture.ShipmentId)));
        Assert.Equal(2, await RawScalarAsync(
            fixture.Target,
            "select count(*) from fulfillment.shipment_return_receipt_items i "
            + "join fulfillment.shipment_return_receipts r on r.shipment_return_receipt_id = i.shipment_return_receipt_id "
            + "where r.shipment_id = @id",
            ("id", fixture.ShipmentId)));
        Assert.Equal(1, await RawScalarAsync(
            fixture.Target,
            "select count(*) from fulfillment.shipments where resend_of_shipment_id = @id and status = 'Delivered'",
            ("id", fixture.ShipmentId)));
        Assert.Equal("Returned", await RawShipmentStatusAsync(fixture.Target, fixture.ShipmentId));

        fixture.Record(
            _output,
            $"A original={fixture.ShipmentId} resend={resend.CreatedShipmentId} "
            + $"lineA={fixture.LineAId} lineB={fixture.LineBId}");
    }

    // ------------------------------------------------------------------ C: exact capacity

    [Fact]
    public async Task C_non_delivery_grants_nothing_and_only_a_real_usable_receipt_funds_one_resend()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateDispatchedFixtureAsync("C");
        await using var lease = fixture.NewDirectory();

        await lease.Directory.RecordNonDeliveryAsync(
            fixture.FulfillmentId,
            fixture.ShipmentId,
            fixture.ActorUserId,
            "refused",
            null,
            CancellationToken.None);

        var afterNonDelivery = await lease.Directory.GetAsync(fixture.FulfillmentId, CancellationToken.None);
        Assert.Equal(0m, afterNonDelivery!.Items.Single(x => x.OrderLineId == fixture.LineAId).ResendableQuantity);

        var tooEarly = await Assert.ThrowsAsync<ContractOperationException>(() =>
            lease.Directory.CreateResendShipmentAsync(
                fixture.FulfillmentId,
                fixture.ActorUserId,
                fixture.ShipmentId,
                [new AppModels.ShipmentLineCommand(fixture.LineAId, 1m)],
                CancellationToken.None,
                $"tb258-C-early-{fixture.ShipmentId:N}"));
        Assert.Equal("fulfillment.resend.original_not_returned", tooEarly.Code);

        await lease.Directory.StartReturnAsync(
            fixture.FulfillmentId, fixture.ShipmentId, fixture.ActorUserId, CancellationToken.None);
        var returning = await lease.Directory.GetAsync(fixture.FulfillmentId, CancellationToken.None);
        Assert.Equal(0m, returning!.Items.Single(x => x.OrderLineId == fixture.LineAId).ResendableQuantity);

        // A receipt can never claim more than that parcel actually carried.
        var exceeds = await Assert.ThrowsAsync<ContractOperationException>(() =>
            lease.Directory.RecordReturnReceiptAsync(
                fixture.FulfillmentId,
                fixture.ShipmentId,
                fixture.ActorUserId,
                [new AppModels.ReturnReceiptLineCommand(fixture.LineAId, 3m, ReturnReceiptCondition.Usable)],
                null,
                CancellationToken.None));
        Assert.Equal("fulfillment.return.receipt_qty_exceeds", exceeds.Code);

        // A line that this parcel never carried is foreign to the receipt.
        var foreign = await Assert.ThrowsAsync<ContractOperationException>(() =>
            lease.Directory.RecordReturnReceiptAsync(
                fixture.FulfillmentId,
                fixture.ShipmentId,
                fixture.ActorUserId,
                [new AppModels.ReturnReceiptLineCommand(Guid.NewGuid(), 1m, ReturnReceiptCondition.Usable)],
                null,
                CancellationToken.None));
        Assert.Equal("fulfillment.return.receipt_line_foreign", foreign.Code);

        var receiptItems = new[]
        {
            new AppModels.ReturnReceiptLineCommand(fixture.LineAId, 1m, ReturnReceiptCondition.Usable),
        };
        var first = await lease.Directory.RecordReturnReceiptAsync(
            fixture.FulfillmentId,
            fixture.ShipmentId,
            fixture.ActorUserId,
            receiptItems,
            "TB258-C",
            CancellationToken.None);
        Assert.Equal(1m, first.Items.Single(x => x.OrderLineId == fixture.LineAId).ResendableQuantity);

        // An identical retry reconciles the ONE durable receipt instead of granting capacity twice.
        var replay = await lease.Directory.RecordReturnReceiptAsync(
            fixture.FulfillmentId,
            fixture.ShipmentId,
            fixture.ActorUserId,
            receiptItems,
            "ignored on replay",
            CancellationToken.None);
        Assert.Equal(1m, replay.Items.Single(x => x.OrderLineId == fixture.LineAId).ResendableQuantity);
        Assert.Single(replay.Shipments.Single(x => x.ShipmentId == fixture.ShipmentId).ReturnReceipt!.Items);

        // A different payload for the same parcel is a typed conflict, never a second receipt.
        var conflict = await Assert.ThrowsAsync<ContractOperationException>(() =>
            lease.Directory.RecordReturnReceiptAsync(
                fixture.FulfillmentId,
                fixture.ShipmentId,
                fixture.ActorUserId,
                [new AppModels.ReturnReceiptLineCommand(fixture.LineAId, 1m, ReturnReceiptCondition.Damaged)],
                null,
                CancellationToken.None));
        Assert.Equal("fulfillment.return.receipt_conflict", conflict.Code);
        Assert.Equal(1, await RawScalarAsync(
            fixture.Target,
            "select count(*) from fulfillment.shipment_return_receipts where shipment_id = @id",
            ("id", fixture.ShipmentId)));

        var resend = await lease.Directory.CreateResendShipmentAsync(
            fixture.FulfillmentId,
            fixture.ActorUserId,
            fixture.ShipmentId,
            [new AppModels.ShipmentLineCommand(fixture.LineAId, 1m)],
            CancellationToken.None,
            $"tb258-C-resend-{fixture.ShipmentId:N}");
        var consumed = await lease.Directory.GetAsync(fixture.FulfillmentId, CancellationToken.None);
        Assert.Equal(0m, consumed!.Items.Single(x => x.OrderLineId == fixture.LineAId).ResendableQuantity);

        // Cancelling the NEW pre-dispatch resend releases its own capacity exactly once.
        var cancelled = await lease.Directory.CancelShipmentAsync(
            fixture.FulfillmentId, resend.CreatedShipmentId, fixture.ActorUserId, CancellationToken.None);
        Assert.Equal(1m, cancelled.Items.Single(x => x.OrderLineId == fixture.LineAId).ResendableQuantity);
        var again = await lease.Directory.CancelShipmentAsync(
            fixture.FulfillmentId, resend.CreatedShipmentId, fixture.ActorUserId, CancellationToken.None);
        Assert.Equal(1m, again.Items.Single(x => x.OrderLineId == fixture.LineAId).ResendableQuantity);

        // The original parcel keeps its history; only the cancelled resend releases capacity.
        Assert.Equal(ShipmentStatus.Returned, again.Shipments.Single(x => x.ShipmentId == fixture.ShipmentId).Status);
        Assert.Equal(ShipmentStatus.Cancelled, again.Shipments.Single(x => x.ShipmentId == resend.CreatedShipmentId).Status);

        fixture.Record(_output, $"C original={fixture.ShipmentId} cancelledResend={resend.CreatedShipmentId}");
    }

    // ------------------------------------------------------------------ D: concurrency and retry identity

    [Fact]
    public async Task D_two_independent_contexts_cannot_consume_the_same_returned_unit_and_a_replay_returns_one_parcel()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateDispatchedFixtureAsync("D", returnedUsable: true);
        var barrier = new ConcurrentSaveBarrier();
        await using var left = fixture.NewDirectory(barrier);
        await using var right = fixture.NewDirectory(barrier);
        var leftKey = $"tb258-D-left-{fixture.ShipmentId:N}";
        var rightKey = $"tb258-D-right-{fixture.ShipmentId:N}";

        async Task<Guid?> AllocateAsync(DirectoryLease lease, string key)
        {
            try
            {
                var created = await lease.Directory.CreateResendShipmentAsync(
                    fixture.FulfillmentId,
                    fixture.ActorUserId,
                    fixture.ShipmentId,
                    [new AppModels.ShipmentLineCommand(fixture.LineAId, 1m)],
                    CancellationToken.None,
                    key);
                return created.CreatedShipmentId;
            }
            catch (ContractOperationException ex) when (ex.Code is "fulfillment.resend.concurrency_conflict"
                or "fulfillment.resend.exceeds_returned")
            {
                return null;
            }
        }

        var results = await Task.WhenAll(
            AllocateAsync(left, leftKey),
            AllocateAsync(right, rightKey));

        var winners = results.Where(x => x is not null).Select(x => x!.Value).ToArray();
        Assert.Single(winners);
        Assert.Equal(1, await RawScalarAsync(
            fixture.Target,
            "select count(*) from fulfillment.shipments where resend_of_shipment_id = @id",
            ("id", fixture.ShipmentId)));
        Assert.NotNull(barrier.ObservedOverlap
            ? "overlapped"
            : throw new InvalidOperationException("the two contexts never overlapped their save"));

        // A retried request with the SAME intent key returns the exact recorded parcel.
        var winnerKey = results[0] is null ? rightKey : leftKey;
        await using var replayLease = fixture.NewDirectory();
        var replay = await replayLease.Directory.CreateResendShipmentAsync(
            fixture.FulfillmentId,
            fixture.ActorUserId,
            fixture.ShipmentId,
            [new AppModels.ShipmentLineCommand(fixture.LineAId, 1m)],
            CancellationToken.None,
            winnerKey);
        Assert.Equal(winners[0], replay.CreatedShipmentId);
        Assert.Equal(1, await RawScalarAsync(
            fixture.Target,
            "select count(*) from fulfillment.shipments where resend_of_shipment_id = @id",
            ("id", fixture.ShipmentId)));

        fixture.Record(
            _output,
            $"D winner={winners[0]} loserKey={(results[0] is null ? "tb258-D-left" : "tb258-D-right")} "
            + "resendRows=1 replay=same");
    }

    // ------------------------------------------------------------------ E: conservation

    [Fact]
    public async Task E_delivery_is_per_parcel_truth_so_one_other_in_transit_parcel_keeps_the_unit_undelivered()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateDispatchedFixtureAsync("E", splitIntoTwoParcels: true);
        await using var lease = fixture.NewDirectory();
        var delivered = await lease.Directory.DeliverShipmentAsync(
            fixture.FulfillmentId, fixture.ShipmentIds[0], fixture.ActorUserId, CancellationToken.None);

        Assert.Equal(1m, delivered.Items.Single(x => x.OrderLineId == fixture.LineAId).DeliveredQuantity);
        Assert.Equal(2m, delivered.Items.Single(x => x.OrderLineId == fixture.LineAId).QuantityShipped);
        Assert.NotEqual(FulfillmentStatus.Delivered, delivered.Status);
        Assert.Equal(ShipmentStatus.Dispatched, delivered.Shipments.Single(x => x.ShipmentId == fixture.ShipmentIds[1]).Status);

        fixture.Record(
            _output,
            $"E first={fixture.ShipmentIds[0]} secondInTransit={fixture.ShipmentIds[1]} delivered=1");
    }

    // ------------------------------------------------------------------ B: illegal transitions / authority

    [Fact]
    public async Task B_delivered_parcel_cannot_be_undelivered_and_a_foreign_or_pre_dispatch_parcel_is_refused()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateDispatchedFixtureAsync("B", splitIntoTwoParcels: true, dispatch: false);
        await using var lease = fixture.NewDirectory();

        // A parcel of another fulfillment can never be reached through this unit.
        var foreign = await Assert.ThrowsAsync<ContractOperationException>(() =>
            lease.Directory.RecordNonDeliveryAsync(
                fixture.FulfillmentId,
                Guid.NewGuid(),
                fixture.ActorUserId,
                "other",
                null,
                CancellationToken.None));
        Assert.Equal("fulfillment.shipment.not_found", foreign.Code);

        // The undispatched second parcel cannot be marked non-delivered or returning yet.
        var preDispatch = await Assert.ThrowsAsync<ContractOperationException>(() =>
            lease.Directory.RecordNonDeliveryAsync(
                fixture.FulfillmentId,
                fixture.ShipmentIds[1],
                fixture.ActorUserId,
                "other",
                null,
                CancellationToken.None));
        Assert.Equal("fulfillment.nondelivery.invalid_status", preDispatch.Code);
        var returnBeforeDispatch = await Assert.ThrowsAsync<ContractOperationException>(() =>
            lease.Directory.StartReturnAsync(
                fixture.FulfillmentId, fixture.ShipmentIds[1], fixture.ActorUserId, CancellationToken.None));
        Assert.Equal("fulfillment.return.invalid_status", returnBeforeDispatch.Code);

        // The first parcel is the dispatched one; the second stays Created for the pre-dispatch refusals above.
        await lease.Directory.AssignTrackingAsync(
            fixture.FulfillmentId, fixture.ShipmentIds[0], fixture.ActorUserId, $"TRK-TB258-B-1-{fixture.ShipmentIds[0]:N}", CancellationToken.None);
        await lease.Directory.DispatchShipmentAsync(
            fixture.FulfillmentId, fixture.ShipmentIds[0], fixture.ActorUserId, CancellationToken.None);
        await lease.Directory.DeliverShipmentAsync(
            fixture.FulfillmentId, fixture.ShipmentIds[0], fixture.ActorUserId, CancellationToken.None);
        var afterDelivered = await Assert.ThrowsAsync<ContractOperationException>(() =>
            lease.Directory.RecordNonDeliveryAsync(
                fixture.FulfillmentId,
                fixture.ShipmentIds[0],
                fixture.ActorUserId,
                "other",
                null,
                CancellationToken.None));
        Assert.Equal("fulfillment.nondelivery.after_delivered", afterDelivered.Code);
        Assert.Equal("Delivered", await RawShipmentStatusAsync(fixture.Target, fixture.ShipmentIds[0]));

        fixture.Record(_output, $"B delivered={fixture.ShipmentIds[0]} preDispatch={fixture.ShipmentIds[1]}");
    }

    // ------------------------------------------------------------------ F: shared package lock

    [Fact]
    public async Task F_a_parcel_locked_in_an_active_consolidated_package_cannot_independently_record_nondelivery()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateDispatchedFixtureAsync("F", splitIntoTwoParcels: true, dispatch: false);
        await using var lease = fixture.NewDirectory();
        // A consolidated package is only lawful across two DIFFERENT sellers of the same checkout, so the second
        // member is a real second-seller parcel of this checkout (not a fabricated member of the same seller).
        var (otherSellerFulfillmentId, otherSellerShipmentId) = await CreateSecondSellerParcelAsync(fixture, "F");
        await lease.Directory.AssignTrackingAsync(
            fixture.FulfillmentId,
            fixture.ShipmentIds[0],
            fixture.ActorUserId,
            $"TRK-TB258-F-1-{fixture.ShipmentIds[0]:N}",
            CancellationToken.None);
        await lease.Directory.AssignTrackingAsync(
            otherSellerFulfillmentId,
            otherSellerShipmentId,
            fixture.ActorUserId,
            $"TRK-TB258-F-2-{otherSellerShipmentId:N}",
            CancellationToken.None);
        var package = await lease.Directory.CreateConsolidatedPackageAsync(
            fixture.CheckoutId,
            [fixture.ShipmentIds[0], otherSellerShipmentId],
            "post",
            null,
            "TB258-F package",
            fixture.ActorUserId,
            CancellationToken.None);
        await lease.Directory.DispatchConsolidatedPackageAsync(
            package.ConsolidatedPackageId, fixture.ActorUserId, CancellationToken.None);

        var locked = await Assert.ThrowsAsync<ContractOperationException>(() =>
            lease.Directory.RecordNonDeliveryAsync(
                fixture.FulfillmentId,
                fixture.ShipmentIds[0],
                fixture.ActorUserId,
                "refused",
                null,
                CancellationToken.None));
        Assert.Equal("fulfillment.shipment.locked_by_consolidated_package", locked.Code);
        Assert.Equal("Dispatched", await RawShipmentStatusAsync(fixture.Target, fixture.ShipmentIds[0]));

        fixture.Record(
            _output,
            $"F package={package.ConsolidatedPackageId} members={fixture.ShipmentIds[0]},{otherSellerShipmentId} locked=refused");
    }

    /// <summary>
    /// TB-258 F: the second member a consolidated package requires — a real paid-order unit of a DIFFERENT seller in
    /// the SAME checkout, packed into one pre-dispatch parcel through the production directory.
    /// </summary>
    private static async Task<(Guid FulfillmentId, Guid ShipmentId)> CreateSecondSellerParcelAsync(Fixture fixture, string tag)
    {
        var fulfillmentId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            options,
            fixture.Target.ConnectionString,
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));

        await using var db = new FulfillmentDbContext(options.Options);
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            fulfillmentId,
            Guid.NewGuid,
            Guid.NewGuid(),
            fixture.CheckoutId,
            sellerPartyId,
            Guid.NewGuid(),
            $"TB258-{tag} فروشنده دوم",
            "09120000001",
            "تهران",
            "تهران",
            $"نشانی آزمون تسک 258 {tag} دوم",
            "1234567891",
            "post",
            "پیشتاز",
            [(lineId, 1m, (Guid?)null)],
            now,
            [
                new FulfillmentShippingGroupPlan(
                    groupId,
                    sellerPartyId,
                    "post",
                    null,
                    null,
                    "پیشتاز",
                    null,
                    DateOnly.FromDateTime(now.UtcDateTime),
                    null,
                    null,
                    1,
                    [lineId]),
            ]);
        db.Fulfillments.Add(unit);
        db.Items.AddRange(unit.Items);
        db.ShippingGroups.AddRange(unit.ShippingGroups);
        await db.SaveChangesAsync();

        var directory = new FulfillmentDirectory(
            db,
            new OpenFulfillmentUseCaseGuard(),
            new UnusedOrderReader(),
            new UnusedInventoryGateway(),
            new FulfillmentInstrumentation(),
            new FixedClock(),
            new NpgsqlIdGenerator());
        var actorUserId = fixture.ActorUserId;
        await directory.MarkProcessingAsync(fulfillmentId, actorUserId, CancellationToken.None);
        await directory.PackSelectionsAsync(
            fulfillmentId,
            actorUserId,
            [new AppModels.FulfillmentSelectionCommand(lineId, 1m)],
            CancellationToken.None);
        var created = await directory.CreateShipmentAsync(
            fulfillmentId,
            actorUserId,
            "پست",
            [new AppModels.ShipmentLineCommand(lineId, 1m)],
            CancellationToken.None,
            shippingMethodCode: "post",
            providerMetadataJson: PostMetadataJson,
            shippingGroupId: groupId);
        return (fulfillmentId, created.CreatedShipmentId);
    }

    // ---- fixture -------------------------------------------------------------------------------

    private sealed record Fixture(
        NpgsqlConnectionStringBuilder Target,
        Guid FulfillmentId,
        Guid CheckoutId,
        Guid ShippingGroupId,
        Guid LineAId,
        Guid LineBId,
        IReadOnlyList<Guid> ShipmentIds,
        Guid ActorUserId)
    {
        public Guid ShipmentId => ShipmentIds[0];

        public DirectoryLease NewDirectory(SaveChangesInterceptor? interceptor = null)
        {
            var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
            Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
                options,
                Target.ConnectionString,
                FulfillmentDbContext.Schema,
                typeof(FulfillmentDbContext));
            if (interceptor is not null)
            {
                options.AddInterceptors(interceptor);
            }

            var db = new FulfillmentDbContext(options.Options);
            return new DirectoryLease(
                db,
                new FulfillmentDirectory(
                    db,
                    new OpenFulfillmentUseCaseGuard(),
                    new UnusedOrderReader(),
                    new UnusedInventoryGateway(),
                    new FulfillmentInstrumentation(),
                    new FixedClock(),
                    new NpgsqlIdGenerator()));
        }

        public void Record(ITestOutputHelper output, string line)
        {
            output.WriteLine(line);
            var directory = Environment.GetEnvironmentVariable("TB258_EVIDENCE_DIR");
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "tb258-relational.txt"), line + Environment.NewLine);
        }
    }

    private sealed class DirectoryLease(FulfillmentDbContext db, FulfillmentDirectory directory) : IAsyncDisposable
    {
        public FulfillmentDirectory Directory { get; } = directory;

        public ValueTask DisposeAsync() => db.DisposeAsync();
    }

    private const string PostMetadataJson =
        "{\"recipientName\":\"TB258 receiver\",\"recipientPhone\":\"09120000000\","
        + "\"postalCode\":\"1234567890\",\"destinationAddress\":\"TB258 task address\"}";

    private static async Task<Fixture> CreateDispatchedFixtureAsync(
        string tag,
        bool splitIntoTwoParcels = false,
        bool dispatch = true,
        bool returnedUsable = false)
    {
        var target = new NpgsqlConnectionStringBuilder(Connection!);
        var fulfillmentId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            options,
            target.ConnectionString,
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));

        await using var db = new FulfillmentDbContext(options.Options);
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            fulfillmentId,
            Guid.NewGuid,
            Guid.NewGuid(),
            checkoutId,
            sellerPartyId,
            Guid.NewGuid(),
            $"TB258-{tag} گیرنده",
            "09120000000",
            "تهران",
            "تهران",
            $"نشانی آزمون تسک 258 {tag}",
            "1234567890",
            "post",
            "پیشتاز",
            [(lineA, 2m, (Guid?)null), (lineB, 1m, (Guid?)null)],
            now,
            [
                new FulfillmentShippingGroupPlan(
                    groupId,
                    sellerPartyId,
                    "post",
                    null,
                    null,
                    "پیشتاز",
                    null,
                    DateOnly.FromDateTime(now.UtcDateTime),
                    null,
                    null,
                    1,
                    [lineA, lineB]),
            ]);
        db.Fulfillments.Add(unit);
        db.Items.AddRange(unit.Items);
        db.ShippingGroups.AddRange(unit.ShippingGroups);
        await db.SaveChangesAsync();

        var directory = new FulfillmentDirectory(
            db,
            new OpenFulfillmentUseCaseGuard(),
            new UnusedOrderReader(),
            new UnusedInventoryGateway(),
            new FulfillmentInstrumentation(),
            new FixedClock(),
            new NpgsqlIdGenerator());

        await directory.MarkProcessingAsync(fulfillmentId, actorUserId, CancellationToken.None);
        await directory.PackSelectionsAsync(
            fulfillmentId,
            actorUserId,
            [
                new AppModels.FulfillmentSelectionCommand(lineA, 2m),
                new AppModels.FulfillmentSelectionCommand(lineB, 1m),
            ],
            CancellationToken.None);

        var shipmentIds = new List<Guid>();
        if (splitIntoTwoParcels)
        {
            var first = await directory.CreateShipmentAsync(
                fulfillmentId,
                actorUserId,
                "پست",
                [new AppModels.ShipmentLineCommand(lineA, 1m)],
                CancellationToken.None,
                shippingMethodCode: "post",
                providerMetadataJson: PostMetadataJson,
                shippingGroupId: groupId);
            shipmentIds.Add(first.CreatedShipmentId);
            var second = await directory.CreateShipmentAsync(
                fulfillmentId,
                actorUserId,
                "پست",
                [
                    new AppModels.ShipmentLineCommand(lineA, 1m),
                    new AppModels.ShipmentLineCommand(lineB, 1m),
                ],
                CancellationToken.None,
                shippingMethodCode: "post",
                providerMetadataJson: PostMetadataJson,
                shippingGroupId: groupId);
            shipmentIds.Add(second.CreatedShipmentId);
        }
        else
        {
            var created = await directory.CreateShipmentAsync(
                fulfillmentId,
                actorUserId,
                "پست",
                [
                    new AppModels.ShipmentLineCommand(lineA, 2m),
                    new AppModels.ShipmentLineCommand(lineB, 1m),
                ],
                CancellationToken.None,
                shippingMethodCode: "post",
                providerMetadataJson: PostMetadataJson,
                shippingGroupId: groupId);
            shipmentIds.Add(created.CreatedShipmentId);
        }

        if (dispatch)
        {
            foreach (var shipmentId in shipmentIds)
            {
                await directory.AssignTrackingAsync(
                    fulfillmentId, shipmentId, actorUserId, $"TRK-TB258-{tag}-{shipmentId:N}", CancellationToken.None);
                await directory.DispatchShipmentAsync(
                    fulfillmentId, shipmentId, actorUserId, CancellationToken.None);
            }
        }

        if (returnedUsable)
        {
            await directory.RecordNonDeliveryAsync(
                fulfillmentId, shipmentIds[0], actorUserId, "refused", "TB258 fixture", CancellationToken.None);
            await directory.StartReturnAsync(fulfillmentId, shipmentIds[0], actorUserId, CancellationToken.None);
            await directory.RecordReturnReceiptAsync(
                fulfillmentId,
                shipmentIds[0],
                actorUserId,
                [new AppModels.ReturnReceiptLineCommand(lineA, 1m, ReturnReceiptCondition.Usable)],
                "TB258 fixture",
                CancellationToken.None);
        }

        return new Fixture(target, fulfillmentId, checkoutId, groupId, lineA, lineB, shipmentIds, actorUserId);
    }

    private static async Task<string?> RawShipmentStatusAsync(NpgsqlConnectionStringBuilder target, Guid shipmentId)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select status from fulfillment.shipments where shipment_id = @id";
        command.Parameters.AddWithValue("id", shipmentId);
        return await command.ExecuteScalarAsync() as string;
    }

    private static async Task<int> RawScalarAsync(
        NpgsqlConnectionStringBuilder target,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Holds both concurrent writers inside their save so the serializable allocation guard is real.</summary>
    private sealed class ConcurrentSaveBarrier : SaveChangesInterceptor
    {
        private readonly CountdownEvent _arrived = new(2);
        private readonly ConcurrentQueue<string> _states = new();

        public bool ObservedOverlap { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Arrive();
            await WaitAsync(cancellationToken);
            return result;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            Arrive();
            WaitAsync(CancellationToken.None).GetAwaiter().GetResult();
            return result;
        }

        private void Arrive()
        {
            _states.Enqueue(Environment.CurrentManagedThreadId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            try
            {
                _arrived.Signal();
            }
            catch (InvalidOperationException)
            {
                // more than two signals: the barrier already opened, no further waiting is needed
            }
        }

        private async Task WaitAsync(CancellationToken cancellationToken)
        {
            var opened = _arrived.Wait(TimeSpan.FromSeconds(20), cancellationToken);
            if (opened)
            {
                ObservedOverlap = true;
            }
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 11, 10, 9, 30, 0, TimeSpan.Zero);
    }

    private sealed class NpgsqlIdGenerator : IIdGenerator
    {
        public Guid NewId() => Guid.NewGuid();
    }

    private sealed class UnusedOrderReader : IOrderFulfillmentReader
    {
        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-258 fixture does not read orders.");

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-258 fixture does not read orders.");
    }

    private sealed class UnusedInventoryGateway : IFulfillmentInventoryGateway
    {
        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-258 fixture does not touch inventory.");

        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-258 fixture does not touch inventory.");
    }
}
