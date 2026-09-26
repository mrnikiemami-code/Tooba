namespace Tooba.Host;

/// <summary>
/// درز محدودسازی نرخ auth-sensitive. هویت را فقط به IP گره نمی‌زند.
/// </summary>
internal interface IAuthenticationThrottleSeam
{
    /// <summary>
    /// تلاش برای مصرف یک permit در پنجرهٔ IP+operation. false یعنی 429 enumeration-safe.
    /// </summary>
    bool TryAcquire(HttpContext context, string operation);
}
