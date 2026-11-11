using Tooba.BuildingBlocks;
using Tooba.Order.Application.Admin.Detail;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Order.Domain;
using Xunit;

using ContractShipmentOperationStatus = Tooba.Fulfillment.Contracts.Operations.ShipmentOperationStatus;
using ContractShipmentSnapshot = Tooba.Fulfillment.Contracts.Operations.ShipmentSnapshot;
using ContractShipmentLineSnapshot = Tooba.Fulfillment.Contracts.Operations.ShipmentLineSnapshot;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T010: lifecycle sequence, exact selection, bulk homogeneity.</summary>
public sealed class AdminFulfillmentScopeSequenceTests
{
    [Fact]
    public void ReadyToProcess_cannot_pack_before_start_processing()
    {
        var line = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady(line, 2, now);
        var ex = Assert.Throws<ContractOperationException>(() => unit.PackSelections([(line, 1)], now));
        Assert.Equal("fulfillment.pack.requires_processing", ex.Code);
        Assert.Equal(0, unit.Items.Single().QuantityPacked);
        Assert.Equal(FulfillmentStatus.ReadyToFulfill, unit.Status);
    }

    [Fact]
    public void Process_one_line_does_not_process_siblings()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady([(l1, 1), (l2, 2)], now);
        unit.ProcessSelections([(l1, 1)], now);
        Assert.Equal(1, unit.Items.Single(x => x.OrderLineId == l1).QuantityProcessing);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l2).QuantityProcessing);
        Assert.Equal(FulfillmentStatus.Processing, unit.Status);
        var packOther = Assert.Throws<ContractOperationException>(() => unit.PackSelections([(l2, 1)], now));
        Assert.Equal("fulfillment.pack.requires_processing", packOther.Code);
        unit.UnprocessSelections([(l1, 1)], now);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l1).QuantityProcessing);
        Assert.Equal(FulfillmentStatus.ReadyToFulfill, unit.Status);
    }

    [Fact]
    public void StartProcessing_then_pack_is_allowed()
    {
        var line = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady(line, 2, now);
        unit.MarkProcessing(now);
        Assert.Equal(FulfillmentStatus.Processing, unit.Status);
        unit.PackSelections([(line, 2)], now);
        Assert.Equal(2, unit.Items.Single().QuantityPacked);
        Assert.Equal(FulfillmentStatus.Packed, unit.Status);
    }

    [Fact]
    public void Process_selected_changes_only_selected_line_and_can_unprocess()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var l3 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady([(l1, 1), (l2, 2), (l3, 3)], now);
        unit.ProcessSelections([(l1, 1)], now);
        Assert.Equal(1, unit.Items.Single(x => x.OrderLineId == l1).QuantityProcessing);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l2).QuantityProcessing);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l3).QuantityProcessing);
        Assert.Equal(FulfillmentStatus.Processing, unit.Status);
        var packOther = Assert.Throws<ContractOperationException>(() => unit.PackSelections([(l2, 1)], now));
        Assert.Equal("fulfillment.pack.requires_processing", packOther.Code);
        unit.PackSelections([(l1, 1)], now);
        Assert.Equal(1, unit.Items.Single(x => x.OrderLineId == l1).QuantityPacked);
        var blocked = Assert.Throws<ContractOperationException>(() => unit.UnprocessSelections([(l1, 1)], now));
        Assert.Contains("fulfillment.processing.release_invalid", blocked.Code, StringComparison.Ordinal);
        unit.UnpackSelections([(l1, 1)], now);
        unit.UnprocessSelections([(l1, 1)], now);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l1).QuantityProcessing);
        Assert.Equal(FulfillmentStatus.ReadyToFulfill, unit.Status);
    }

    [Fact]
    public void Pack_selected_changes_only_selected_line()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var l3 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady([(l1, 1), (l2, 2), (l3, 3)], now);
        unit.MarkProcessing(now);
        unit.PackSelections([(l1, 1)], now);
        Assert.Equal(1, unit.Items.Single(x => x.OrderLineId == l1).QuantityPacked);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l2).QuantityPacked);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l3).QuantityPacked);
        Assert.Equal(FulfillmentStatus.Processing, unit.Status);
    }

    [Fact]
    public void Partial_quantity_packs_exact_amount()
    {
        var line = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady(line, 3, now);
        unit.MarkProcessing(now);
        unit.PackSelections([(line, 1)], now);
        Assert.Equal(1, unit.Items.Single().QuantityPacked);
        Assert.Equal(FulfillmentStatus.Processing, unit.Status);
    }

    [Fact]
    public void Pack_all_packs_every_eligible_line()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady([(l1, 1), (l2, 2)], now);
        unit.MarkProcessing(now);
        unit.MarkPacked(now);
        Assert.Equal(1, unit.Items.Single(x => x.OrderLineId == l1).QuantityPacked);
        Assert.Equal(2, unit.Items.Single(x => x.OrderLineId == l2).QuantityPacked);
        Assert.Equal(FulfillmentStatus.Packed, unit.Status);
    }

    [Fact]
    public void Oversized_quantity_rejected()
    {
        var line = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady(line, 2, now);
        unit.MarkProcessing(now);
        var ex = Assert.Throws<ContractOperationException>(() => unit.PackSelections([(line, 3)], now));
        Assert.Equal(0, unit.Items.Single().QuantityPacked);
        Assert.Contains("fulfillment.pack.qty_exceeds", ex.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void Homogeneous_packable_selection_accepted()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var snapshot = Snapshot(
            FulfillmentStatus.Processing,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), l1, 1, 0, null, 0, 1),
             new FulfillmentItemSnapshot(Guid.NewGuid(), l2, 2, 0, null, 0, 2)]);
        Assert.True(AdminOrderOperationsPolicy.SelectionsAreHomogeneousPackable(snapshot.ToContracts(),
            [new AdminOrderLineSelection(l1, 1), new AdminOrderLineSelection(l2, 1)]));
    }

    [Fact]
    public void Mixed_packable_and_already_packed_is_incompatible()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var snapshot = Snapshot(
            FulfillmentStatus.Processing,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), l1, 1, 0, null, 1, 1),
             new FulfillmentItemSnapshot(Guid.NewGuid(), l2, 2, 0, null, 0, 2)]);
        Assert.False(AdminOrderOperationsPolicy.SelectionsAreHomogeneousPackable(snapshot.ToContracts(),
            [new AdminOrderLineSelection(l1, 1), new AdminOrderLineSelection(l2, 1)]));
    }

    [Fact]
    public void Fulfillment_error_codes_are_human_fa()
    {
        Assert.Equal("ابتدا پردازش را شروع کنید.", AdminOrderOperationsPolicy.FulfillmentOpToFa("fulfillment.pack.requires_processing"));
        Assert.Equal("این قلم هنوز در مرحله پردازش نیست.", AdminOrderOperationsPolicy.FulfillmentOpToFa("fulfillment.pack.not_processing"));
        Assert.Equal("این قلم هنوز بسته‌بندی نشده است.", AdminOrderOperationsPolicy.FulfillmentOpToFa("fulfillment.ship.not_packed"));
        Assert.Equal("تعداد انتخاب‌شده بیشتر از تعداد قابل عملیات است.", AdminOrderOperationsPolicy.FulfillmentOpToFa("fulfillment.selection.qty_exceeded"));
        Assert.Equal("ردیف‌های انتخاب‌شده برای این عملیات سازگار نیستند.", AdminOrderOperationsPolicy.FulfillmentOpToFa("fulfillment.bulk.incompatible"));
        Assert.Equal("عملیات گروهی روی فروشندگان متفاوت مجاز نیست.", AdminOrderOperationsPolicy.FulfillmentOpToFa("fulfillment.bulk.cross_seller"));
    }

    [Fact]
    public void Line_status_stays_mixed_after_partial_pack()
    {
        var ready = Snapshot(FulfillmentStatus.ReadyToFulfill, [new FulfillmentItemSnapshot(Guid.NewGuid(), Guid.NewGuid(), 2, 0, null, 0)]).ToContracts();
        Assert.Equal("ReadyToFulfill", AdminOrderDetailFulfillmentStatus.LineOperationalStatus(SellerOrderStatus.Paid, ready, 0, 2, 0));
        var processing = Snapshot(FulfillmentStatus.Processing, [new FulfillmentItemSnapshot(Guid.NewGuid(), Guid.NewGuid(), 3, 1, null, 0, 3)]).ToContracts();
        Assert.Equal("ReadyToFulfill", AdminOrderDetailFulfillmentStatus.LineOperationalStatus(SellerOrderStatus.Paid, processing, 0, 3, 0));
        Assert.Equal("Processing", AdminOrderDetailFulfillmentStatus.LineOperationalStatus(SellerOrderStatus.Paid, processing, 1, 3, 3));
        Assert.Equal("Packed", AdminOrderDetailFulfillmentStatus.LineOperationalStatus(SellerOrderStatus.Paid, processing, 3, 3, 3));
        var dispatched = Snapshot(FulfillmentStatus.Dispatched, [new FulfillmentItemSnapshot(Guid.NewGuid(), Guid.NewGuid(), 1.25m, 0.50m, null, 0.50m, 1.25m)]).ToContracts();
        Assert.Equal("PartialDispatched", AdminOrderDetailFulfillmentStatus.LineOperationalStatus(SellerOrderStatus.Paid, dispatched, 0.50m, 1.25m, 1.25m, 0.50m));
        Assert.Equal("Dispatched", AdminOrderDetailFulfillmentStatus.LineOperationalStatus(SellerOrderStatus.Paid, dispatched, 1.25m, 1.25m, 1.25m, 1.25m));
        Assert.Equal("PendingPayment", AdminOrderDetailFulfillmentStatus.LineOperationalStatus(SellerOrderStatus.PendingPayment, null, 0, 2));
        Assert.Equal("Cancelled", AdminOrderDetailFulfillmentStatus.LineOperationalStatus(SellerOrderStatus.Cancelled, null, 0, 2));
    }

    [Fact]
    public void Line_status_follows_the_parcels_that_actually_carry_it()
    {
        // TB-261: the screenshot defect — a fully shipped line whose only parcel came back at origin kept a
        // stale «in transit» state, so the operator saw a carrier return labelled as a customer return.
        var lineId = Guid.NewGuid();
        var fulfillment = Snapshot(
            FulfillmentStatus.InTransit,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), lineId, 1, 0, null, 1, 1)])
            .ToContracts() with
        {
            Shipments = [Shipment(ContractShipmentOperationStatus.Returned, lineId, 1)],
        };
        var parcels = AdminOrderDetailFulfillmentStatus.ParcelsOfLine(fulfillment, lineId);
        Assert.Single(parcels);
        Assert.Equal(
            "Returned",
            AdminOrderDetailFulfillmentStatus.LineOperationalStatus(SellerOrderStatus.Paid, fulfillment, 1, 1, 1, 1, parcels));

        // A sibling parcel still moving must not be overwritten by the returned one.
        var mixed = fulfillment with
        {
            Shipments =
            [
                Shipment(ContractShipmentOperationStatus.Returned, lineId, 1),
                Shipment(ContractShipmentOperationStatus.InTransit, lineId, 1),
            ],
        };
        Assert.Equal(
            "Mixed",
            AdminOrderDetailFulfillmentStatus.LineOperationalStatus(
                SellerOrderStatus.Paid,
                mixed,
                2,
                2,
                2,
                2,
                AdminOrderDetailFulfillmentStatus.ParcelsOfLine(mixed, lineId)));

        // Only a replacement being prepared is open, so that is the honest state of the line.
        var resending = fulfillment with
        {
            Shipments =
            [
                Shipment(ContractShipmentOperationStatus.Returned, lineId, 1),
                Shipment(ContractShipmentOperationStatus.Created, lineId, 1, resendOf: Guid.NewGuid()),
            ],
        };
        Assert.Equal(
            "Created",
            AdminOrderDetailFulfillmentStatus.LineOperationalStatus(
                SellerOrderStatus.Paid,
                resending,
                1,
                1,
                1,
                1,
                AdminOrderDetailFulfillmentStatus.ParcelsOfLine(resending, lineId)));

        // Not fully shipped: the exception never hides the quantity that still awaits dispatch.
        var partial = Snapshot(
            FulfillmentStatus.Dispatched,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), lineId, 2, 0, null, 1, 1)])
            .ToContracts() with
        {
            Shipments = [Shipment(ContractShipmentOperationStatus.Returned, lineId, 1)],
        };
        Assert.Equal(
            "PartialDispatched",
            AdminOrderDetailFulfillmentStatus.LineOperationalStatus(
                SellerOrderStatus.Paid,
                partial,
                0,
                2,
                1,
                1,
                AdminOrderDetailFulfillmentStatus.ParcelsOfLine(partial, lineId)));

        // Delivered plus returned is a genuinely mixed line, never one parcel's state.
        var deliveredAndReturned = fulfillment with
        {
            Shipments =
            [
                Shipment(ContractShipmentOperationStatus.Delivered, lineId, 1),
                Shipment(ContractShipmentOperationStatus.Returned, lineId, 1),
            ],
        };
        Assert.Equal(
            "Mixed",
            AdminOrderDetailFulfillmentStatus.LineOperationalStatus(
                SellerOrderStatus.Paid,
                deliveredAndReturned,
                2,
                2,
                2,
                2,
                AdminOrderDetailFulfillmentStatus.ParcelsOfLine(deliveredAndReturned, lineId)));

        // A cancelled parcel carries no goods, so it can never describe the line.
        var cancelledOnly = Guid.NewGuid();
        var cancelled = fulfillment with
        {
            Shipments = [Shipment(ContractShipmentOperationStatus.Cancelled, cancelledOnly, 1)],
        };
        Assert.Empty(AdminOrderDetailFulfillmentStatus.ParcelsOfLine(cancelled, cancelledOnly));
    }

    [Fact]
    public void Composer_source_keeps_pack_behind_processing()
    {
        var root = FindRepoRoot();
        var composer = OrderPartialSources.ReadAllAbsolute(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Admin", "Operations", "Services"), "AdminOrderOperationsOrchestrator*.cs");
        Assert.Contains("pack_selected", composer, StringComparison.Ordinal);
        Assert.Contains("QuantityProcessing", composer, StringComparison.Ordinal);
        Assert.Contains("unprocess", composer, StringComparison.Ordinal);
        Assert.Contains("fulfillment.pack.requires_processing", composer, StringComparison.Ordinal);
    }

    private static FulfillmentUnit CreateReady(Guid lineId, decimal qty, DateTimeOffset now) =>
        CreateReady([(lineId, qty)], now);

    private static FulfillmentUnit CreateReady(IReadOnlyList<(Guid LineId, decimal Qty)> lines, DateTimeOffset now) =>
        FulfillmentUnit.CreateFromPaidOrder(Guid.NewGuid(), () => Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "گیرنده",
            "+98912",
            "تهران",
            "تهران",
            "آدرس",
            "12345",
            "storefront-default",
            "ارسال",
            lines.Select(x => (x.LineId, x.Qty, (Guid?)Guid.NewGuid())).ToArray(),
            now);

    private static FulfillmentSnapshot Snapshot(
        FulfillmentStatus status,
        IReadOnlyList<FulfillmentItemSnapshot> items) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status,
            "n",
            "m",
            "p",
            "c",
            "a",
            "1",
            "post",
            "پست",
            items,
            []);

    private static ContractShipmentSnapshot Shipment(
        ContractShipmentOperationStatus status,
        Guid lineId,
        decimal quantity,
        Guid? resendOf = null) =>
        new(
            ShipmentId: Guid.NewGuid(),
            Status: status,
            CarrierDisplayName: "پست",
            TrackingReference: null,
            DispatchedAt: null,
            DeliveredAt: null,
            Items: [new ContractShipmentLineSnapshot(lineId, quantity)],
            ResendOfShipmentId: resendOf);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
