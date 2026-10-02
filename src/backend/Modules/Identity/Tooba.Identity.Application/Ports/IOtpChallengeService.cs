using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;

namespace Tooba.Identity.Application.Ports;

/// <summary>
/// سرویس چالش OTP مستقل از ارائه‌دهنده.
/// </summary>
public interface IOtpChallengeService
{
    /// <summary>
    /// چالش جدید برای هدف مشخص می‌سازد و از طریق <see cref="IOtpSender"/> می‌فرستد.
    /// </summary>
    Task<OtpChallengeHandle> IssueAsync(OtpPurpose purpose, string destination, CancellationToken cancellationToken);

    /// <summary>
    /// کد را می‌سنجد. plaintext کد persist بلندمدت نمی‌شود.
    /// </summary>
    Task<bool> VerifyAsync(OtpChallengeHandle handle, string oneTimeCode, CancellationToken cancellationToken);
}
