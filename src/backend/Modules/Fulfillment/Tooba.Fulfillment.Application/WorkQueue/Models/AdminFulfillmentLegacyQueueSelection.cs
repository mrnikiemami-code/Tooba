namespace Tooba.Fulfillment.Application.WorkQueue.Models;

/// <summary>
/// TB-271: the ONE documented mapping from an old <c>queueFilter</c> token to the new (job stage, time scope)
/// selection. The legacy token is never applied as a second hidden filter: it is translated once, so an old link
/// keeps showing a meaningful, explainable queue instead of silently combining with — or contradicting — the
/// visible stage/time controls. An unknown token clears to the clean default (all stages, today).
/// </summary>
public static class AdminFulfillmentLegacyQueueSelection
{
    /// <summary>Translates one legacy quick-filter token into the new selection.</summary>
    /// <remarks>
    /// Deliberate outcomes:
    /// <list type="bullet">
    /// <item>processing/packing tokens → «آماده‌سازی و بسته‌بندی» over every time scope (the work is the stage, not a day);</item>
    /// <item>shipping/tracking tokens → «آمادهٔ ارسال» over every time scope;</item>
    /// <item>in-transit → «پیگیری تحویل» over every time scope;</item>
    /// <item>delivered/problem → the explicit archive (history) over every time scope;</item>
    /// <item>timing tokens (<c>dispatch_*</c>) and «همه»/«نیازمند اقدام» → all stages, «امروز» (the live default work);
    /// the row's own job time still shows that it is overdue, and the default order still puts it first.</item>
    /// </list>
    /// </remarks>
    public static (string Stage, string TimeScope) Map(string? legacyToken)
    {
        var token = (legacyToken ?? string.Empty).Trim().ToLowerInvariant();
        return token switch
        {
            AdminFulfillmentQueueFilters.ReadyToProcess => (AdminFulfillmentJobStageNames.Preparation, AdminFulfillmentTimeScopeNames.All),
            AdminFulfillmentQueueFilters.ReadyToPack => (AdminFulfillmentJobStageNames.Preparation, AdminFulfillmentTimeScopeNames.All),
            AdminFulfillmentQueueFilters.ReadyToShip => (AdminFulfillmentJobStageNames.ReadyToSend, AdminFulfillmentTimeScopeNames.All),
            AdminFulfillmentQueueFilters.MissingTracking => (AdminFulfillmentJobStageNames.ReadyToSend, AdminFulfillmentTimeScopeNames.All),
            AdminFulfillmentQueueFilters.InTransit => (AdminFulfillmentJobStageNames.Delivery, AdminFulfillmentTimeScopeNames.All),
            AdminFulfillmentQueueFilters.Delivered => (AdminFulfillmentJobStageNames.History, AdminFulfillmentTimeScopeNames.All),
            AdminFulfillmentQueueFilters.Problem => (AdminFulfillmentJobStageNames.History, AdminFulfillmentTimeScopeNames.All),
            _ => (AdminFulfillmentJobStageNames.All, AdminFulfillmentTimeScopeNames.Default),
        };
    }
}
