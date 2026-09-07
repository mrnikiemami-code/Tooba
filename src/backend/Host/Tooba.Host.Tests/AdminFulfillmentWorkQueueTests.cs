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
        Assert.Contains("mark_packed", codes);
        Assert.DoesNotContain("dispatch_shipment", codes);
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
