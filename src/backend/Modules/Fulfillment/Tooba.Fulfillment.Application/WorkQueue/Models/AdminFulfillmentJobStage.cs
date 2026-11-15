namespace Tooba.Fulfillment.Application.WorkQueue.Models;

/// <summary>
/// TB-271: the stable tokens of the five employee job stages plus the explicit archive. A stage is a READ label over
/// physical facts that already exist (the work-unit kind/state, the operations the owning aggregate already offers and
/// the accepted plan instants) — it is never an authorization and never a second lifecycle.
/// </summary>
public static class AdminFulfillmentJobStageNames
{
    /// <summary>«همه» — every active stage, the un-filtered selection token.</summary>
    public const string All = "all";

    /// <summary>آماده‌سازی و بسته‌بندی — processing/packing of THIS group's remaining quantity.</summary>
    public const string Preparation = "preparation";

    /// <summary>آمادهٔ ارسال — parcel allocation, the linked parcel's own plan, or a required tracking code.</summary>
    public const string ReadyToSend = "ready_to_send";

    /// <summary>تحویل به حامل — the carrier handoff the server already offers.</summary>
    public const string Handoff = "handoff";

    /// <summary>پیگیری تحویل — a dispatched/in-transit unit that still needs its delivery outcome.</summary>
    public const string Delivery = "delivery";

    /// <summary>برگشتی‌ها — actual return transit/receipt and the remaining source-owned decision.</summary>
    public const string Returns = "returns";

    /// <summary>سابقهٔ ارسال — terminal/no-obligation archive, outside the five active stages.</summary>
    public const string History = "history";

    /// <summary>The five active stages, in the operator's daily work order.</summary>
    public static readonly IReadOnlyList<string> Active =
        [Preparation, ReadyToSend, Handoff, Delivery, Returns];

    /// <summary>Every accepted stage token (the five + «همه»/archive).</summary>
    public static readonly IReadOnlyList<string> Known = [All, .. Active, History];

    /// <summary>Is this one of the five active job stages?</summary>
    public static bool IsActive(string? value) =>
        value is not null && Active.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>Normalises a wire token; an unknown token is NOT guessed into a stage.</summary>
    public static bool TryNormalize(string? value, out string stage)
    {
        stage = (value ?? string.Empty).Trim().ToLowerInvariant();
        return Known.Contains(stage, StringComparer.Ordinal);
    }
}

/// <summary>TB-271: the ONLY primary time scopes. They are computed server-side from the injected clock.</summary>
public static class AdminFulfillmentTimeScopeNames
{
    /// <summary>امروز (default) — past-due + due-today + honestly undated current work.</summary>
    public const string Today = "today";

    /// <summary>آینده — active work whose relevant known job time is a later day in its accepted zone.</summary>
    public const string Future = "future";

    /// <summary>همه — every active row, undated included.</summary>
    public const string All = "all";

    /// <summary>The accepted time-scope tokens.</summary>
    public static readonly IReadOnlyList<string> Known = [Today, Future, All];

    /// <summary>The default scope on a clean entry.</summary>
    public const string Default = Today;

    /// <summary>Normalises a wire token, defaulting an absent/unknown value to <see cref="Today"/>.</summary>
    public static string Normalize(string? value)
    {
        var token = (value ?? string.Empty).Trim().ToLowerInvariant();
        return Known.Contains(token, StringComparer.Ordinal) ? token : Default;
    }
}

/// <summary>TB-271: why the row's relevant time is what it is. One meaning per stage, never a merged blur.</summary>
public static class AdminFulfillmentJobTimeMeanings
{
    /// <summary>Latest instant at which preparation must START (not a packing-finish time).</summary>
    public const string PreparationStart = "preparation_start";

    /// <summary>Latest instant at which the parcel must leave the seller toward the carrier.</summary>
    public const string TransferDeparture = "transfer_departure";

    /// <summary>Accepted carrier acceptance close (half-open); NOT a booked appointment.</summary>
    public const string HandoffClose = "handoff_close";

    /// <summary>Active work whose accepted evidence carries no usable plan instant.</summary>
    public const string MissingPlan = "missing_plan";

    /// <summary>Delivery/return follow-up with no stored SLA: no invented hourly deadline.</summary>
    public const string OngoingFollowUp = "ongoing_followup";

    /// <summary>Terminal history: no job time at all.</summary>
    public const string None = "none";
}

/// <summary>TB-271: the row's position against the injected server now, in its accepted zone.</summary>
public static class AdminFulfillmentJobTimeStates
{
    /// <summary>The relevant instant is at/behind now (half-open: exactly at the cutoff is already late).</summary>
    public const string PastDue = "past_due";

    /// <summary>The relevant instant is still ahead, on the same local day.</summary>
    public const string DueToday = "due_today";

    /// <summary>A later local day.</summary>
    public const string Future = "future";

    /// <summary>No usable instant (missing plan, or a follow-up that stores no SLA).</summary>
    public const string Undated = "undated";
}

/// <summary>
/// TB-271: the classification of ONE queue row. <see cref="Stage"/> is the employee job, <see cref="JobTimeUtc"/> the
/// relevant persisted instant of THAT job (null when the fact does not exist), and <see cref="JobTimeState"/> the
/// server's own comparison with the injected clock.
/// </summary>
public sealed record AdminFulfillmentJobStageFacts(
    string Stage,
    string StageLabelFa,
    string JobTimeMeaning,
    string JobTimeLabelFa,
    DateTimeOffset? JobTimeUtc,
    string JobTimeState);

/// <summary>
/// TB-271 — ONE pure read-model classifier over the existing queue row facts. No IO, no ambient clock, no status
/// engine, no shipping policy: it reads the physical state, the operations the owning module already offers and the
/// accepted plan instants, and answers "which job is this row's next work, and when does that job matter".
/// </summary>
/// <remarks>
/// Precedence follows the ACTUAL next work (see <c>work-stage-mapping.md</c>): terminal/history → return work →
/// preparation → handoff → delivery → ready-for-send. Tracking correction never outranks a real handoff, and an
/// unplanned linked parcel shows planning (the job the server will not dispatch without) while keeping every
/// capability unchanged.
/// </remarks>
public static class AdminFulfillmentJobStageAuthority
{
    private const string LabelPreparation = "آماده‌سازی و بسته‌بندی";
    private const string LabelReadyToSend = "آمادهٔ ارسال";
    private const string LabelHandoff = "تحویل به حامل";
    private const string LabelDelivery = "پیگیری تحویل";
    private const string LabelReturns = "برگشتی‌ها";
    private const string LabelHistory = "سابقهٔ ارسال";

    private const string TimeLabelPreparation = "آخرین مهلت شروع آماده‌سازی";
    private const string TimeLabelTransfer = "آخرین مهلت خروج به سمت حامل";
    private const string TimeLabelHandoff = "آخرین مهلت تحویل به حامل";
    private const string TimeLabelMissingPlan = "موعد ارسال مشخص نشده";
    private const string TimeLabelOngoing = "پیگیری جاری؛ مهلت مشخصی ثبت نشده";
    private const string TimeLabelNone = "—";

    /// <summary>The Persian operator label of one stage token.</summary>
    public static string LabelFa(string stage) => stage switch
    {
        AdminFulfillmentJobStageNames.Preparation => LabelPreparation,
        AdminFulfillmentJobStageNames.ReadyToSend => LabelReadyToSend,
        AdminFulfillmentJobStageNames.Handoff => LabelHandoff,
        AdminFulfillmentJobStageNames.Delivery => LabelDelivery,
        AdminFulfillmentJobStageNames.Returns => LabelReturns,
        AdminFulfillmentJobStageNames.History => LabelHistory,
        _ => string.Empty,
    };

    /// <summary>
    /// Classifies ONE physical work unit. <paramref name="pendingDispatch"/> is the existing physical-work fact and
    /// <paramref name="nowUtc"/> is the injected server clock; nothing here reads settings or a browser clock.
    /// </summary>
    public static AdminFulfillmentJobStageFacts Classify(
        string workUnitKind,
        string workUnitState,
        IReadOnlyList<string> availableActionCodes,
        bool pendingDispatch,
        DateTimeOffset? preparationStartUtc,
        DateTimeOffset? transferDepartureUtc,
        DateTimeOffset? handoffDeadlineUtc,
        string? timeZone,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(availableActionCodes);
        var stage = ResolveStage(workUnitKind, workUnitState, availableActionCodes, pendingDispatch);
        return stage switch
        {
            AdminFulfillmentJobStageNames.Preparation => WithInstant(
                stage, AdminFulfillmentJobTimeMeanings.PreparationStart, TimeLabelPreparation,
                preparationStartUtc, timeZone, nowUtc),
            AdminFulfillmentJobStageNames.ReadyToSend => WithInstant(
                stage, AdminFulfillmentJobTimeMeanings.TransferDeparture, TimeLabelTransfer,
                transferDepartureUtc, timeZone, nowUtc),
            AdminFulfillmentJobStageNames.Handoff => WithInstant(
                stage, AdminFulfillmentJobTimeMeanings.HandoffClose, TimeLabelHandoff,
                handoffDeadlineUtc, timeZone, nowUtc),
            AdminFulfillmentJobStageNames.Delivery or AdminFulfillmentJobStageNames.Returns => new AdminFulfillmentJobStageFacts(
                stage,
                LabelFa(stage),
                AdminFulfillmentJobTimeMeanings.OngoingFollowUp,
                TimeLabelOngoing,
                null,
                AdminFulfillmentJobTimeStates.Undated),
            _ => new AdminFulfillmentJobStageFacts(
                AdminFulfillmentJobStageNames.History,
                LabelHistory,
                AdminFulfillmentJobTimeMeanings.None,
                TimeLabelNone,
                null,
                AdminFulfillmentJobTimeStates.Undated),
        };
    }

    /// <summary>The stage token of one physical work unit (the precedence in <c>work-stage-mapping.md</c> §2).</summary>
    public static string ResolveStage(
        string workUnitKind,
        string workUnitState,
        IReadOnlyList<string> availableActionCodes,
        bool pendingDispatch)
    {
        ArgumentNullException.ThrowIfNull(availableActionCodes);
        var kind = (workUnitKind ?? string.Empty).Trim();
        var state = (workUnitState ?? string.Empty).Trim();
        var codes = availableActionCodes;
        var hasForwardCode = codes.Count > 0;

        // 1) Terminal first: the projection's own "no open physical unit" shape, a finished state, or nothing forward.
        if (string.Equals(kind, AdminFulfillmentWorkUnitKinds.Fulfillment, StringComparison.OrdinalIgnoreCase)
            || IsTerminalState(state)
            || (!pendingDispatch && !hasForwardCode))
        {
            return AdminFulfillmentJobStageNames.History;
        }

        // 2) Actual return transit / receipt work, or a received source that STILL owns a return decision. A returned
        // parcel whose usable capacity and damaged decision are fully settled is history, not active return work —
        // its status alone must never keep it in the employee's queue.
        if (IsReturnInTransitState(state)
            || Has(codes, "start_return", "record_return_receipt")
            || (string.Equals(state, "Returned", StringComparison.OrdinalIgnoreCase)
                && Has(codes, "resend_shipment", "resolve_damaged_goods", "create_courier_return")))
        {
            return AdminFulfillmentJobStageNames.Returns;
        }

        // 3) Processing/packing of this row's OWN remaining quantity.
        if (Has(codes, "mark_processing", "mark_packed"))
        {
            return AdminFulfillmentJobStageNames.Preparation;
        }

        // 4) The carrier handoff the server already offers. A package without tracking still owes its tracking code,
        // so its dispatch code alone is not yet "all owning prerequisites present".
        var packageNeedsTracking = Has(codes, "assign_consolidated_package_tracking");
        if (Has(codes, "dispatch_shipment") || (Has(codes, "dispatch_consolidated_package") && !packageNeedsTracking))
        {
            return AdminFulfillmentJobStageNames.Handoff;
        }

        // 5) Already out of the warehouse: the remaining work is its delivery outcome.
        if (IsDeliveryState(state)
            || Has(codes, "deliver_shipment", "deliver_consolidated_package", "record_non_delivery"))
        {
            return AdminFulfillmentJobStageNames.Delivery;
        }

        // 6) Allocation from packed work, the linked parcel's OWN plan, or the tracking code it still needs.
        if (Has(
                codes,
                "create_shipment",
                "plan_shipment_dispatch",
                "assign_tracking",
                "correct_tracking",
                "assign_consolidated_package_tracking")
            || string.Equals(state, "Created", StringComparison.OrdinalIgnoreCase))
        {
            return AdminFulfillmentJobStageNames.ReadyToSend;
        }

        return AdminFulfillmentJobStageNames.History;
    }

    /// <summary>Does a row's own time state belong to the requested time scope?</summary>
    public static bool MatchesTimeScope(string jobTimeState, string? timeScope) =>
        AdminFulfillmentTimeScopeNames.Normalize(timeScope) switch
        {
            AdminFulfillmentTimeScopeNames.Future =>
                string.Equals(jobTimeState, AdminFulfillmentJobTimeStates.Future, StringComparison.Ordinal),
            AdminFulfillmentTimeScopeNames.All => true,
            // «امروز» carries the overdue work AND the honestly undated current work; a later day is excluded.
            _ => !string.Equals(jobTimeState, AdminFulfillmentJobTimeStates.Future, StringComparison.Ordinal),
        };

    /// <summary>Does a row's stage belong to the requested stage selection?</summary>
    public static bool MatchesStage(string jobStage, string? selectedStage)
    {
        var token = (selectedStage ?? string.Empty).Trim().ToLowerInvariant();
        return token.Length == 0
            || string.Equals(token, AdminFulfillmentJobStageNames.All, StringComparison.Ordinal)
            || string.Equals(jobStage, token, StringComparison.Ordinal);
    }

    /// <summary>
    /// Default queue ordering: past-due first (earliest relevant instant), then due-today, then the explicit undated
    /// current work, then future work by its own stored instant. The work-unit key is the stable tie-break.
    /// </summary>
    public static int PriorityRank(string jobTimeState) => jobTimeState switch
    {
        AdminFulfillmentJobTimeStates.PastDue => 0,
        AdminFulfillmentJobTimeStates.DueToday => 1,
        AdminFulfillmentJobTimeStates.Undated => 2,
        AdminFulfillmentJobTimeStates.Future => 3,
        _ => 4,
    };

    private static AdminFulfillmentJobStageFacts WithInstant(
        string stage,
        string meaning,
        string label,
        DateTimeOffset? instant,
        string? timeZone,
        DateTimeOffset nowUtc)
    {
        if (instant is null)
        {
            // The accepted evidence carries no usable operational instant for THIS job: say so, never guess.
            return new AdminFulfillmentJobStageFacts(
                stage,
                LabelFa(stage),
                AdminFulfillmentJobTimeMeanings.MissingPlan,
                TimeLabelMissingPlan,
                null,
                AdminFulfillmentJobTimeStates.Undated);
        }

        return new AdminFulfillmentJobStageFacts(
            stage,
            LabelFa(stage),
            meaning,
            label,
            instant,
            StateOf(instant.Value, timeZone, nowUtc));
    }

    private static string StateOf(DateTimeOffset instant, string? timeZone, DateTimeOffset nowUtc)
    {
        // Half-open: exactly at the accepted cutoff is already late.
        if (instant <= nowUtc)
        {
            return AdminFulfillmentJobTimeStates.PastDue;
        }

        var instantDay = AdminFulfillmentDispatchTimingAuthority.LocalDate(instant, timeZone);
        var today = AdminFulfillmentDispatchTimingAuthority.LocalDate(nowUtc, timeZone);
        return instantDay <= today
            ? AdminFulfillmentJobTimeStates.DueToday
            : AdminFulfillmentJobTimeStates.Future;
    }

    private static bool IsTerminalState(string state) =>
        state is "Delivered" or "Cancelled" or "Failed";

    private static bool IsReturnInTransitState(string state) =>
        state is "NonDelivered" or "Returning";

    private static bool IsDeliveryState(string state) =>
        state is "Dispatched" or "InTransit";

    private static bool Has(IReadOnlyList<string> codes, params string[] wanted)
    {
        foreach (var code in wanted)
        {
            if (codes.Contains(code, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
