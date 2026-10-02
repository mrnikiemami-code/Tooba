namespace Tooba.Identity.Application.Options;

/// <summary>
/// عمر نشست، چالش و حد تلاش. antifraud تجاری اینجا نیست.
/// </summary>
public sealed class IdentityLifecycleOptions
{
    /// <summary>
    /// عمر Refresh نشست به ساعت.
    /// </summary>
    public int SessionLifetimeHours { get; set; } = 336;

    /// <summary>
    /// عمر چالش OTP/بازنشانی به دقیقه.
    /// </summary>
    public int ChallengeLifetimeMinutes { get; set; } = 15;

    /// <summary>
    /// حداکثر تلاش نادرست روی یک چالش قبل از قفل همان چالش.
    /// </summary>
    public int MaxChallengeAttempts { get; set; } = 5;
}
