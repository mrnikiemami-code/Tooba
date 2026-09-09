using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Host.Admin;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T008 — صف کار ارسال و تحویل: فیلتر سریع، projection، bulk سازگاری.</summary>
public sealed class AdminFulfillmentWorkQueueTests
{
    [Theory]
    [InlineData(FulfillmentStatus.ReadyToFulfill, AdminFulfillmentQueueFilters.ReadyToProcess, true)]
    [InlineData(FulfillmentStatus.Processing, AdminFulfillmentQueueFilters.ReadyToPack, true)]
    [InlineData(FulfillmentStatus.Packed, AdminFulfillmentQueueFilters.ReadyToShip, true)]
    [InlineData(FulfillmentStatus.Dispatched, AdminFulfillmentQueueFilters.InTransit, true)]
    [InlineData(FulfillmentStatus.InTransit, AdminFulfillmentQueueFilters.InTransit, true)]
    [InlineData(FulfillmentStatus.Delivered, AdminFulfillmentQueueFilters.Delivered, true)]
    [InlineData(FulfillmentStatus.Failed, AdminFulfillmentQueueFilters.Problem, true)]
    [InlineData(FulfillmentStatus.ReadyToFulfill, AdminFulfillmentQueueFilters.Delivered, false)]
    public void Queue_status_only_filters_map_to_real_backend_states(
        FulfillmentStatus status,
        string filter,
        bool expected)
    {
        Assert.Equal(expected, AdminFulfillmentQueueFilters.MatchesStatusOnly(status, filter));
    }

    [Fact]
    public void Missing_tracking_detects_created_shipment_without_reference()
    {
        var shipments = new[]
        {
            new ShipmentSnapshot(Guid.NewGuid(), ShipmentStatus.Created, "پست", null, null, null, [], default),
        };
        Assert.True(AdminFulfillmentQueueFilters.HasMissingTracking(shipments));
        Assert.False(AdminFulfillmentQueueFilters.HasMissingTracking([
            new ShipmentSnapshot(Guid.NewGuid(), ShipmentStatus.Created, "پست", "TRK", null, null, [], default),
        ]));
    }

    [Fact]
    public void Project_action_codes_reuse_domain_status_rules()
    {
        var lineId = Guid.NewGuid();
        var ready = Snapshot(
            FulfillmentStatus.ReadyToFulfill,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), lineId, 2, 0, null, 0)],
            []);
        var codes = AdminFulfillmentQueueFilters.ProjectActionCodes(ready);
        Assert.Contains("mark_processing", codes);
        Assert.DoesNotContain("mark_packed", codes);
        Assert.DoesNotContain("dispatch_shipment", codes);
    }

    [Fact]
    public void Cancelled_fulfillment_projects_no_forward_actions()
    {
        var lineId = Guid.NewGuid();
        var cancelled = Snapshot(
            FulfillmentStatus.Cancelled,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), lineId, 1.25m, 0, null, 0, 0)],
            []);
        Assert.Empty(AdminFulfillmentQueueFilters.ProjectActionCodes(cancelled));
    }

    [Fact]
    public void Created_shipment_with_tracking_projects_correct_and_dispatch()
    {
        var lineId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var packed = Snapshot(
            FulfillmentStatus.Packed,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), lineId, 1.25m, 0, null, 1.25m, 1.25m)],
            [
                new ShipmentSnapshot(
                    shipmentId,
                    ShipmentStatus.Created,
                    "پست",
                    "TRK-1",
                    null,
                    null,
                    [new ShipmentLineSnapshot(lineId, 0.50m)]),
            ]);
        var codes = AdminFulfillmentQueueFilters.ProjectActionCodes(packed);
        Assert.Contains("correct_tracking", codes);
        Assert.Contains("dispatch_shipment", codes);
        Assert.Contains("cancel_shipment", codes);
        Assert.DoesNotContain("assign_tracking", codes);
    }

    [Fact]
    public void Partial_dispatch_with_remainder_projects_pack_and_needs_action()
    {
        var lineId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var partial = Snapshot(
            FulfillmentStatus.Dispatched,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), lineId, 1.25m, 0.50m, null, 0.50m, 1.25m)],
            [
                new ShipmentSnapshot(
                    shipmentId,
                    ShipmentStatus.Dispatched,
                    "پست",
                    "TRK-A",
                    DateTimeOffset.UtcNow,
                    null,
                    [new ShipmentLineSnapshot(lineId, 0.50m)]),
            ]);
        var codes = AdminFulfillmentQueueFilters.ProjectActionCodes(partial);
        Assert.Contains("mark_packed", codes);
        Assert.Contains("pack_selected", codes);
        Assert.True(AdminFulfillmentQueueFilters.MatchesNeedsAction(partial.Status, partial.Shipments, partial.Items));
        Assert.Equal(AdminFulfillmentQueueFilters.PartialDispatched, AdminFulfillmentQueueFilters.ComposeOperationalStatus(partial));
        Assert.DoesNotContain("cancel", codes);
    }

    [Fact]
    public void Full_dispatch_projects_terminal_dispatched_not_partial()
    {
        var lineId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var full = Snapshot(
            FulfillmentStatus.Dispatched,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), lineId, 1.25m, 1.25m, null, 1.25m, 1.25m)],
            [
                new ShipmentSnapshot(
                    shipmentId,
                    ShipmentStatus.Dispatched,
                    "پست",
                    "TRK-A",
                    DateTimeOffset.UtcNow,
                    null,
                    [new ShipmentLineSnapshot(lineId, 1.25m)]),
            ]);
        Assert.Equal("Dispatched", AdminFulfillmentQueueFilters.ComposeOperationalStatus(full));
        Assert.False(AdminFulfillmentQueueFilters.HasRemainingFulfillableQuantity(full.Items));
        Assert.False(AdminFulfillmentQueueFilters.MatchesNeedsAction(full.Status, full.Shipments, full.Items));
    }

    [Fact]
    public void Work_queue_query_engine_filters_order_reference_by_order_number()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host"));
        var engine = File.ReadAllText(Path.Combine(root, "Grid", "AdminFulfillmentWorkQueueQueryEngine.cs"));
        Assert.Contains("case \"orderReference\"", engine, StringComparison.Ordinal);
        Assert.Contains("x => x.OrderNumber", engine, StringComparison.Ordinal);
        Assert.Contains("OrderByOrderNumber", engine, StringComparison.Ordinal);
        Assert.Contains("\"فروشنده\"", engine, StringComparison.Ordinal);
        Assert.Contains("QuantityOrdered > i.QuantityShipped", engine, StringComparison.Ordinal);
        Assert.Contains("PartialDispatched", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("CheckoutId.ToString(\"N\")", engine, StringComparison.Ordinal);
    }

    [Fact]
    public void Composer_maps_stale_dispatch_english_to_human_fa()
    {
        var mapped = AdminOrderOperationsComposer.MapFulfillmentException("dispatch از این وضعیت مجاز نیست.");
        Assert.Equal("fulfillment.dispatch.invalid_state", mapped.Code);
        Assert.Equal("ارسال در وضعیت فعلی مرسوله مجاز نیست.", mapped.Fa);
        var tracking = AdminOrderOperationsComposer.MapFulfillmentException("dispatch بدون tracking مجاز نیست.");
        Assert.Equal("fulfillment.dispatch.tracking_required", tracking.Code);
        var voided = AdminOrderOperationsComposer.MapFulfillmentException("ابطال مرسوله پس از ارسال مجاز نیست.");
        Assert.Equal("fulfillment.shipment.void_after_dispatch", voided.Code);
        var packAfter = AdminOrderOperationsComposer.MapFulfillmentException("بسته‌بندی پس از تحویل کامل مجاز نیست.");
        Assert.Equal("fulfillment.pack.after_delivered", packAfter.Code);
        Assert.Equal("پس از تحویل کامل نمی‌توان بسته‌بندی کرد.", packAfter.Fa);
        var processAfter = AdminOrderOperationsComposer.MapFulfillmentException("پردازش پس از تحویل کامل مجاز نیست.");
        Assert.Equal("fulfillment.process.after_delivered", processAfter.Code);
    }

    [Fact]
    public void Bulk_compatible_requires_same_seller_and_shared_action()
    {
        var sellerA = Guid.NewGuid();
        var sellerB = Guid.NewGuid();
        var rowA = Row(sellerA, ["mark_processing", "mark_packed"]);
        var rowB = Row(sellerA, ["mark_processing"]);
        var rowC = Row(sellerB, ["mark_processing"]);

        Assert.True(AdminFulfillmentQueueFilters.AreBulkCompatible([rowA, rowB], "mark_processing"));
        Assert.False(AdminFulfillmentQueueFilters.AreBulkCompatible([rowA, rowB], "mark_packed"));
        Assert.False(AdminFulfillmentQueueFilters.AreBulkCompatible([rowA, rowC], "mark_processing"));
    }

    [Fact]
    public void Safe_bulk_codes_exclude_actions_needing_unique_input()
    {
        Assert.Contains("mark_processing", AdminFulfillmentQueueFilters.SafeBulkActionCodes);
        Assert.Contains("dispatch_shipment", AdminFulfillmentQueueFilters.SafeBulkActionCodes);
        Assert.DoesNotContain("assign_tracking", AdminFulfillmentQueueFilters.SafeBulkActionCodes);
        Assert.DoesNotContain("create_shipment", AdminFulfillmentQueueFilters.SafeBulkActionCodes);
    }

    [Fact]
    public void Work_queue_endpoints_and_composer_are_wired()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host"));
        var endpoints = File.ReadAllText(Path.Combine(root, "Fulfillment", "FulfillmentEndpoints.cs"));
        var program = File.ReadAllText(Path.Combine(root, "Program.cs"));
        Assert.Contains("/fulfillments/work-queue/query", endpoints, StringComparison.Ordinal);
        Assert.Contains("/fulfillments/work-queue/bulk", endpoints, StringComparison.Ordinal);
        Assert.Contains("AdminFulfillmentWorkQueueComposer", program, StringComparison.Ordinal);
        Assert.Contains("AdminFulfillmentWorkQueueQueryEngine", program, StringComparison.Ordinal);
    }

    private static FulfillmentSnapshot Snapshot(
        FulfillmentStatus status,
        IReadOnlyList<FulfillmentItemSnapshot> items,
        IReadOnlyList<ShipmentSnapshot> shipments) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status,
            "گیرنده",
            "0912",
            "تهران",
            "تهران",
            "آدرس",
            "1234567890",
            "post",
            "پست",
            items,
            shipments,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private static AdminFulfillmentWorkQueueRow Row(Guid sellerPartyId, IReadOnlyList<string> codes) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            sellerPartyId,
            "فروشنده",
            "ORD-1",
            "ReadyToFulfill",
            "گیرنده",
            "تهران",
            "post",
            "پست",
            1,
            1,
            0,
            0,
            null,
            "",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            codes);
}
