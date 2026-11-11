using Tooba.Order.Contracts.Shipping;

namespace Tooba.Fulfillment.Application.WorkQueue.Models;

/// <summary>
/// TB-260: the ONE physical-dispatch timing classification of an operational queue row. The bucket is
/// mutually exclusive so counts and pages cannot double-count the same work.
/// </summary>
public enum AdminFulfillmentDispatchBucket
{
    /// <summary>No active dispatch work remains (delivered/cancelled, nothing in transit).</summary>
    NotQueued = 0,
    /// <summary>Pending work whose accepted handoff window has already closed, or whose handoff day is past.</summary>
    Overdue = 1,
    /// <summary>Pending work whose accepted handoff day is <b>today</b> in the accepted carrier zone.</summary>
    Today = 2,
    /// <summary>Pending work whose accepted handoff day is in the future.</summary>
    Upcoming = 3,
    /// <summary>Pending work whose accepted evidence carries no plan (historical/legacy group).</summary>
    Unspecified = 4,
    /// <summary>No preparation work left; a dispatched/in-transit/non-delivered parcel is still open.</summary>
    InTransit = 5,
}

/// <summary>
/// TB-260: exactly where a row sits relative to its accepted handoff window. Display labels are derived
/// from these flags, never from a browser clock.
/// </summary>
public enum AdminFulfillmentHandoffWindowState
{
    /// <summary>No plan (or nothing pending).</summary>
    None = 0,
    /// <summary>The accepted handoff day is still ahead.</summary>
    Future = 1,
    /// <summary>Today, before <c>open</c> in the accepted carrier zone.</summary>
    TodayNotOpen = 2,
    /// <summary>Now is inside <c>[open, close)</c>.</summary>
    Open = 3,
    /// <summary>Now is at or past <c>close</c>, or the handoff day already passed.</summary>
    Closed = 4,
}

/// <summary>
/// TB-260: wire-stable names of the timing facts. The browser compares these exact strings; a localized
/// label is derived from them and never parsed out of Persian text.
/// </summary>
public static class AdminFulfillmentDispatchBucketNames
{
    /// <summary>Bucket names (mirror of <see cref="AdminFulfillmentDispatchBucket"/>).</summary>
    public const string NotQueued = "NotQueued";
    /// <inheritdoc cref="AdminFulfillmentDispatchBucket.Overdue" />
    public const string Overdue = "Overdue";
    /// <inheritdoc cref="AdminFulfillmentDispatchBucket.Today" />
    public const string Today = "Today";
    /// <inheritdoc cref="AdminFulfillmentDispatchBucket.Upcoming" />
    public const string Upcoming = "Upcoming";
    /// <inheritdoc cref="AdminFulfillmentDispatchBucket.Unspecified" />
    public const string Unspecified = "Unspecified";
    /// <inheritdoc cref="AdminFulfillmentDispatchBucket.InTransit" />
    public const string InTransit = "InTransit";

    /// <summary>Handoff window-state names (mirror of <see cref="AdminFulfillmentHandoffWindowState"/>).</summary>
    public const string WindowNone = "None";
    /// <inheritdoc cref="AdminFulfillmentHandoffWindowState.Future" />
    public const string WindowFuture = "Future";
    /// <inheritdoc cref="AdminFulfillmentHandoffWindowState.TodayNotOpen" />
    public const string WindowTodayNotOpen = "TodayNotOpen";
    /// <inheritdoc cref="AdminFulfillmentHandoffWindowState.Open" />
    public const string WindowOpen = "Open";
    /// <inheritdoc cref="AdminFulfillmentHandoffWindowState.Closed" />
    public const string WindowClosed = "Closed";

    /// <summary>The canonical bucket name of one classification.</summary>
    public static string Of(AdminFulfillmentDispatchBucket bucket) => bucket.ToString();

    /// <summary>The canonical window-state name of one classification.</summary>
    public static string Of(AdminFulfillmentHandoffWindowState state) => state.ToString();
}

/// <summary>
/// TB-260: the row's truthful next operation. It is derived from the SAME projected action codes the
/// owning commands enforce, so a due row never promises an operation the server would refuse.
/// </summary>
public static class AdminFulfillmentNextStep
{
    private static readonly (string Code, string LabelFa)[] Progression =
    [
        ("mark_processing", "شروع پردازش"),
        ("mark_packed", "بسته‌بندی"),
        ("assign_tracking", "ثبت کد رهگیری"),
        // TB-260-R1: once a Created parcel HAS a tracking code its physical next step is the carrier handoff;
        // «اصلاح کد رهگیری» is a correction, not the outstanding operation.
        ("dispatch_shipment", "ارسال به حامل"),
        ("correct_tracking", "اصلاح کد رهگیری"),
        // TB-260-R1: a consolidated common package owns its own tracking/dispatch/delivery commands, so the
        // package row's next step is the package command — never a locked member parcel's command.
        ("assign_consolidated_package_tracking", "ثبت کد رهگیری بسته"),
        ("dispatch_consolidated_package", "ارسال بستهٔ تجمیعی"),
        ("deliver_consolidated_package", "تحویل بستهٔ تجمیعی"),
        ("create_shipment", "ایجاد مرسوله"),
        ("deliver_shipment", "ثبت تحویل"),
        ("record_non_delivery", "ثبت عدم تحویل"),
        ("start_return", "برگشت به مبدأ"),
        ("record_return_receipt", "تأیید دریافت در مبدأ"),
        ("resend_shipment", "ارسال مجدد"),
        // TB-262: damaged received units are not resend capacity; their decision is its own physical next step.
        ("resolve_damaged_goods", "تعیین تکلیف کالای آسیب‌دیده"),
    ];

    /// <summary>First permitted operation in physical progression, or none.</summary>
    public static (string Code, string LabelFa) Resolve(IReadOnlyList<string> availableActionCodes)
    {
        ArgumentNullException.ThrowIfNull(availableActionCodes);
        foreach (var step in Progression)
        {
            if (availableActionCodes.Contains(step.Code, StringComparer.OrdinalIgnoreCase))
            {
                return step;
            }
        }

        return (string.Empty, string.Empty);
    }
}


public sealed record AdminFulfillmentDispatchTiming(
    AdminFulfillmentDispatchBucket Bucket,
    AdminFulfillmentHandoffWindowState WindowState,
    bool HasPlan,
    bool IsToday,
    bool IsOverdue,
    DateTimeOffset? OpensAtUtc,
    DateTimeOffset? DeadlineUtc,
    DateOnly? HandoffDate,
    string HandoffTimeWindow,
    string? HandoffTimeZone,
    int DistinctPlanVariants,
    // TB-264: operational preparation state relative to the SAME accepted plan and the SAME injected now.
    AdminFulfillmentPreparationState PreparationState = AdminFulfillmentPreparationState.None,
    // TB-264: latest instant at which physical preparation must start (policy v2+, else null).
    DateTimeOffset? PreparationStartUtc = null,
    // TB-264: latest instant at which the parcel must leave the seller toward the carrier.
    DateTimeOffset? TransferDepartureUtc = null,
    // TB-264: latest carrier acceptance instant = the dispatch deadline (same instant as <see cref="DeadlineUtc"/>).
    DateTimeOffset? HandoffDeadlineUtc = null,
    // TB-264: accepted timing policy version (1 = no operational deadlines, 2+ = preparation-aware).
    int PolicyVersion = 1);

/// <summary>
/// TB-264: where physical preparation sits relative to the accepted operational deadlines. It is derived
/// server-side from the accepted plan and an injected <c>nowUtc</c>; the browser never computes it.
/// </summary>
public enum AdminFulfillmentPreparationState
{
    /// <summary>No plan, or the accepted plan carries no preparation deadline.</summary>
    None = 0,
    /// <summary>Physical work is already done (parcel dispatched or later): nothing to prepare.</summary>
    NotApplicable = 1,
    /// <summary>Preparation may still wait: <c>now &lt; preparationStart</c> and not today.</summary>
    Upcoming = 2,
    /// <summary>Preparation starts today (carrier zone) and its start instant is still ahead.</summary>
    Today = 3,
    /// <summary>Preparation should already be underway (<c>now &gt;= preparationStart</c>).</summary>
    Overdue = 4,
    /// <summary>The carrier acceptance deadline already passed: the accepted date is no longer reachable.</summary>
    HandoffMissed = 5,
}

/// <summary>TB-264: wire-stable names of <see cref="AdminFulfillmentPreparationState"/>.</summary>
public static class AdminFulfillmentPreparationStateNames
{
    /// <inheritdoc cref="AdminFulfillmentPreparationState.None" />
    public const string None = "None";
    /// <inheritdoc cref="AdminFulfillmentPreparationState.NotApplicable" />
    public const string NotApplicable = "NotApplicable";
    /// <inheritdoc cref="AdminFulfillmentPreparationState.Upcoming" />
    public const string Upcoming = "Upcoming";
    /// <inheritdoc cref="AdminFulfillmentPreparationState.Today" />
    public const string Today = "Today";
    /// <inheritdoc cref="AdminFulfillmentPreparationState.Overdue" />
    public const string Overdue = "Overdue";
    /// <inheritdoc cref="AdminFulfillmentPreparationState.HandoffMissed" />
    public const string HandoffMissed = "HandoffMissed";

    /// <summary>The canonical name of one preparation state.</summary>
    public static string Of(AdminFulfillmentPreparationState state) => state.ToString();
}

/// <summary>
/// TB-260: <b>ONE</b> pure authority that answers "where does this work sit in the accepted dispatch
/// timeline" with an explicit <c>nowUtc</c> and the accepted carrier time zone. No IO, no ambient clock, no
/// browser date arithmetic, and the customer receiving date is never an input.
/// </summary>
/// <remarks>
/// <para>
/// Window semantics match TB-256 exactly: the handoff window is half-open <c>[open, close)</c>, therefore
/// exactly <c>close</c> is already late and exactly <c>open</c> is already open. "Today" is the calendar day
/// of <c>nowUtc</c> converted to the accepted carrier zone, never the server's local date.
/// </para>
/// <para>
/// Filters deliberately overlap the exclusive buckets: «سررسیدشده» = every pending plan whose window has
/// opened (today-open + overdue), «ارسال امروز» = every pending plan whose accepted handoff day is today
/// (including a window that has not opened yet), «عقب‌افتاده» = overdue only, «آینده» = a strictly future
/// handoff day. «در مسیر» is a state, not a timing bucket, and never contributes to pending counts.
/// </para>
/// </remarks>
public static class AdminFulfillmentDispatchTimingAuthority
{
    /// <summary>«سررسیدشده» — pending work whose accepted window has opened (including overdue).</summary>
    public const string FilterDue = "dispatch_due";

    /// <summary>«ارسال امروز» — pending work whose accepted handoff day is today in the carrier zone.</summary>
    public const string FilterToday = "dispatch_today";

    /// <summary>«عقب‌افتاده» — pending work at/after the accepted close instant or a past handoff day.</summary>
    public const string FilterOverdue = "dispatch_overdue";

    /// <summary>«آینده» — pending work with a strictly future accepted handoff day.</summary>
    public const string FilterUpcoming = "dispatch_upcoming";

    /// <summary>«موعد ارسال مشخص نشده» — pending work whose accepted evidence carries no plan.</summary>
    public const string FilterUnspecified = "dispatch_unspecified";

    /// <summary>TB-264: «شروع آماده‌سازی رسیده/گذشته» — pending work whose preparation must already have started.</summary>
    public const string FilterPreparationDue = "dispatch_preparation_due";

    /// <summary>All dispatch-timing filters.</summary>
    public static readonly IReadOnlyList<string> TimingFilters =
    [
        FilterDue,
        FilterToday,
        FilterOverdue,
        FilterUpcoming,
        FilterUnspecified,
        FilterPreparationDue,
    ];

    /// <summary>Is this one of the dispatch-timing filters owned here?</summary>
    public static bool IsTimingFilter(string? value) =>
        value is not null && TimingFilters.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Classifies one row. <paramref name="pending"/> is the caller's physical work state (Fulfillment
    /// domain), <paramref name="inTransit"/> is "a parcel exists that is dispatched or later".
    /// </summary>
    public static AdminFulfillmentDispatchTiming Classify(
        bool pending,
        bool inTransit,
        IReadOnlyList<OrderAcceptedGroupDispatchPlanFact> plans,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(plans);

        if (!pending)
        {
            if (!inTransit)
            {
                return new AdminFulfillmentDispatchTiming(
                    AdminFulfillmentDispatchBucket.NotQueued,
                    AdminFulfillmentHandoffWindowState.None,
                    false,
                    false,
                    false,
                    null,
                    null,
                    null,
                    string.Empty,
                    null,
                    0);
            }

            // No preparation work left, but a dispatched/in-transit/non-delivered parcel is still open.
            // The plan is reported for context only; nothing here is "due dispatch work".
            var transit = EarliestPlan(plans);
            return new AdminFulfillmentDispatchTiming(
                AdminFulfillmentDispatchBucket.InTransit,
                AdminFulfillmentHandoffWindowState.None,
                transit is not null,
                false,
                false,
                null,
                null,
                transit?.HandoffDate,
                transit is null ? string.Empty : Window(transit),
                transit?.HandoffTimeZone,
                plans.Count,
                transit is null
                    ? AdminFulfillmentPreparationState.None
                    : AdminFulfillmentPreparationState.NotApplicable,
                transit?.PreparationStartUtc,
                transit?.TransferDepartureUtc,
                transit?.HandoffDeadlineUtc,
                transit?.PolicyVersion ?? 1);
        }

        if (plans.Count == 0)
        {
            return new AdminFulfillmentDispatchTiming(
                AdminFulfillmentDispatchBucket.Unspecified,
                AdminFulfillmentHandoffWindowState.None,
                false,
                false,
                false,
                null,
                null,
                null,
                string.Empty,
                null,
                0);
        }

        // The actionable plan is the EARLIEST accepted handoff: it is the one that can become late first.
        // Extra variants are counted, never averaged into an invented single date.
        var plan = EarliestPlan(plans)!;
        var (opensAt, deadline) = Instants(plan);
        var todayLocal = LocalDate(nowUtc, plan.HandoffTimeZone);
        var isToday = plan.HandoffDate == todayLocal;
        var handedOff = plan.HandoffDate < todayLocal;
        var closed = handedOff || nowUtc >= deadline;

        var bucket = closed
            ? AdminFulfillmentDispatchBucket.Overdue
            : isToday
                ? AdminFulfillmentDispatchBucket.Today
                : AdminFulfillmentDispatchBucket.Upcoming;

        var windowState = closed
            ? AdminFulfillmentHandoffWindowState.Closed
            : nowUtc >= opensAt
                ? AdminFulfillmentHandoffWindowState.Open
                : isToday
                    ? AdminFulfillmentHandoffWindowState.TodayNotOpen
                    : AdminFulfillmentHandoffWindowState.Future;

        return new AdminFulfillmentDispatchTiming(
            bucket,
            windowState,
            true,
            isToday,
            bucket == AdminFulfillmentDispatchBucket.Overdue,
            opensAt,
            deadline,
            plan.HandoffDate,
            Window(plan),
            plan.HandoffTimeZone,
            plans.Count,
            PreparationStateOf(plan, nowUtc, notApplicable: false),
            plan.PreparationStartUtc,
            plan.TransferDepartureUtc,
            plan.HandoffDeadlineUtc ?? deadline,
            plan.PolicyVersion);
    }

    /// <summary>
    /// TB-264: ONE pure derivation of physical-preparation urgency from the accepted plan and the injected
    /// <c>nowUtc</c>. <paramref name="notApplicable"/> is true when the physical work is already done (a
    /// dispatched/in-transit parcel), so a stale preparation deadline can never demand preparation again.
    /// </summary>
    public static AdminFulfillmentPreparationState PreparationStateOf(
        OrderAcceptedGroupDispatchPlanFact plan,
        DateTimeOffset nowUtc,
        bool notApplicable)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (notApplicable)
        {
            return AdminFulfillmentPreparationState.NotApplicable;
        }

        if (plan.PreparationStartUtc is null)
        {
            return AdminFulfillmentPreparationState.None;
        }

        var handoffDeadline = plan.HandoffDeadlineUtc ?? Instants(plan).DeadlineUtc;
        if (nowUtc >= handoffDeadline)
        {
            return AdminFulfillmentPreparationState.HandoffMissed;
        }

        if (nowUtc >= plan.PreparationStartUtc.Value)
        {
            return AdminFulfillmentPreparationState.Overdue;
        }

        var today = LocalDate(nowUtc, plan.HandoffTimeZone);
        var preparationLocalDate = LocalDate(plan.PreparationStartUtc.Value, plan.HandoffTimeZone);
        return preparationLocalDate <= today
            ? AdminFulfillmentPreparationState.Today
            : AdminFulfillmentPreparationState.Upcoming;
    }

    /// <summary>Does a classified row belong to the requested dispatch-timing filter?</summary>
    public static bool Matches(AdminFulfillmentDispatchTiming timing, string? filter)
    {
        ArgumentNullException.ThrowIfNull(timing);
        if (!IsTimingFilter(filter))
        {
            return true;
        }

        var preparationDue = IsPreparationDue(timing);
        return filter!.Trim().ToLowerInvariant() switch
        {
            // TB-264: «سررسیدشده» is prep-aware — a row whose preparation must already have started is due now
            // even when the handoff window has not opened yet, so it can never look comfortably upcoming.
            FilterDue => preparationDue
                || timing.Bucket is AdminFulfillmentDispatchBucket.Today or AdminFulfillmentDispatchBucket.Overdue
                && timing.WindowState is AdminFulfillmentHandoffWindowState.Open
                    or AdminFulfillmentHandoffWindowState.Closed,
            FilterToday => timing.Bucket == AdminFulfillmentDispatchBucket.Today,
            FilterOverdue => timing.Bucket == AdminFulfillmentDispatchBucket.Overdue,
            FilterUpcoming => timing.Bucket == AdminFulfillmentDispatchBucket.Upcoming,
            FilterUnspecified => timing.Bucket == AdminFulfillmentDispatchBucket.Unspecified,
            FilterPreparationDue => preparationDue,
            _ => true,
        };
    }

    /// <summary>
    /// TB-264: true when the accepted plan says physical preparation should already be underway (or the
    /// carrier deadline has passed). Legacy policy-v1 plans have no preparation deadline, so they are never
    /// claimed as preparation-due.
    /// </summary>
    public static bool IsPreparationDue(AdminFulfillmentDispatchTiming timing)
    {
        ArgumentNullException.ThrowIfNull(timing);
        return timing.PreparationState is AdminFulfillmentPreparationState.Overdue
            or AdminFulfillmentPreparationState.HandoffMissed;
    }

    /// <summary>
    /// Deterministic queue priority: overdue first (earliest deadline first), then today, then upcoming,
    /// then unspecified, then in-transit; ties break on the handoff instant and finally on the row id so a
    /// page can never reorder between two identical requests.
    /// </summary>
    public static int PriorityRank(AdminFulfillmentDispatchBucket bucket) => bucket switch
    {
        AdminFulfillmentDispatchBucket.Overdue => 0,
        AdminFulfillmentDispatchBucket.Today => 1,
        AdminFulfillmentDispatchBucket.Upcoming => 2,
        AdminFulfillmentDispatchBucket.Unspecified => 3,
        AdminFulfillmentDispatchBucket.InTransit => 4,
        _ => 5,
    };

    /// <summary>Converts accepted ticks to absolute instants in the accepted carrier zone.</summary>
    public static (DateTimeOffset OpensAtUtc, DateTimeOffset DeadlineUtc) Instants(
        OrderAcceptedGroupDispatchPlanFact plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var zone = ResolveZone(plan.HandoffTimeZone);
        var openSpan = TimeSpan.FromTicks(plan.HandoffOpenTicks);
        var closeSpan = TimeSpan.FromTicks(plan.HandoffCloseTicks);
        var openLocal = plan.HandoffDate.ToDateTime(TimeOnly.MinValue).Add(openSpan);
        var closeLocal = plan.HandoffDate.ToDateTime(TimeOnly.MinValue).Add(closeSpan);
        var open = new DateTimeOffset(DateTime.SpecifyKind(openLocal, DateTimeKind.Unspecified), zone.GetUtcOffset(openLocal));
        var close = new DateTimeOffset(DateTime.SpecifyKind(closeLocal, DateTimeKind.Unspecified), zone.GetUtcOffset(closeLocal));
        return (open.ToUniversalTime(), close.ToUniversalTime());
    }

    /// <summary>Calendar day of <paramref name="nowUtc"/> in the accepted carrier zone.</summary>
    public static DateOnly LocalDate(DateTimeOffset nowUtc, string? timeZone)
    {
        var zone = ResolveZone(timeZone);
        var local = TimeZoneInfo.ConvertTime(nowUtc, zone);
        return DateOnly.FromDateTime(local.DateTime);
    }

    private static TimeZoneInfo ResolveZone(string? timeZone)
    {
        var id = string.IsNullOrWhiteSpace(timeZone) ? "UTC" : timeZone.Trim();
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static OrderAcceptedGroupDispatchPlanFact? EarliestPlan(
        IReadOnlyList<OrderAcceptedGroupDispatchPlanFact> plans) =>
        plans
            .OrderBy(plan => plan.HandoffDate)
            .ThenBy(plan => plan.HandoffOpenTicks)
            .ThenBy(plan => plan.ShippingGroupId)
            .FirstOrDefault();

    private static string Window(OrderAcceptedGroupDispatchPlanFact plan) =>
        $"{TimeSpan.FromTicks(plan.HandoffOpenTicks):hh\\:mm}–{TimeSpan.FromTicks(plan.HandoffCloseTicks):hh\\:mm}";
}
