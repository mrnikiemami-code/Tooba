using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.WorkQueue.Models;
using Tooba.Fulfillment.Domain.ValueObjects;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-260-R1: the queue row identity is the PHYSICAL work-unit key, and the projected next step is the physical
/// operation of that unit — not a generic aggregate. These pin the pure rules the real query projection relies on.
/// </summary>
public sealed class AdminFulfillmentWorkUnitIdentity260R1Tests
{
    [Fact]
    public void Work_unit_key_is_discriminated_so_two_parcels_of_one_order_never_collide()
    {
        var id = Guid.Parse("018f3c1a-2b3c-7d4e-8f01-23456789abcd");

        var group = AdminFulfillmentWorkUnitKinds.Key(AdminFulfillmentWorkUnitKinds.Group, id);
        var parcel = AdminFulfillmentWorkUnitKinds.Key(AdminFulfillmentWorkUnitKinds.Parcel, id);
        var package = AdminFulfillmentWorkUnitKinds.Key(AdminFulfillmentWorkUnitKinds.Package, id);
        var fulfillment = AdminFulfillmentWorkUnitKinds.Key(AdminFulfillmentWorkUnitKinds.Fulfillment, id);

        Assert.Equal($"group:{id:D}", group);
        Assert.Equal($"parcel:{id:D}", parcel);
        Assert.Equal($"package:{id:D}", package);
        Assert.Equal($"fulfillment:{id:D}", fulfillment);
        Assert.Equal(4, new[] { group, parcel, package, fulfillment }.Distinct(StringComparer.Ordinal).Count());
        // Stable across calls: paging and selection must never see a different key for the same unit.
        Assert.Equal(parcel, AdminFulfillmentWorkUnitKinds.Key("Parcel", id));
    }

    [Fact]
    public void A_created_parcel_with_tracking_offers_the_carrier_handoff_not_a_tracking_correction()
    {
        var codes = AdminFulfillmentQueueFilters.ProjectParcelActionCodes(
            ShipmentStatus.Created,
            hasTracking: true,
            hasDispatchedAt: false,
            hasResendableCapacity: false);

        Assert.Contains("dispatch_shipment", codes);
        Assert.Equal("dispatch_shipment", AdminFulfillmentNextStep.Resolve(codes).Code);
        Assert.Equal("ارسال به حامل", AdminFulfillmentNextStep.Resolve(codes).LabelFa);

        var untracked = AdminFulfillmentQueueFilters.ProjectParcelActionCodes(
            ShipmentStatus.Created,
            hasTracking: false,
            hasDispatchedAt: false,
            hasResendableCapacity: false);
        Assert.Equal("assign_tracking", AdminFulfillmentNextStep.Resolve(untracked).Code);
    }

    [Fact]
    public void A_parcel_exposes_only_the_exit_of_its_own_lifecycle()
    {
        Assert.Equal(
            ["deliver_shipment", "record_non_delivery"],
            AdminFulfillmentQueueFilters.ProjectParcelActionCodes(ShipmentStatus.InTransit, true, true, false));
        Assert.Equal(
            ["start_return"],
            AdminFulfillmentQueueFilters.ProjectParcelActionCodes(ShipmentStatus.NonDelivered, true, true, false));
        Assert.Equal(
            ["record_return_receipt"],
            AdminFulfillmentQueueFilters.ProjectParcelActionCodes(ShipmentStatus.Returning, true, true, false));
        // A returned parcel without physically received usable capacity offers no resend it would refuse.
        Assert.Empty(AdminFulfillmentQueueFilters.ProjectParcelActionCodes(ShipmentStatus.Returned, true, true, false));
        Assert.Equal(
            ["resend_shipment"],
            AdminFulfillmentQueueFilters.ProjectParcelActionCodes(ShipmentStatus.Returned, true, true, true));
        Assert.Empty(AdminFulfillmentQueueFilters.ProjectParcelActionCodes(ShipmentStatus.Delivered, true, true, false));
    }

    [Fact]
    public void A_package_row_owns_package_commands_and_never_a_locked_member_command()
    {
        var created = AdminFulfillmentQueueFilters.ProjectPackageActionCodes(
            ConsolidatedPackageStatus.Created,
            hasTracking: false);
        Assert.Contains("assign_consolidated_package_tracking", created);
        Assert.Equal("assign_consolidated_package_tracking", AdminFulfillmentNextStep.Resolve(created).Code);
        Assert.DoesNotContain(created, code => code.Contains("shipment", StringComparison.Ordinal));

        var dispatched = AdminFulfillmentQueueFilters.ProjectPackageActionCodes(
            ConsolidatedPackageStatus.Dispatched,
            hasTracking: true);
        Assert.Equal(["deliver_consolidated_package"], dispatched);
        Assert.Equal("deliver_consolidated_package", AdminFulfillmentNextStep.Resolve(dispatched).Code);
    }

    [Fact]
    public void Group_scope_work_is_its_own_lines_and_open_first_allocations_only()
    {
        var line = new FulfillmentItemSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            QuantityOrdered: 2m,
            QuantityShipped: 1m,
            ReservationId: null,
            QuantityPacked: 2m,
            QuantityProcessing: 2m,
            ShippingGroupId: Guid.NewGuid());
        var group = new[] { line };

        var noParcels = Array.Empty<ShipmentSnapshot>();
        Assert.True(AdminFulfillmentQueueFilters.HasGroupScopeWork(group, noParcels));
        Assert.Equal(1m, AdminFulfillmentQueueFilters.ScopeRemainingPackedQuantity(line, noParcels));

        var openParcel = new[]
        {
            Parcel(ShipmentStatus.Created, line.OrderLineId, 1m),
        };
        // The open Created parcel already covers the packed remainder: no phantom second allocation.
        Assert.False(AdminFulfillmentQueueFilters.HasGroupScopeWork(group, openParcel));
        Assert.Equal(0m, AdminFulfillmentQueueFilters.ScopeRemainingPackedQuantity(line, openParcel));

        // A resend parcel spends RETURNED capacity, never the packed remainder.
        var resend = new[]
        {
            Parcel(ShipmentStatus.Created, line.OrderLineId, 1m, resendOf: Guid.NewGuid()),
        };
        Assert.True(AdminFulfillmentQueueFilters.HasGroupScopeWork(group, resend));
        Assert.Equal(1m, AdminFulfillmentQueueFilters.ScopeRemainingPackedQuantity(line, resend));
    }

    private static ShipmentSnapshot Parcel(
        ShipmentStatus status,
        Guid orderLineId,
        decimal quantity,
        Guid? resendOf = null) =>
        new(
            Guid.NewGuid(),
            status,
            "پست",
            null,
            null,
            null,
            [new ShipmentLineSnapshot(orderLineId, quantity)],
            DateTimeOffset.UtcNow,
            "post",
            "پیشتاز",
            ResendOfShipmentId: resendOf);
}
