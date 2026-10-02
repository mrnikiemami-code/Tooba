namespace Tooba.Identity.Application.Models;

/// <summary>
/// رخداد امنیتی بدون payload محرمانه.
/// </summary>
public sealed class IdentitySecurityEvent
{
    /// <summary>
    /// نام رخداد مانند login_success.
    /// </summary>
    public required string EventName { get; init; }

    /// <summary>
    /// User در صورت شناخته بودن.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// زمان رخداد.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }
}
