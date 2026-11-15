using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.WorkQueue.Models;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Queries;
using Tooba.Order.Contracts.Shipping;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-270-R2: the ONE queue projection over physical Fulfillment facts.
/// </summary>
/// <remarks>
/// <para>
/// A linked resend/replacement parcel is a NEW physical attempt: its timing may ONLY come from its own confirmed
/// Order plan. The original accepted group plan belongs to the first parcel and is never lent, so an unplanned
/// attempt stays truthfully «موعد ارسال مشخص نشده» and offers planning instead of dispatch.
/// </para>
/// <para>
/// This is a projection-level fact (no database): the queue engine's own wiring of the same reader is exercised by
/// the existing relational queue tests. Nothing here writes, reserves stock or creates a parcel.
/// </para>
/// </remarks>
public sealed class AdminFulfillmentOwnPlanProjection270R2Tests
{
    private static readonly DateOnly Today = new(2026, 11, 15);
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 6, 0, 0, TimeSpan.Zero);

    private readonly ITestOutputHelper _output;

    public AdminFulfillmentOwnPlanProjection270R2Tests(ITestOutputHelper output) => _output = output;

    [Fact]
    [Trait("Case", "A-own-plan-versus-original-group-plan")]
    public void A_linked_attempt_reads_only_its_own_confirmed_plan_and_an_unplanned_one_stays_unspecified()
    {
        var unit = Unit(
            [
                Parcel("tb270r2-first", Today, tracking: "TRK-FIRST"),
                Parcel("tb270r2-resend-unplanned", Today, resendOf: "tb270r2-first", tracking: "TRK-UNPLANNED"),
                Parcel("tb270r2-resend-planned", Today, resendOf: "tb270r2-first", tracking: "TRK-PLANNED"),
            ]);

        var ownPlan = OwnPlan("tb270r2-resend-planned", Today.AddDays(3), customerDate: Today.AddDays(5));
        var rows = AdminFulfillmentWorkUnitProjection.Build(new AdminFulfillmentWorkUnitContext(
            [unit],
            new Dictionary<(Guid, Guid), IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>>
            {
                [(unit.SellerOrderId, GroupId)] = [GroupPlan(Today, customerDate: Today.AddDays(1))],
            },
            new Dictionary<Guid, OrderShipmentDispatchPlanFact> { [ownPlan.ShipmentId] = ownPlan },
            new HashSet<Guid> { ownPlan.ShipmentId, Guid.Parse(Id("tb270r2-resend-unplanned")) },
            new Dictionary<Guid, AdminFulfillmentPackageWorkFacts>(),
            new Dictionary<Guid, Guid>(),
            new Dictionary<(Guid, Guid), decimal>(),
            new Dictionary<Guid, string>(),
            new Dictionary<Guid, string>(),
            Now));

        var first = RowFor(rows, "tb270r2-first");
        var unplanned = RowFor(rows, "tb270r2-resend-unplanned");
        var planned = RowFor(rows, "tb270r2-resend-planned");

        // First dispatch keeps the accepted group plan (unchanged behaviour).
        Assert.Equal(Today, first.PlannedHandoffDate);
        Assert.Equal(Today.AddDays(1), first.CustomerReceivingDate);
        Assert.Equal(AdminFulfillmentDispatchBucketNames.Today, first.DispatchBucket);
        Assert.DoesNotContain("plan_shipment_dispatch", first.AvailableActionCodes);
        Assert.Contains("dispatch_shipment", first.AvailableActionCodes);

        // The unplanned attempt: no borrowed promise, no invented date, planning is its ONLY forward step.
        // Its shipping group is the physical service it travels under (that is how the resend was created), so it is
        // NOT a timing source: every plan-derived field stays empty.
        Assert.Equal(GroupId, unplanned.TargetShippingGroupId);
        Assert.Null(unplanned.PlannedHandoffDate);
        Assert.Null(unplanned.PlannedHandoffWindow);
        Assert.Null(unplanned.DispatchDeadlineUtc);
        Assert.Null(unplanned.CustomerReceivingDate);
        Assert.Equal(AdminFulfillmentDispatchBucketNames.Unspecified, unplanned.DispatchBucket);
        Assert.Contains("plan_shipment_dispatch", unplanned.AvailableActionCodes);
        Assert.DoesNotContain("dispatch_shipment", unplanned.AvailableActionCodes);

        // The planned attempt: exactly its own stored promise, never the original parcel's.
        Assert.Equal(Today.AddDays(3), planned.PlannedHandoffDate);
        Assert.Equal(Today.AddDays(5), planned.CustomerReceivingDate);
        Assert.Equal(AdminFulfillmentDispatchBucketNames.Upcoming, planned.DispatchBucket);
        Assert.DoesNotContain("plan_shipment_dispatch", planned.AvailableActionCodes);
        Assert.Contains("dispatch_shipment", planned.AvailableActionCodes);
        Assert.NotEqual(first.CustomerReceivingDate, planned.CustomerReceivingDate);

        _output.WriteLine(
            $"A first={first.DispatchBucket}/{first.PlannedHandoffDate:yyyy-MM-dd} "
            + $"unplanned={unplanned.DispatchBucket}/{(unplanned.PlannedHandoffDate is null ? "null" : "date")} "
            + $"actions=[{string.Join(",", unplanned.AvailableActionCodes)}] "
            + $"planned={planned.DispatchBucket}/{planned.PlannedHandoffDate:yyyy-MM-dd} "
            + $"customer={planned.CustomerReceivingDate:yyyy-MM-dd}");
    }

    [Fact]
    [Trait("Case", "B-no-sibling-lending")]
    public void B_a_package_member_never_lends_its_group_plan_to_an_unplanned_linked_attempt()
    {
        var first = Parcel("tb270r2-pkg-first", Today, tracking: "TRK-PKG-1");
        var linked = Parcel("tb270r2-pkg-linked", Today, resendOf: "tb270r2-pkg-first", tracking: "TRK-PKG-2");
        var unit = Unit([first, linked]);

        var groupA = GroupId;
        var groupB = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var package = new AdminFulfillmentPackageWorkFacts(
            packageId,
            "MP-270R2",
            unit.CheckoutId,
            ConsolidatedPackageStatus.Created,
            "post",
            "پیشتاز",
            null,
            Now,
            Now,
            null,
            [
                new AdminFulfillmentPackageMemberFacts(
                    Guid.Parse(Id("tb270r2-pkg-first")),
                    unit.FulfillmentId,
                    unit.SellerPartyId,
                    unit.SellerOrderId,
                    groupA,
                    ShipmentStatus.Created,
                    1,
                    1m),
                new AdminFulfillmentPackageMemberFacts(
                    Guid.Parse(Id("tb270r2-pkg-linked")),
                    unit.FulfillmentId,
                    unit.SellerPartyId,
                    unit.SellerOrderId,
                    groupB,
                    ShipmentStatus.Created,
                    1,
                    1m),
            ]);

        var rows = AdminFulfillmentWorkUnitProjection.Build(new AdminFulfillmentWorkUnitContext(
            [unit],
            new Dictionary<(Guid, Guid), IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>>
            {
                [(unit.SellerOrderId, groupA)] = [GroupPlan(Today, customerDate: Today.AddDays(1), groupId: groupA)],
                // A plan that exists for the linked member's own group: it still belongs to the ORIGINAL parcel.
                [(unit.SellerOrderId, groupB)] = [GroupPlan(Today.AddDays(2), customerDate: Today.AddDays(4), groupId: groupB)],
            },
            new Dictionary<Guid, OrderShipmentDispatchPlanFact>(),
            new HashSet<Guid> { Guid.Parse(Id("tb270r2-pkg-linked")) },
            new Dictionary<Guid, AdminFulfillmentPackageWorkFacts> { [packageId] = package },
            new Dictionary<Guid, Guid>
            {
                [Guid.Parse(Id("tb270r2-pkg-first"))] = packageId,
                [Guid.Parse(Id("tb270r2-pkg-linked"))] = packageId,
            },
            new Dictionary<(Guid, Guid), decimal>(),
            new Dictionary<Guid, string>(),
            new Dictionary<Guid, string>(),
            Now));

        var packageRow = rows.Single(x => x.TargetPackageId == packageId);
        // Only the ordinary member's group plan is visible: the linked member contributes NOTHING without its own plan.
        Assert.Equal(1, packageRow.HandoffPlanVariants);
        Assert.Equal(Today, packageRow.PlannedHandoffDate);
        Assert.Equal(Today.AddDays(1), packageRow.CustomerReceivingDate);

        _output.WriteLine(
            $"B package={packageId} variants={packageRow.HandoffPlanVariants} "
            + $"handoff={packageRow.PlannedHandoffDate:yyyy-MM-dd} customer={packageRow.CustomerReceivingDate:yyyy-MM-dd}");
    }

    [Fact]
    [Trait("Case", "C-completed-history-has-no-planning-action")]
    public void C_a_finished_linked_attempt_gains_no_planning_action_from_history()
    {
        var dispatched = Parcel(
            "tb270r2-resend-done",
            Today,
            resendOf: "tb270r2-first",
            tracking: "TRK-DONE",
            status: ShipmentStatus.Dispatched);
        var unit = Unit([dispatched]);
        var ownPlan = OwnPlan("tb270r2-resend-done", Today, customerDate: Today.AddDays(2));

        var rows = AdminFulfillmentWorkUnitProjection.Build(new AdminFulfillmentWorkUnitContext(
            [unit],
            new Dictionary<(Guid, Guid), IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>>(),
            new Dictionary<Guid, OrderShipmentDispatchPlanFact> { [ownPlan.ShipmentId] = ownPlan },
            new HashSet<Guid> { ownPlan.ShipmentId },
            new Dictionary<Guid, AdminFulfillmentPackageWorkFacts>(),
            new Dictionary<Guid, Guid>(),
            new Dictionary<(Guid, Guid), decimal>(),
            new Dictionary<Guid, string>(),
            new Dictionary<Guid, string>(),
            Now));

        var row = RowFor(rows, "tb270r2-resend-done");
        Assert.False(row.PendingDispatch);
        Assert.DoesNotContain("plan_shipment_dispatch", row.AvailableActionCodes);

        _output.WriteLine(
            $"C done pending={row.PendingDispatch} actions=[{string.Join(",", row.AvailableActionCodes)}]");
    }

    // ------------------------------------------------------------------ helpers

    private static readonly Guid GroupId = Guid.NewGuid();

    private static string Id(string seed) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(seed))[..16]);

    private static Guid ParcelId(string seed) => Guid.Parse(Id(seed));

    private static AdminFulfillmentWorkQueueRow RowFor(
        IReadOnlyList<AdminFulfillmentWorkQueueRow> rows,
        string seed) =>
        rows.Single(x => x.TargetShipmentId == ParcelId(seed));

    private static FulfillmentSnapshot Unit(IReadOnlyList<ShipmentSnapshot> shipments) => new(
        FulfillmentId,
        SellerOrderId,
        CheckoutId,
        SellerPartyId,
        FulfillmentStatus.Packed,
        "گیرنده",
        "09120000000",
        "تهران",
        "تهران",
        "خیابان",
        "1234567890",
        "post",
        "پیشتاز",
        [
            new FulfillmentItemSnapshot(
                Guid.NewGuid(),
                OrderLineId,
                1m,
                0m,
                null,
                QuantityPacked: 1m,
                ShippingGroupId: GroupId),
        ],
        shipments,
        Now,
        Now);

    private static readonly Guid SellerOrderId = Guid.NewGuid();
    private static readonly Guid CheckoutId = Guid.NewGuid();
    private static readonly Guid SellerPartyId = Guid.NewGuid();
    private static readonly Guid FulfillmentId = Guid.NewGuid();
    private static readonly Guid OrderLineId = Guid.NewGuid();

    private static ShipmentSnapshot Parcel(
        string seed,
        DateOnly createdDay,
        string? resendOf = null,
        string? tracking = null,
        ShipmentStatus status = ShipmentStatus.Created) =>
        new(
            ParcelId(seed),
            status,
            "پست",
            tracking,
            null,
            null,
            [new ShipmentLineSnapshot(OrderLineId, 1m)],
            CreatedAt: new DateTimeOffset(createdDay.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero),
            ShippingMethodCode: "post",
            ShippingMethodLabel: "پیشتاز",
            ShippingGroupId: GroupId,
            ResendOfShipmentId: resendOf is null ? null : ParcelId(resendOf));

    private static OrderAcceptedGroupDispatchPlanFact GroupPlan(
        DateOnly handoffDate,
        DateOnly customerDate,
        Guid? groupId = null) =>
        new(
            SellerOrderId,
            groupId ?? GroupId,
            CheckoutId,
            SellerPartyId,
            "post",
            "پیشتاز",
            handoffDate,
            new TimeOnly(0, 0).Ticks,
            new TimeOnly(23, 59, 59).Ticks,
            "Asia/Tehran",
            1,
            1,
            customerDate,
            "09:00-17:00",
            new DateTimeOffset(handoffDate.ToDateTime(new TimeOnly(0, 0)), TimeSpan.Zero).AddDays(-1),
            new DateTimeOffset(handoffDate.ToDateTime(new TimeOnly(4, 0)), TimeSpan.Zero),
            new DateTimeOffset(handoffDate.ToDateTime(new TimeOnly(19, 0)), TimeSpan.Zero));

    private static OrderShipmentDispatchPlanFact OwnPlan(string seed, DateOnly handoffDate, DateOnly customerDate) =>
        new(
            Guid.NewGuid(),
            ParcelId(seed),
            CheckoutId,
            SellerOrderId,
            FulfillmentId,
            GroupId,
            OrderShipmentDispatchPlanKinds.Resend,
            ParcelId("tb270r2-first"),
            null,
            null,
            OrderShipmentDispatchPlanBindingRules.BuildLineFingerprint([(OrderLineId, 1m)]),
            Guid.NewGuid(),
            Guid.NewGuid(),
            7,
            4,
            9,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "post",
            "پیشتاز",
            "post",
            null,
            customerDate,
            new TimeOnly(9, 0).Ticks,
            new TimeOnly(10, 0).Ticks,
            "Asia/Tehran",
            handoffDate,
            new TimeOnly(8, 0).Ticks,
            new TimeOnly(20, 0).Ticks,
            "Asia/Tehran",
            new DateTimeOffset(handoffDate.ToDateTime(new TimeOnly(2, 0)), TimeSpan.Zero),
            new DateTimeOffset(handoffDate.ToDateTime(new TimeOnly(5, 0)), TimeSpan.Zero),
            new DateTimeOffset(handoffDate.ToDateTime(new TimeOnly(16, 0)), TimeSpan.Zero),
            120,
            60,
            2,
            3,
            OrderShipmentReadinessSources.ReturnReceipt,
            new DateTimeOffset(handoffDate.AddDays(-1).ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero),
            "tb270r2-occurrence-" + seed,
            Guid.NewGuid(),
            Now);
}


