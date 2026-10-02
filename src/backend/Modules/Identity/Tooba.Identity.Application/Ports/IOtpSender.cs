using Tooba.Identity.Contracts;

namespace Tooba.Identity.Application.Ports;

/// <summary>
/// فرستندهٔ OTP. ارائه‌دهندهٔ واقعی SMS/ایمیل اینجا وصل نمی‌شود.
/// </summary>
public interface IOtpSender
{
    /// <summary>
    /// چالش را به کانال مقصد می‌فرستد. کد OTP نباید لاگ شود.
    /// </summary>
    Task SendAsync(OtpPurpose purpose, string destination, string oneTimeCode, CancellationToken cancellationToken);
}
