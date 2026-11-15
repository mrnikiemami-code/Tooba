namespace Tooba.Fulfillment.Application.WorkQueue.Models;

/// <summary>
/// TB-271 — the ONE count authority of the employee queue. It reads the SAME classified rows the page is cut from, so
/// a stage badge can never be tallied from one fetched page and a time badge can never describe a different set.
/// </summary>
/// <remarks>
/// <para>Badge dimensions are deliberate and explicit:</para>
/// <list type="bullet">
/// <item>stage badge = whole ACTIVE set inside the SELECTED time scope, before the selected stage;</item>
/// <item>time badge = the SELECTED active stage, before the selected scope (0 on the archive, which has no time);</item>
/// <item>page total = both selections, cut from the SAME predicate as <c>SelectedTotal</c>;</item>
/// <item>archive (history) = time-independent, reachable ONLY through the explicit history token, and excluded
/// from every active total;</item>
/// <item>undated = the explicit «موعد مشخص نشده» subset of the current selection.</item>
/// </list>
/// </remarks>
public static class AdminFulfillmentWorkQueueCountsBuilder
{
    /// <summary>Builds every badge from one already-classified set.</summary>
    /// <param name="rows">The classified rows AFTER search/column filters, BEFORE stage/time selection.</param>
    /// <param name="selectedStage">The selected stage token (unknown/empty = «همه»).</param>
    /// <param name="selectedTimeScope">The selected time scope token (unknown/empty = «امروز»).</param>
    public static AdminFulfillmentWorkQueueCounts Build(
        IReadOnlyList<AdminFulfillmentWorkQueueRow> rows,
        string? selectedStage,
        string? selectedTimeScope)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var stage = NormalizeStage(selectedStage);
        var scope = AdminFulfillmentTimeScopeNames.Normalize(selectedTimeScope);
        var active = rows.Where(IsActive).ToList();
        var history = rows.Where(row => !IsActive(row)).ToList();
        var inScope = active.Where(row => InScope(row, scope)).ToList();

        return new AdminFulfillmentWorkQueueCounts(
            // TB-260 legacy fields keep their original, page-independent meaning over the classified set.
            TotalActive: active.Count,
            Due: active.Count(row => IsDue(row)),
            Today: active.Count(row => row.DispatchBucket == AdminFulfillmentDispatchBucketNames.Today),
            Overdue: active.Count(row => row.DispatchBucket == AdminFulfillmentDispatchBucketNames.Overdue),
            Upcoming: active.Count(row => row.DispatchBucket == AdminFulfillmentDispatchBucketNames.Upcoming),
            Unspecified: active.Count(row => row.DispatchBucket == AdminFulfillmentDispatchBucketNames.Unspecified),
            InTransit: active.Count(row => row.DispatchBucket == AdminFulfillmentDispatchBucketNames.InTransit),
            PreparationDue: active.Count(row => row.PreparationState is AdminFulfillmentPreparationStateNames.Overdue
                or AdminFulfillmentPreparationStateNames.HandoffMissed),
            StageAll: inScope.Count,
            StagePreparation: CountStage(inScope, AdminFulfillmentJobStageNames.Preparation),
            StageReadyToSend: CountStage(inScope, AdminFulfillmentJobStageNames.ReadyToSend),
            StageHandoff: CountStage(inScope, AdminFulfillmentJobStageNames.Handoff),
            StageDelivery: CountStage(inScope, AdminFulfillmentJobStageNames.Delivery),
            StageReturns: CountStage(inScope, AdminFulfillmentJobStageNames.Returns),
            StageHistory: history.Count,
            TimeToday: CountTime(Selectable(active, stage), AdminFulfillmentTimeScopeNames.Today),
            TimeFuture: CountTime(Selectable(active, stage), AdminFulfillmentTimeScopeNames.Future),
            TimeAll: Selectable(active, stage).Count,
            Undated: Selectable(active, stage)
                .Count(row => InScope(row, scope)
                    && string.Equals(row.JobTimeState, AdminFulfillmentJobTimeStates.Undated, StringComparison.Ordinal)),
            // TB-271-R1: the page total is EXACTLY the rows the page is cut from — the same predicate as Select.
            // Deriving it from the active-only helpers made the archive tab claim «۰ ردیف» while rendering its rows.
            SelectedTotal: rows.Count(row => InSelection(row, stage, scope)),
            SelectedStage: stage,
            SelectedTimeScope: scope);
    }

    /// <summary>The rows the CURRENT (stage + time) selection contains — the page is cut from exactly this sequence.</summary>
    public static IEnumerable<AdminFulfillmentWorkQueueRow> Select(
        IReadOnlyList<AdminFulfillmentWorkQueueRow> rows,
        string? selectedStage,
        string? selectedTimeScope)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var stage = NormalizeStage(selectedStage);
        var scope = AdminFulfillmentTimeScopeNames.Normalize(selectedTimeScope);
        return rows.Where(row => InSelection(row, stage, scope));
    }

    /// <summary>The applied stage token (an unknown/empty token falls back to «همه», never to a guessed stage).</summary>
    public static string NormalizeStage(string? selectedStage) =>
        AdminFulfillmentJobStageNames.TryNormalize(selectedStage, out var stage)
            ? stage
            : AdminFulfillmentJobStageNames.All;

    /// <summary>History (the archive) is the only row state outside the five active job stages.</summary>
    public static bool IsActive(AdminFulfillmentWorkQueueRow row) =>
        !string.Equals(row.JobStage, AdminFulfillmentJobStageNames.History, StringComparison.Ordinal);

    /// <summary>
    /// TB-271-R1: the ONE exact predicate behind the page, <c>SelectedTotal</c>, the undated count and the time
    /// badges. An active view (including «همه») contains ACTIVE rows only inside the selected time scope; the explicit
    /// archive selection contains terminal rows only and is time-independent.
    /// </summary>
    private static bool InSelection(AdminFulfillmentWorkQueueRow row, string stage, string scope)
    {
        if (string.Equals(stage, AdminFulfillmentJobStageNames.History, StringComparison.Ordinal))
        {
            // The archive is exactly the terminal subset, whatever time scope the client happens to echo.
            return !IsActive(row);
        }

        if (!IsActive(row))
        {
            // No terminal row can ever sit on an active page or inside an active total.
            return false;
        }

        if (!AdminFulfillmentJobStageAuthority.MatchesStage(row.JobStage, stage))
        {
            return false;
        }

        return InScope(row, scope);
    }

    private static bool InScope(AdminFulfillmentWorkQueueRow row, string scope) =>
        AdminFulfillmentJobStageAuthority.MatchesTimeScope(row.JobTimeState, scope);

    /// <summary>
    /// The rows the TIME badges and the page describe: the SELECTED active stage, before the selected time scope.
    /// The archive has no time dimension (its control is disabled and its rows carry no job time), so all three time
    /// badges are honestly 0 there instead of borrowing the terminal count.
    /// </summary>
    private static List<AdminFulfillmentWorkQueueRow> Selectable(
        List<AdminFulfillmentWorkQueueRow> active,
        string stage)
    {
        if (string.Equals(stage, AdminFulfillmentJobStageNames.History, StringComparison.Ordinal))
        {
            return [];
        }

        if (string.Equals(stage, AdminFulfillmentJobStageNames.All, StringComparison.Ordinal))
        {
            return active;
        }

        return active.Where(row => string.Equals(row.JobStage, stage, StringComparison.Ordinal)).ToList();
    }

    private static int CountStage(List<AdminFulfillmentWorkQueueRow> rows, string stage) =>
        rows.Count(row => string.Equals(row.JobStage, stage, StringComparison.Ordinal));

    private static int CountTime(List<AdminFulfillmentWorkQueueRow> rows, string scope) =>
        rows.Count(row => InScope(row, scope));

    /// <summary>TB-260 «سررسیدشده»: preparation already due, or a today/overdue accepted window that has opened.</summary>
    private static bool IsDue(AdminFulfillmentWorkQueueRow row) =>
        row.PreparationState is AdminFulfillmentPreparationStateNames.Overdue
            or AdminFulfillmentPreparationStateNames.HandoffMissed
        || row.DispatchBucket is AdminFulfillmentDispatchBucketNames.Today or AdminFulfillmentDispatchBucketNames.Overdue
            && row.HandoffWindowState is AdminFulfillmentDispatchBucketNames.WindowOpen
                or AdminFulfillmentDispatchBucketNames.WindowClosed;
}
