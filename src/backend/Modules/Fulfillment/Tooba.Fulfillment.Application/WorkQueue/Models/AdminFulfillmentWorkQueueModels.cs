using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using QuantityAuthority = Tooba.Fulfillment.Contracts.Operations.FulfillmentPackedQuantityAuthority;

namespace Tooba.Fulfillment.Application.WorkQueue.Models;

/// <summary>
/// ردیف صف کار ارسال و تحویل Admin — fulfillment/seller-group نه Customer Order خام.
/// </summary>
/// <remarks>
/// TB-260: each row also carries the accepted dispatch timing (plan, bucket, deadline and the exact
/// operational-group locator) so the queue can answer "what must be handed to the carrier now" without
/// reading Shipping settings in the browser. Every timing field is additive and defaults to "unknown" so
/// older callers keep working.
/// </remarks>
public sealed record AdminFulfillmentWorkQueueRow(
    Guid FulfillmentId,
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    string SellerDisplayName,
    string OrderReference,
    string Status,
    string RecipientName,
    string CityName,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    int ItemCount,
    decimal QuantityOrdered,
    decimal QuantityShipped,
    int ShipmentCount,
    string? PrimaryShipmentId,
    string TrackingSummary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> AvailableActionCodes,
    // TB-260: mutually exclusive dispatch bucket (<c>Overdue</c>/<c>Today</c>/…).
    string DispatchBucket = AdminFulfillmentDispatchBucketNames.NotQueued,
    // TB-260: position versus the accepted half-open handoff window.
    string HandoffWindowState = AdminFulfillmentDispatchBucketNames.WindowNone,
    // TB-260: accepted handoff day (carrier calendar), null when no plan exists.
    DateOnly? PlannedHandoffDate = null,
    // TB-260: accepted handoff window text <c>hh:mm–hh:mm</c>.
    string? PlannedHandoffWindow = null,
    // TB-260: accepted carrier time zone id.
    string? PlannedHandoffTimeZone = null,
    // TB-260: number of distinct plans among this row's accepted groups (1 = one plan).
    int HandoffPlanVariants = 0,
    // TB-260: accepted close instant in UTC (the dispatch deadline), null when no plan exists.
    DateTimeOffset? DispatchDeadlineUtc = null,
    // TB-260: customer receiving date — a commitment, never the dispatch deadline.
    DateOnly? CustomerReceivingDate = null,
    // TB-260: customer receiving time window text.
    string? CustomerReceivingWindow = null,
    // TB-260: accepted shipping group of the actionable plan (exact view target).
    Guid? TargetShippingGroupId = null,
    // TB-260: the truthful next operation code for this row, empty when none is permitted.
    string NextStepCode = "",
    // TB-260: Persian label of <see cref="NextStepCode"/>.
    string NextStepLabelFa = "",
    // TB-260: why no next step is offered while the row is still active work.
    string? NextStepBlockerFa = null,
    // TB-260: true when physical preparation/dispatch work still exists on this row.
    bool PendingDispatch = false,
    // TB-260-R1: stable discriminated identity of ONE physical work unit (group/parcel/package/fulfillment).
    // It is the ONLY row identity: two parcels of one seller order never share it, and it is the key used for
    // selection, sorting tie-breaks, paging and the exact operational target locator.
    string WorkUnitKey = "",
    // TB-260-R1: <see cref="AdminFulfillmentWorkUnitKinds"/> token of this row's physical scope.
    string WorkUnitKind = AdminFulfillmentWorkUnitKinds.Group,
    // TB-260-R1: the physical state OF THIS work unit (parcel status, package status, or fulfillment status).
    string WorkUnitState = "",
    // TB-260-R1: exact parcel locator when this row IS one physical parcel.
    Guid? TargetShipmentId = null,
    // TB-260-R1: exact consolidated-package locator when this row IS one physical common package.
    Guid? TargetPackageId = null,
    // TB-260-R1: human package number (MP-…) of the package locator.
    string? TargetPackageNumber = null,
    // TB-260-R1: lines physically owned by THIS work-unit scope (never the whole seller order).
    int ScopeLineCount = 0,
    // TB-260-R1: quantity physically owned by THIS work-unit scope.
    decimal ScopeQuantity = 0,
    // TB-260-R1: quantity of this scope still eligible for a NEW parcel (packed − shipped − open Created).
    decimal ScopeRemainingQuantity = 0);

/// <summary>
/// TB-260-R1: the physical scopes a queue row can own. A row is ONE work unit, never a whole seller order:
/// an accepted group that still needs a parcel, one existing parcel, one active common package, or — only for
/// finished/history rows that own no physical unit — the fulfillment itself.
/// </summary>
public static class AdminFulfillmentWorkUnitKinds
{
    /// <summary>An accepted shipping group whose lines still need process/pack/parcel work.</summary>
    public const string Group = "Group";

    /// <summary>One existing physical parcel (independent or from TB-259 separate packing).</summary>
    public const string Parcel = "Parcel";

    /// <summary>One active consolidated common package spanning its locked members.</summary>
    public const string Package = "Package";

    /// <summary>No physical unit is open (delivered/cancelled/failed history row).</summary>
    public const string Fulfillment = "Fulfillment";

    /// <summary>The stable, discriminated row key of one physical work unit.</summary>
    public static string Key(string kind, Guid id) => $"{kind.ToLowerInvariant()}:{id:D}";
}

/// <summary>
/// فیلترهای سریع صف کار بر اساس وضعیت واقعی دامنه — بدون SLA ساختگی.
/// </summary>
public static class AdminFulfillmentQueueFilters
{
    /// <summary>همه.</summary>
    public const string All = "all";
    /// <summary>نیازمند اقدام.</summary>
    public const string NeedsAction = "needs_action";
    /// <summary>آماده پردازش.</summary>
    public const string ReadyToProcess = "ready_to_process";
    /// <summary>آماده بسته‌بندی.</summary>
    public const string ReadyToPack = "ready_to_pack";
    /// <summary>آماده ارسال.</summary>
    public const string ReadyToShip = "ready_to_ship";
    /// <summary>بدون کد رهگیری.</summary>
    public const string MissingTracking = "missing_tracking";
    /// <summary>در مسیر.</summary>
    public const string InTransit = "in_transit";
    /// <summary>تحویل‌شده.</summary>
    public const string Delivered = "delivered";
    /// <summary>مشکل‌دار.</summary>
    public const string Problem = "problem";
    /// <summary>TB-260: pending work whose accepted handoff window has opened (including overdue).</summary>
    public const string DispatchDue = AdminFulfillmentDispatchTimingAuthority.FilterDue;
    /// <summary>TB-260: pending work whose accepted handoff day is today (carrier zone).</summary>
    public const string DispatchToday = AdminFulfillmentDispatchTimingAuthority.FilterToday;
    /// <summary>TB-260: pending work at/after the accepted close instant or a past handoff day.</summary>
    public const string DispatchOverdue = AdminFulfillmentDispatchTimingAuthority.FilterOverdue;
    /// <summary>TB-260: pending work with a strictly future accepted handoff day.</summary>
    public const string DispatchUpcoming = AdminFulfillmentDispatchTimingAuthority.FilterUpcoming;
    /// <summary>TB-260: pending work whose accepted evidence carries no plan.</summary>
    public const string DispatchUnspecified = AdminFulfillmentDispatchTimingAuthority.FilterUnspecified;

    /// <summary>کدهای شناخته‌شدهٔ فیلتر سریع.</summary>
    public static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        All,
        NeedsAction,
        ReadyToProcess,
        ReadyToPack,
        ReadyToShip,
        MissingTracking,
        InTransit,
        Delivered,
        Problem,
        DispatchDue,
        DispatchToday,
        DispatchOverdue,
        DispatchUpcoming,
        DispatchUnspecified,
    };

    /// <summary>
    /// TB-260: physical work state of one fulfillment row. A row is <b>pending dispatch work</b> while any
    /// preparation/parcel step is still open (a Created parcel is dispatch work even when nothing else is
    /// left, exactly like TB-257 quantity authority). Delivered/cancelled rows are never pending.
    /// </summary>
    public static bool HasPendingDispatchWork(
        FulfillmentStatus status,
        IReadOnlyList<FulfillmentItemSnapshot> items,
        IReadOnlyList<ShipmentSnapshot> shipments)
    {
        if (status is FulfillmentStatus.Cancelled or FulfillmentStatus.Delivered)
        {
            return false;
        }

        if (HasProcessableQuantity(items)
            || HasPackableQuantity(items)
            || HasUnallocatedShipmentQuantity(items, shipments))
        {
            return true;
        }

        // An open Created parcel still needs tracking and/or a carrier handoff.
        return shipments.Any(s => s.Status == ShipmentStatus.Created);
    }

    /// <summary>TB-260: a parcel is physically out of the warehouse (in transit bucket).</summary>
    public static bool HasOpenTransitParcels(IEnumerable<ShipmentSnapshot> shipments) =>
        shipments.Any(s => s.Status is ShipmentStatus.Dispatched
            or ShipmentStatus.InTransit
            or ShipmentStatus.NonDelivered
            or ShipmentStatus.Returning
            or ShipmentStatus.Returned);

    /// <summary>
    /// آیا واحد با فیلتر سریع هم‌خوان است (پس از بارگذاری shipments برای missing_tracking/needs_action).
    /// برای فیلترهای فقط‌وضعیت می‌توان در SQL اعمال کرد.
    /// </summary>
    public static bool MatchesStatusOnly(FulfillmentStatus status, string queueFilter)
    {
        return queueFilter.Trim().ToLowerInvariant() switch
        {
            ReadyToProcess => status == FulfillmentStatus.ReadyToFulfill,
            ReadyToPack => status == FulfillmentStatus.Processing,
            ReadyToShip => status == FulfillmentStatus.Packed,
            InTransit => status is FulfillmentStatus.Dispatched or FulfillmentStatus.InTransit,
            Delivered => status == FulfillmentStatus.Delivered,
            Problem => status == FulfillmentStatus.Failed,
            _ => true,
        };
    }

    /// <summary>وضعیت نمایشی quantity-aware؛ enum دامنه را عوض نمی‌کند.</summary>
    public const string PartialDispatched = "PartialDispatched";

    /// <summary>باقیماندهٔ قابل fulfillment وقتی تعداد سفارش از ارسال بیشتر است.</summary>
    public static bool HasRemainingFulfillableQuantity(IReadOnlyList<FulfillmentItemSnapshot> items) =>
        items.Any(x => x.QuantityOrdered > x.QuantityShipped);

    /// <summary>
    /// وضعیت عملیاتی ترکیب‌شده: ارسال جزئی وقتی مقدار ارسال‌شده کمتر از سفارش است.
    /// وضعیت persisted <see cref="FulfillmentStatus.Dispatched"/> حفظ می‌شود.
    /// </summary>
    public static string ComposeOperationalStatus(FulfillmentSnapshot fulfillment)
    {
        if (fulfillment.Status is FulfillmentStatus.Cancelled or FulfillmentStatus.Failed)
        {
            return fulfillment.Status.ToString();
        }

        var ordered = fulfillment.Items.Sum(x => x.QuantityOrdered);
        var shipped = fulfillment.Items.Sum(x => x.QuantityShipped);
        if (ordered > 0 && shipped > 0 && shipped < ordered)
        {
            return PartialDispatched;
        }

        return fulfillment.Status.ToString();
    }

    /// <summary>آیا مرسولهٔ Created بدون کد رهگیری دارد.</summary>
    public static bool HasMissingTracking(IEnumerable<ShipmentSnapshot> shipments) =>
        shipments.Any(s =>
            s.Status == ShipmentStatus.Created
            && string.IsNullOrWhiteSpace(s.TrackingReference));

    /// <summary>
    /// نیازمند اقدام: آماده پردازش، در حال پردازش با ظرفیت بسته‌بندی، بدون رهگیری، Packed با ظرفیت ارسال، یا Failed.
    /// </summary>
    public static bool MatchesNeedsAction(
        FulfillmentStatus status,
        IReadOnlyList<ShipmentSnapshot> shipments,
        IReadOnlyList<FulfillmentItemSnapshot> items)
    {
        if (status is FulfillmentStatus.Cancelled or FulfillmentStatus.Delivered)
        {
            return false;
        }

        if (status == FulfillmentStatus.Failed || status == FulfillmentStatus.ReadyToFulfill)
        {
            return true;
        }

        if (HasProcessableQuantity(items)
            || HasPackableQuantity(items)
            || HasUnallocatedShipmentQuantity(items, shipments))
        {
            return true;
        }

        if (HasMissingTracking(shipments))
        {
            return true;
        }

        return false;
    }

    /// <summary>کدهای عملیات قابل‌اجرای گروهی بدون ورودی اضافه.</summary>
    public static readonly HashSet<string> SafeBulkActionCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "mark_processing",
        "mark_packed",
        "dispatch_shipment",
        "deliver_shipment",
    };

    /// <summary>کدهای عملیاتی دامنه برای ردیف — بدون بررسی مجوز (اجرا در عملیات سفارش تأیید می‌شود).</summary>
    public static IReadOnlyList<string> ProjectActionCodes(FulfillmentSnapshot fulfillment)
    {
        if (fulfillment.Status is FulfillmentStatus.Cancelled)
        {
            return [];
        }

        var codes = new List<string>();
        if (HasProcessableQuantity(fulfillment.Items))
        {
            codes.Add("mark_processing");
        }

        if (HasPackableQuantity(fulfillment.Items))
        {
            codes.Add("mark_packed");
            codes.Add("pack_selected");
        }

        if (HasUnpackableQuantity(fulfillment))
        {
            codes.Add("unpack");
        }

        if (fulfillment.Status is not (FulfillmentStatus.Cancelled or FulfillmentStatus.Failed or FulfillmentStatus.Delivered)
            && HasUnallocatedShipmentQuantity(fulfillment.Items, fulfillment.Shipments))
        {
            codes.Add("create_shipment");
        }

        foreach (var shipment in fulfillment.Shipments.Where(s => s.Status != ShipmentStatus.Cancelled))
        {
            if (shipment.Status == ShipmentStatus.Created)
            {
                codes.Add("cancel_shipment");
                if (string.IsNullOrWhiteSpace(shipment.TrackingReference))
                {
                    codes.Add("assign_tracking");
                }
                else if (shipment.DispatchedAt is null)
                {
                    codes.Add("correct_tracking");
                    codes.Add("dispatch_shipment");
                }
            }

            if (shipment.Status is ShipmentStatus.Dispatched or ShipmentStatus.InTransit
                && shipment.DeliveredAt is null)
            {
                codes.Add("deliver_shipment");
            }
        }

        return codes.Distinct(StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    /// TB-260-R1: the exact operations ONE physical parcel permits, derived from its own lifecycle only. A parcel
    /// row never borrows a sibling parcel's exit, so a Created parcel cannot offer «ثبت تحویل» and a delivered one
    /// cannot offer «ارسال».
    /// </summary>
    /// <remarks>
    /// For a returned parcel the ONLY forward exit is a new resend parcel, and a resend needs physically received
    /// usable capacity (<paramref name="hasResendableCapacity"/>), so an emptied/committed parcel truthfully offers
    /// no action instead of promising one the command would refuse.
    /// </remarks>
    public static IReadOnlyList<string> ProjectParcelActionCodes(
        ShipmentStatus status,
        bool hasTracking,
        bool hasDispatchedAt,
        bool hasResendableCapacity)
    {
        var codes = new List<string>();
        switch (status)
        {
            case ShipmentStatus.Created:
                codes.Add("cancel_shipment");
                if (!hasTracking)
                {
                    codes.Add("assign_tracking");
                }
                else if (!hasDispatchedAt)
                {
                    codes.Add("correct_tracking");
                    codes.Add("dispatch_shipment");
                }

                break;
            case ShipmentStatus.Dispatched:
            case ShipmentStatus.InTransit:
                codes.Add("deliver_shipment");
                codes.Add("record_non_delivery");
                break;
            case ShipmentStatus.NonDelivered:
                // The courier already reported a failed attempt: the next physical step is the return, while a
                // late genuine delivery stays reachable inside the shared operational view.
                codes.Add("start_return");
                break;
            case ShipmentStatus.Returning:
                codes.Add("record_return_receipt");
                break;
            case ShipmentStatus.Returned:
                if (hasResendableCapacity)
                {
                    codes.Add("resend_shipment");
                }

                break;
            default:
                break;
        }

        return codes;
    }

    /// <summary>
    /// TB-260-R1: the exact operations ONE consolidated common package permits. A locked member parcel exposes
    /// nothing of its own, so the package is the single action owner of the physical unit.
    /// </summary>
    public static IReadOnlyList<string> ProjectPackageActionCodes(
        ConsolidatedPackageStatus status,
        bool hasTracking)
    {
        return status switch
        {
            ConsolidatedPackageStatus.Created => hasTracking
                ? ["cancel_consolidated_package", "dispatch_consolidated_package"]
                : ["cancel_consolidated_package", "assign_consolidated_package_tracking", "dispatch_consolidated_package"],
            ConsolidatedPackageStatus.Dispatched => ["deliver_consolidated_package"],
            _ => [],
        };
    }

    /// <summary>
    /// TB-260-R1: the work an accepted group still owns for its OWN lines — processing, packing, or the remainder
    /// that is still eligible for a new parcel. None of these can be satisfied by a different group's work, so an
    /// already-dispatched sibling can never make this group look finished.
    /// </summary>
    public static bool HasGroupScopeWork(
        IReadOnlyList<FulfillmentItemSnapshot> groupItems,
        IReadOnlyList<ShipmentSnapshot> groupShipments)
    {
        if (HasProcessableQuantity(groupItems) || HasPackableQuantity(groupItems))
        {
            return true;
        }

        foreach (var item in groupItems)
        {
            if (ScopeRemainingPackedQuantity(item, groupShipments) > 0m)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// TB-260-R1: remaining quantity of ONE line inside ONE Scope = packed − shipped − open Created first
    /// allocations, reusing the single TB-257 quantity authority (never a second formula).
    /// </summary>
    public static decimal ScopeRemainingPackedQuantity(
        FulfillmentItemSnapshot item,
        IReadOnlyList<ShipmentSnapshot> scopeShipments)
    {
        var openAllocated = scopeShipments
            .Where(s => s.Status == ShipmentStatus.Created && s.ResendOfShipmentId is null)
            .SelectMany(s => s.Items)
            .Where(line => line.OrderLineId == item.OrderLineId)
            .Sum(line => line.Quantity);
        return QuantityAuthority.RemainingPackedQuantity(
            item.QuantityPacked, item.QuantityShipped, openAllocated);
    }

    /// <summary>سازگاری انتخاب چندتایی برای یک کد عملیات — همان فروشنده + همه دارای کد.</summary>
    public static bool AreBulkCompatible(
        IReadOnlyList<AdminFulfillmentWorkQueueRow> rows,
        string actionCode)
    {
        if (rows.Count == 0 || string.IsNullOrWhiteSpace(actionCode))
        {
            return false;
        }

        var seller = rows[0].SellerPartyId;
        if (rows.Any(r => r.SellerPartyId != seller))
        {
            return false;
        }

        return rows.All(r => r.AvailableActionCodes.Contains(actionCode, StringComparer.OrdinalIgnoreCase));
    }

    private static bool HasProcessableQuantity(IReadOnlyList<FulfillmentItemSnapshot> items) =>
        items.Any(x => x.QuantityOrdered > x.QuantityProcessing);

    private static bool HasPackableQuantity(IReadOnlyList<FulfillmentItemSnapshot> items) =>
        items.Any(x => x.QuantityProcessing > x.QuantityPacked);

    private static bool HasUnallocatedShipmentQuantity(
        IReadOnlyList<FulfillmentItemSnapshot> items,
        IReadOnlyList<ShipmentSnapshot> shipments)
    {
        // TB-257: ONE owning rule (packed − shipped − open Created). Packing a line that is already shipped must
        // never look like a new shippable remainder.
        foreach (var item in items)
        {
            var openAllocated = shipments
                .Where(s => s.Status == ShipmentStatus.Created)
                .SelectMany(s => s.Items)
                .Where(line => line.OrderLineId == item.OrderLineId)
                .Sum(line => line.Quantity);
            if (QuantityAuthority.RemainingPackedQuantity(
                    item.QuantityPacked, item.QuantityShipped, openAllocated) > 0m)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasUnpackableQuantity(FulfillmentSnapshot fulfillment)
    {
        foreach (var item in fulfillment.Items)
        {
            var openAllocated = fulfillment.Shipments
                .Where(s => s.Status == ShipmentStatus.Created)
                .SelectMany(s => s.Items)
                .Where(line => line.OrderLineId == item.OrderLineId)
                .Sum(line => line.Quantity);
            if (QuantityAuthority.RemainingPackedQuantity(
                    item.QuantityPacked, item.QuantityShipped, openAllocated) > 0m)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>درخواست اجرای گروهی روی ردیف‌های صف کار.</summary>
public sealed record AdminFulfillmentWorkQueueBulkRequest(
    string ActionCode,
    IReadOnlyList<AdminFulfillmentWorkQueueBulkItem> Items,
    string? TrackingReference = null,
    string? CarrierDisplayName = null,
    string? ShippingMethodCode = null);

/// <summary>یک هدف گروهی در صف کار.</summary>
public sealed record AdminFulfillmentWorkQueueBulkItem(
    Guid CheckoutId,
    Guid FulfillmentId,
    Guid SellerOrderId,
    Guid? ShipmentId);

/// <summary>نتیجهٔ اجرای گروهی — بدون موفقیت جزئی خاموش.</summary>
public sealed record AdminFulfillmentWorkQueueBulkResult(
    int Attempted,
    int Succeeded,
    string? ErrorCode,
    string? ErrorMessage);

/// <summary>
/// TB-260: honest counts of the SAME filtered set the queue pages through. <see cref="TotalActive"/> is the
/// number of rows that still carry pending dispatch work (Today + Overdue + Upcoming + Unspecified), the
/// timing buckets are mutually exclusive, <see cref="Due"/> is their overlapping «سررسیدشده» subset
/// (Today-open + Overdue), and InTransit is reported separately because "in the carrier's hands" is a state,
/// not an outstanding preparation task. No physical parcel/package shape can inflate a count.
/// </summary>
public sealed record AdminFulfillmentWorkQueueCounts(
    int TotalActive,
    int Due,
    int Today,
    int Overdue,
    int Upcoming,
    int Unspecified,
    int InTransit);

/// <summary>
/// TB-260: one queue page plus the bucket counts of the whole filtered set it was cut from. Field names
/// stay compatible with the shared grid page (<c>items/page/pageSize/totalCount</c>).
/// </summary>
public sealed record AdminFulfillmentWorkQueuePage(
    IReadOnlyList<AdminFulfillmentWorkQueueRow> Items,
    int Page,
    int PageSize,
    int TotalCount,
    AdminFulfillmentWorkQueueCounts DispatchCounts);
