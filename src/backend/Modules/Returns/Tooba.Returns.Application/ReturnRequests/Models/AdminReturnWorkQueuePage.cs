namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// TB-275: one requested line of a case, as the operator must see it: its OWN product identity, quantity and unit.
/// Lines are never summed into the first item's unit, because incompatible units would then read as one number.
/// </summary>
public sealed record AdminReturnWorkQueueLine(
    string ProductLabel,
    decimal Quantity,
    string UnitLabel,
    string Condition);

/// <summary>
/// TB-275: the fast work-strip counts. They are counted from the SAME constrained classification the page is cut from
/// (before paging), so a badge can never disagree with the rows it selects. <see cref="AllOpen"/> is the active set and
/// <see cref="History"/> is the closed/settled set; the stage counts are disjoint subsets of the active set and the
/// same case may therefore appear under two different questions only when those questions genuinely overlap (a
/// pending refund is system work, not an operator notification).
/// </summary>
public sealed record AdminReturnWorkCounts(
    int AllOpen,
    int Receiving,
    int Decision,
    int Refund,
    int RefundFailed,
    int Disposition,
    int History);

/// <summary>
/// TB-275: one page of the admin return work queue plus the whole-set counts it was cut from. Transport-neutral; the
/// wire shape keeps the shared grid members (<c>items</c>/<c>page</c>/<c>pageSize</c>/<c>totalCount</c>) and adds the
/// Returns-owned <c>counts</c>.
/// </summary>
public sealed record AdminReturnWorkQueuePage(
    IReadOnlyList<AdminReturnWorkQueueRow> Items,
    int Page,
    int PageSize,
    int TotalCount,
    AdminReturnWorkCounts Counts);

/// <summary>TB-275: the canonical work-strip tokens the queue selector accepts.</summary>
public static class AdminReturnWorkQueueSelection
{
    /// <summary>All cases that still own open work (the default).</summary>
    public const string Open = "open";

    /// <summary>Closed/settled cases.</summary>
    public const string History = "history";

    /// <summary>Every accepted token, including the stage codes.</summary>
    public static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        Open,
        History,
        ReturnWorkCategoryCodes.Receiving,
        ReturnWorkCategoryCodes.Decision,
        ReturnWorkCategoryCodes.Refund,
        ReturnWorkCategoryCodes.RefundFailed,
        ReturnWorkCategoryCodes.Disposition,
        ReturnWorkCategoryCodes.Settled,
    };

    /// <summary>
    /// TB-275: one canonical selection token from any legacy queue token or new stage token. An unrecognized token is
    /// NEVER read as "everything": it falls back to the honest active set, which is what the operator's default strip
    /// shows.
    /// </summary>
    public static string Canonicalize(string? raw)
    {
        var token = raw?.Trim().ToLowerInvariant() ?? string.Empty;
        return token switch
        {
            "" => Open,
            "all" => Open,

            // Legacy fast filters, translated to the stage they actually mean.
            "pending_review" => ReturnWorkCategoryCodes.Decision,
            "approved" => ReturnWorkCategoryCodes.Refund,
            "refund_needed" => ReturnWorkCategoryCodes.Refund,
            "refund_pending" => ReturnWorkCategoryCodes.Refund,
            "refund_failed" => ReturnWorkCategoryCodes.RefundFailed,
            "completed" => History,
            "rejected" => History,

            _ => Known.Contains(token) ? token : Open,
        };
    }

    /// <summary>True when the work description matches the canonical selection token.</summary>
    public static bool Matches(ReturnWorkDescription work, string canonicalToken) =>
        canonicalToken switch
        {
            History => !work.IsOpen,
            ReturnWorkCategoryCodes.Receiving => work.Category == ReturnWorkCategory.Receiving,
            ReturnWorkCategoryCodes.Decision => work.Category == ReturnWorkCategory.Decision,
            ReturnWorkCategoryCodes.Refund => work.Category == ReturnWorkCategory.Refund,
            ReturnWorkCategoryCodes.RefundFailed => work.Category == ReturnWorkCategory.RefundFailed,
            ReturnWorkCategoryCodes.Disposition => work.Category == ReturnWorkCategory.Disposition,
            ReturnWorkCategoryCodes.Settled => work.Category == ReturnWorkCategory.Settled,
            _ => work.IsOpen,
        };
}
