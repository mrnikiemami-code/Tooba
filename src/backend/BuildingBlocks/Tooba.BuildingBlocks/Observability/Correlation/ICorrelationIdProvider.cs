namespace Tooba.BuildingBlocks.Observability.Correlation;

/// <summary>
/// قرارداد چرخهٔ حیات CorrelationId برای Application و Host.
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>شناسه همبستگی جاری با فرمت N.</summary>
    string? GetCorrelationId();

    /// <summary>همان شناسه به‌صورت <see cref="Guid"/>.</summary>
    Guid? GetCorrelationGuid();

    /// <summary>
    /// وجود شناسه را تضمین می‌کند: مقدار فعلی، incoming معتبر، یا تولید پایدار برای جریان async.
    /// </summary>
    string EnsureCorrelationId(string? incoming = null);

    /// <summary>شناسه معتبر را پس از نرمال‌سازی تنظیم می‌کند.</summary>
    void SetCorrelationId(string correlationId);
}
