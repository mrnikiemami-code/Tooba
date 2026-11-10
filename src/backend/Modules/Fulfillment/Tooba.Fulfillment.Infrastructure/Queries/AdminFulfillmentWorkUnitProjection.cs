using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.WorkQueue.Models;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Order.Contracts.Shipping;

namespace Tooba.Fulfillment.Infrastructure.Queries;

/// <summary>TB-260-R1: one active physical common package and its complete (locked) member set.</summary>
internal sealed record AdminFulfillmentPackageWorkFacts(
    Guid ConsolidatedPackageId,
    string PackageNumber,
    Guid CheckoutId,
    ConsolidatedPackageStatus Status,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    string? TrackingReference,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DispatchedAt,
    IReadOnlyList<AdminFulfillmentPackageMemberFacts> Members);

/// <summary>TB-260-R1: one parcel locked inside an active common package.</summary>
internal sealed record AdminFulfillmentPackageMemberFacts(
    Guid ShipmentId,
    Guid FulfillmentId,
    Guid SellerPartyId,
    Guid SellerOrderId,
    Guid? ShippingGroupId,
    ShipmentStatus Status,
    int LineCount,
    decimal Quantity);

/// <summary>TB-260-R1: every fact the work-unit projection needs, already loaded in batch by the query engine.</summary>
internal sealed record AdminFulfillmentWorkUnitContext(
    IReadOnlyList<FulfillmentSnapshot> Units,
    IReadOnlyDictionary<(Guid SellerOrderId, Guid ShippingGroupId), IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>> PlansByGroup,
    IReadOnlyDictionary<Guid, AdminFulfillmentPackageWorkFacts> Packages,
    IReadOnlyDictionary<Guid, Guid> PackageIdByShipmentId,
    IReadOnlyDictionary<(Guid ShipmentId, Guid OrderLineId), decimal> ResendableBySourceLine,
    IReadOnlyDictionary<Guid, string> SellerNameByPartyId,
    IReadOnlyDictionary<Guid, string> OrderNumberBySellerOrderId,
    DateTimeOffset NowUtc);

/// <summary>
/// TB-260-R1 — the ONE projection from physical Fulfillment facts to queue rows.
/// </summary>
/// <remarks>
/// <para>
/// A row is <b>one physical work unit</b>, never a seller summary. Before any parcel exists the unit is the exact
/// accepted group that still owns work (its own quantities, its own accepted plan); an existing parcel is its own
/// row with its own lifecycle and its own group's plan; one active common package is ONE parent row across its
/// locked members, so its child parcels never appear as duplicated action rows or inflated counts. A seller order
/// with nothing physical left keeps exactly one history row so delivered/problem filters stay reachable.
/// </para>
/// <para>
/// No plan is averaged, borrowed or recomputed: a work scope only ever sees the durable plan of the accepted group
/// it physically belongs to. A dispatched sibling therefore can never make a later Created parcel look due, and a
/// group with no accepted plan stays explicitly «موعد ارسال مشخص نشده» for itself.
/// </para>
/// </remarks>
internal static class AdminFulfillmentWorkUnitProjection
{
    private const string NoActionBlockerFa =
        "اقدام مجاز فعلی برای این ردیف وجود ندارد؛ وضعیت پرداخت/مالکیت/گروه را بررسی کنید.";

    private const string ReturnedWithoutCapacityBlockerFa =
        "این مرسوله در مبدأ دریافت شد؛ ارسال مجدد به ظرفیت قابل‌ارسال بازگشتی نیاز دارد.";

    internal static List<AdminFulfillmentWorkQueueRow> Build(AdminFulfillmentWorkUnitContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var rows = new List<AdminFulfillmentWorkQueueRow>();

        // 1) ONE row per active physical common package, in package-number order so paging is deterministic.
        var packageRowIds = new HashSet<Guid>();
        foreach (var package in context.Packages.Values.OrderBy(x => x.PackageNumber, StringComparer.Ordinal))
        {
            var row = BuildPackageRow(context, package);
            if (row is null)
            {
                continue;
            }

            rows.Add(row);
            packageRowIds.Add(package.ConsolidatedPackageId);
        }

        // A parcel is owned by the package row ONLY when that row actually exists; a delivered package leaves its
        // members to the ordinary unit/parcel projection instead of hiding them.
        var coveredShipments = context.PackageIdByShipmentId
            .Where(pair => packageRowIds.Contains(pair.Value))
            .Select(pair => pair.Key)
            .ToHashSet();

        // 2) ONE row per physical work unit of every seller unit in scope.
        foreach (var unit in context.Units)
        {
            rows.AddRange(BuildUnitRows(context, unit, coveredShipments));
        }

        return rows;
    }

    private static IEnumerable<AdminFulfillmentWorkQueueRow> BuildUnitRows(
        AdminFulfillmentWorkUnitContext context,
        FulfillmentSnapshot unit,
        IReadOnlySet<Guid> coveredShipments)
    {
        var produced = new List<AdminFulfillmentWorkQueueRow>();
        var liveShipments = unit.Shipments
            .Where(x => x.Status != ShipmentStatus.Cancelled)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.ShipmentId)
            .ToList();

        foreach (var scope in unit.Items.GroupBy(x => x.ShippingGroupId).OrderBy(g => g.Key))
        {
            var groupItems = scope.ToList();
            var groupShipments = scope.Key is { } groupId
                ? liveShipments.Where(x => x.ShippingGroupId == groupId).ToList()
                : liveShipments.Where(x => x.ShippingGroupId is null).ToList();
            if (!AdminFulfillmentQueueFilters.HasGroupScopeWork(groupItems, groupShipments))
            {
                continue;
            }

            produced.Add(BuildGroupRow(context, unit, scope.Key, groupItems, groupShipments));
        }

        foreach (var shipment in liveShipments)
        {
            // The package row is the single action owner of a locked member; a finished parcel owns no work.
            if (coveredShipments.Contains(shipment.ShipmentId) || shipment.Status == ShipmentStatus.Delivered)
            {
                continue;
            }

            produced.Add(BuildParcelRow(context, unit, shipment));
        }

        if (produced.Count == 0 && !unit.Shipments.Any(x => coveredShipments.Contains(x.ShipmentId)))
        {
            produced.Add(BuildHistoryRow(context, unit));
        }

        return produced;
    }

    private static AdminFulfillmentWorkQueueRow BuildGroupRow(
        AdminFulfillmentWorkUnitContext context,
        FulfillmentSnapshot unit,
        Guid? shippingGroupId,
        IReadOnlyList<FulfillmentItemSnapshot> groupItems,
        IReadOnlyList<ShipmentSnapshot> groupShipments)
    {
        var plans = PlansOf(context, unit.SellerOrderId, shippingGroupId);
        // A group row only exists because the group itself still owns preparation work, so it is pending by
        // construction — and it is classified with ITS OWN accepted plan, never the seller order's earliest one.
        var timing = AdminFulfillmentDispatchTimingAuthority.Classify(true, false, plans, context.NowUtc);
        var remaining = groupItems.Sum(item =>
            AdminFulfillmentQueueFilters.ScopeRemainingPackedQuantity(item, groupShipments));
        var codes = GroupActionCodes(groupItems, remaining);
        var tracking = groupShipments
            .Select(x => x.TrackingReference)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToArray();

        return CreateRow(
            context,
            unit,
            AdminFulfillmentWorkUnitKinds.Group,
            shippingGroupId ?? unit.FulfillmentId,
            shippingGroupId is null ? "Ungrouped" : "Group",
            timing,
            plans,
            codes,
            explicitBlocker: null,
            sellerDisplayName: SellerNameOf(context, unit.SellerPartyId),
            orderReference: OrderReferenceOf(context, unit.SellerOrderId),
            methodCode: unit.ShippingMethodCode,
            methodLabel: ShippingMethodRegistry.ResolveLabel(
                unit.ShippingMethodCode,
                string.IsNullOrWhiteSpace(unit.ShippingMethodLabel) ? null : unit.ShippingMethodLabel),
            shipmentCount: groupShipments.Count,
            primaryShipmentId: null,
            trackingSummary: tracking.Length == 0 ? string.Empty : string.Join(" · ", tracking),
            scopeLineCount: groupItems.Count,
            scopeQuantity: groupItems.Sum(x => x.QuantityPacked),
            scopeRemaining: remaining,
            targetShippingGroupId: shippingGroupId,
            targetShipmentId: null,
            targetPackageId: null,
            targetPackageNumber: null,
            pending: true);
    }

    private static AdminFulfillmentWorkQueueRow BuildParcelRow(
        AdminFulfillmentWorkUnitContext context,
        FulfillmentSnapshot unit,
        ShipmentSnapshot shipment)
    {
        // A Created resend parcel is funded by returned capacity and carries no accepted first-dispatch plan, so
        // its timing is truthfully unspecified instead of inheriting the shipped parcel's old deadline.
        var plans = shipment.ResendOfShipmentId is null
            ? PlansOf(context, unit.SellerOrderId, shipment.ShippingGroupId)
            : [];
        var pending = shipment.Status == ShipmentStatus.Created;
        var inTransit = shipment.Status is ShipmentStatus.Dispatched
            or ShipmentStatus.InTransit
            or ShipmentStatus.NonDelivered
            or ShipmentStatus.Returning
            or ShipmentStatus.Returned;
        var timing = AdminFulfillmentDispatchTimingAuthority.Classify(pending, inTransit, plans, context.NowUtc);
        var resendable = shipment.Status == ShipmentStatus.Returned
            && shipment.Items.Any(line => ResendableOf(context, shipment.ShipmentId, line.OrderLineId) > 0m);
        var codes = AdminFulfillmentQueueFilters.ProjectParcelActionCodes(
            shipment.Status,
            !string.IsNullOrWhiteSpace(shipment.TrackingReference),
            shipment.DispatchedAt is not null,
            resendable);
        var methodCode = string.IsNullOrWhiteSpace(shipment.ShippingMethodCode)
            ? unit.ShippingMethodCode
            : shipment.ShippingMethodCode;

        return CreateRow(
            context,
            unit,
            AdminFulfillmentWorkUnitKinds.Parcel,
            shipment.ShipmentId,
            shipment.Status.ToString(),
            timing,
            plans,
            codes,
            explicitBlocker: shipment.Status == ShipmentStatus.Returned && !resendable
                ? ReturnedWithoutCapacityBlockerFa
                : null,
            sellerDisplayName: SellerNameOf(context, unit.SellerPartyId),
            orderReference: OrderReferenceOf(context, unit.SellerOrderId),
            methodCode: methodCode,
            methodLabel: ShippingMethodRegistry.ResolveLabel(
                methodCode,
                string.IsNullOrWhiteSpace(shipment.ShippingMethodLabel) ? null : shipment.ShippingMethodLabel),
            shipmentCount: 1,
            primaryShipmentId: shipment.ShipmentId.ToString(),
            trackingSummary: string.IsNullOrWhiteSpace(shipment.TrackingReference)
                ? string.Empty
                : shipment.TrackingReference!,
            scopeLineCount: shipment.Items.Count,
            scopeQuantity: shipment.Items.Sum(x => x.Quantity),
            scopeRemaining: 0m,
            targetShippingGroupId: shipment.ShippingGroupId,
            targetShipmentId: shipment.ShipmentId,
            targetPackageId: null,
            targetPackageNumber: null,
            pending: pending);
    }

    private static AdminFulfillmentWorkQueueRow? BuildPackageRow(
        AdminFulfillmentWorkUnitContext context,
        AdminFulfillmentPackageWorkFacts package)
    {
        // A delivered package owns no work: nothing to hand over, nothing to deliver.
        if (package.Status == ConsolidatedPackageStatus.Delivered)
        {
            return null;
        }

        var anchor = context.Units.FirstOrDefault(unit =>
            package.Members.Any(member => member.FulfillmentId == unit.FulfillmentId));
        if (anchor is null)
        {
            return null;
        }

        var plans = package.Members
            .Where(member => member.ShippingGroupId is not null)
            .SelectMany(member => PlansOf(context, member.SellerOrderId, member.ShippingGroupId))
            .GroupBy(plan => plan.ShippingGroupId)
            .Select(group => group.First())
            .OrderBy(plan => plan.HandoffDate)
            .ThenBy(plan => plan.HandoffOpenTicks)
            .ThenBy(plan => plan.ShippingGroupId)
            .ToList();
        var pending = package.Status == ConsolidatedPackageStatus.Created;
        var inTransit = package.Status == ConsolidatedPackageStatus.Dispatched;
        var timing = AdminFulfillmentDispatchTimingAuthority.Classify(pending, inTransit, plans, context.NowUtc);
        var codes = AdminFulfillmentQueueFilters.ProjectPackageActionCodes(
            package.Status,
            !string.IsNullOrWhiteSpace(package.TrackingReference));
        var sellerNames = package.Members
            .Select(member => SellerNameOf(context, member.SellerPartyId))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var orderNumbers = package.Members
            .Select(member => OrderReferenceOf(context, member.SellerOrderId))
            .Where(number => !string.IsNullOrWhiteSpace(number))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var tracking = !string.IsNullOrWhiteSpace(package.TrackingReference)
            ? package.TrackingReference!
            : string.Empty;

        return CreateRow(
            context,
            anchor,
            AdminFulfillmentWorkUnitKinds.Package,
            package.ConsolidatedPackageId,
            package.Status.ToString(),
            timing,
            plans,
            codes,
            explicitBlocker: null,
            sellerDisplayName: sellerNames.Length == 0 ? SellerNameOf(context, anchor.SellerPartyId) : string.Join("، ", sellerNames),
            orderReference: orderNumbers.Length == 0 ? OrderReferenceOf(context, anchor.SellerOrderId) : string.Join(" · ", orderNumbers),
            methodCode: package.ShippingMethodCode,
            methodLabel: ShippingMethodRegistry.ResolveLabel(
                package.ShippingMethodCode,
                string.IsNullOrWhiteSpace(package.ShippingMethodLabel) ? null : package.ShippingMethodLabel),
            shipmentCount: package.Members.Count,
            primaryShipmentId: null,
            trackingSummary: tracking,
            scopeLineCount: package.Members.Sum(member => member.LineCount),
            scopeQuantity: package.Members.Sum(member => member.Quantity),
            scopeRemaining: 0m,
            // The package itself is the exact target; a member's group is never used as a locator for it.
            targetShippingGroupId: null,
            targetShipmentId: null,
            targetPackageId: package.ConsolidatedPackageId,
            targetPackageNumber: package.PackageNumber,
            pending: pending,
            updatedAt: package.UpdatedAt);
    }

    /// <summary>
    /// History/terminal row: the seller unit owns no open physical unit (everything delivered/cancelled/failed),
    /// so `delivered`/`problem` queue filters stay reachable without inventing a parcel.
    /// </summary>
    private static AdminFulfillmentWorkQueueRow BuildHistoryRow(
        AdminFulfillmentWorkUnitContext context,
        FulfillmentSnapshot unit)
    {
        var inTransit = AdminFulfillmentQueueFilters.HasOpenTransitParcels(unit.Shipments);
        var timing = AdminFulfillmentDispatchTimingAuthority.Classify(false, inTransit, [], context.NowUtc);
        var tracking = unit.Shipments
            .Select(x => x.TrackingReference)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToArray();

        return CreateRow(
            context,
            unit,
            AdminFulfillmentWorkUnitKinds.Fulfillment,
            unit.FulfillmentId,
            unit.Status.ToString(),
            timing,
            [],
            // Finished history owns no forward operation: no fake work is offered for a delivered/failed unit.
            [],
            explicitBlocker: null,
            sellerDisplayName: SellerNameOf(context, unit.SellerPartyId),
            orderReference: OrderReferenceOf(context, unit.SellerOrderId),
            methodCode: unit.ShippingMethodCode,
            methodLabel: ShippingMethodRegistry.ResolveLabel(
                unit.ShippingMethodCode,
                string.IsNullOrWhiteSpace(unit.ShippingMethodLabel) ? null : unit.ShippingMethodLabel),
            shipmentCount: unit.Shipments.Count,
            primaryShipmentId: null,
            trackingSummary: tracking.Length == 0 ? string.Empty : string.Join(" · ", tracking),
            scopeLineCount: unit.Items.Count,
            scopeQuantity: unit.Items.Sum(x => x.QuantityOrdered),
            scopeRemaining: 0m,
            targetShippingGroupId: null,
            targetShipmentId: null,
            targetPackageId: null,
            targetPackageNumber: null,
            pending: false);
    }

    private static AdminFulfillmentWorkQueueRow CreateRow(
        AdminFulfillmentWorkUnitContext context,
        FulfillmentSnapshot unit,
        string kind,
        Guid scopeId,
        string state,
        AdminFulfillmentDispatchTiming timing,
        IReadOnlyList<OrderAcceptedGroupDispatchPlanFact> plans,
        IReadOnlyList<string> codes,
        string? explicitBlocker,
        string sellerDisplayName,
        string orderReference,
        string methodCode,
        string methodLabel,
        int shipmentCount,
        string? primaryShipmentId,
        string trackingSummary,
        int scopeLineCount,
        decimal scopeQuantity,
        decimal scopeRemaining,
        Guid? targetShippingGroupId,
        Guid? targetShipmentId,
        Guid? targetPackageId,
        string? targetPackageNumber,
        bool pending,
        DateTimeOffset? updatedAt = null)
    {
        var next = AdminFulfillmentNextStep.Resolve(codes);
        var actionable = timing.Bucket is AdminFulfillmentDispatchBucket.Overdue
            or AdminFulfillmentDispatchBucket.Today
            or AdminFulfillmentDispatchBucket.Upcoming
            or AdminFulfillmentDispatchBucket.Unspecified;
        var plan = Earliest(plans);
        var blocker = explicitBlocker
            ?? (actionable && next.Code.Length == 0 ? NoActionBlockerFa : null);

        return new AdminFulfillmentWorkQueueRow(
            unit.FulfillmentId,
            unit.SellerOrderId,
            unit.CheckoutId,
            unit.SellerPartyId,
            string.IsNullOrWhiteSpace(sellerDisplayName) ? "فروشنده" : sellerDisplayName,
            orderReference,
            AdminFulfillmentQueueFilters.ComposeOperationalStatus(unit),
            unit.RecipientName,
            unit.CityName,
            methodCode,
            methodLabel,
            unit.Items.Count,
            unit.Items.Sum(x => x.QuantityOrdered),
            unit.Items.Sum(x => x.QuantityShipped),
            shipmentCount,
            primaryShipmentId,
            trackingSummary,
            unit.CreatedAt,
            updatedAt ?? unit.UpdatedAt,
            codes,
            AdminFulfillmentDispatchBucketNames.Of(timing.Bucket),
            AdminFulfillmentDispatchBucketNames.Of(timing.WindowState),
            timing.HandoffDate,
            string.IsNullOrWhiteSpace(timing.HandoffTimeWindow) ? null : timing.HandoffTimeWindow,
            string.IsNullOrWhiteSpace(timing.HandoffTimeZone) ? null : timing.HandoffTimeZone,
            timing.DistinctPlanVariants,
            timing.DeadlineUtc,
            plan?.CustomerReceivingDate,
            plan?.CustomerReceivingTimeWindow,
            targetShippingGroupId,
            next.Code,
            next.LabelFa,
            blocker,
            pending,
            AdminFulfillmentWorkUnitKinds.Key(kind, scopeId),
            kind,
            state,
            targetShipmentId,
            targetPackageId,
            targetPackageNumber,
            scopeLineCount,
            scopeQuantity,
            scopeRemaining);
    }

    /// <summary>
    /// Group-scoped operations: processing/packing of THIS group's lines and a new parcel only when THIS scope still
    /// has packed quantity that no open Created parcel already consumed. Creation is never offered for shipped
    /// quantity (TB-257), and an already Created sibling parcel does not remove another line's remainder.
    /// </summary>
    private static IReadOnlyList<string> GroupActionCodes(
        IReadOnlyList<FulfillmentItemSnapshot> groupItems,
        decimal scopeRemaining)
    {
        var codes = new List<string>();
        if (groupItems.Any(x => x.QuantityOrdered > x.QuantityProcessing))
        {
            codes.Add("mark_processing");
        }

        if (groupItems.Any(x => x.QuantityProcessing > x.QuantityPacked))
        {
            codes.Add("mark_packed");
            codes.Add("pack_selected");
        }

        if (scopeRemaining > 0m)
        {
            codes.Add("unpack");
            codes.Add("create_shipment");
        }

        return codes;
    }

    private static IReadOnlyList<OrderAcceptedGroupDispatchPlanFact> PlansOf(
        AdminFulfillmentWorkUnitContext context,
        Guid sellerOrderId,
        Guid? shippingGroupId) =>
        shippingGroupId is { } groupId
            && context.PlansByGroup.TryGetValue((sellerOrderId, groupId), out var plans)
            ? plans
            : [];

    private static decimal ResendableOf(
        AdminFulfillmentWorkUnitContext context,
        Guid shipmentId,
        Guid orderLineId) =>
        context.ResendableBySourceLine.TryGetValue((shipmentId, orderLineId), out var quantity)
            ? quantity
            : 0m;

    private static string SellerNameOf(AdminFulfillmentWorkUnitContext context, Guid sellerPartyId) =>
        context.SellerNameByPartyId.TryGetValue(sellerPartyId, out var name) ? name : "فروشنده";

    private static string OrderReferenceOf(AdminFulfillmentWorkUnitContext context, Guid sellerOrderId) =>
        context.OrderNumberBySellerOrderId.TryGetValue(sellerOrderId, out var number) ? number : string.Empty;

    private static OrderAcceptedGroupDispatchPlanFact? Earliest(
        IReadOnlyList<OrderAcceptedGroupDispatchPlanFact> plans) =>
        plans
            .OrderBy(plan => plan.HandoffDate)
            .ThenBy(plan => plan.HandoffOpenTicks)
            .ThenBy(plan => plan.ShippingGroupId)
            .FirstOrDefault();
}
