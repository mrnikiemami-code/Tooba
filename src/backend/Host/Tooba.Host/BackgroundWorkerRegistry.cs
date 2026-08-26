using System.Collections.Concurrent;

namespace Tooba.Host;

/// <summary>
/// وضعیت آخرین اجرای کارگرهای پس‌زمینه برای لاگ/عملیات. اسکن DB انجام نمی‌دهد.
/// </summary>
internal sealed class BackgroundWorkerRegistry
{
    private readonly ConcurrentDictionary<string, BackgroundWorkerRunState> _states = new(StringComparer.Ordinal);

    /// <summary>
    /// موفقیت یک دور کارگر را ثبت می‌کند.
    /// </summary>
    public void RecordSuccess(string workerName, int processedCount)
    {
        _states.AddOrUpdate(
            workerName,
            _ => new BackgroundWorkerRunState(DateTimeOffset.UtcNow, null, processedCount, null),
            (_, existing) => existing with
            {
                LastSuccessUtc = DateTimeOffset.UtcNow,
                LastProcessedCount = processedCount,
                LastErrorType = null,
            });
    }

    /// <summary>
    /// شکست یک دور کارگر را ثبت می‌کند.
    /// </summary>
    public void RecordFailure(string workerName, string errorType)
    {
        _states.AddOrUpdate(
            workerName,
            _ => new BackgroundWorkerRunState(null, DateTimeOffset.UtcNow, 0, errorType),
            (_, existing) => existing with
            {
                LastFailureUtc = DateTimeOffset.UtcNow,
                LastErrorType = errorType,
            });
    }

    /// <summary>
    /// وضعیت ثبت‌شدهٔ یک کارگر را برمی‌گرداند.
    /// </summary>
    public BackgroundWorkerRunState? GetState(string workerName) =>
        _states.TryGetValue(workerName, out var state) ? state : null;
}

/// <summary>
/// آخرین نتیجهٔ poll یک کارگر پس‌زمینه.
/// </summary>
internal sealed record BackgroundWorkerRunState(
    DateTimeOffset? LastSuccessUtc,
    DateTimeOffset? LastFailureUtc,
    int LastProcessedCount,
    string? LastErrorType);
