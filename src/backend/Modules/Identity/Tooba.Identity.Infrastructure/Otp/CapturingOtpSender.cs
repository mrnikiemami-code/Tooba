using Tooba.Identity.Application;
using Tooba.Identity.Contracts;

namespace Tooba.Identity.Infrastructure.Otp;

/// <summary>
/// فرستندهٔ جعلی که کد را در حافظه نگه می‌دارد نه در لاگ.
/// </summary>
public sealed class CapturingOtpSender : IOtpSender
{
    /// <summary>
    /// آخرین کد برای تست؛ در تولید استفاده نشود.
    /// </summary>
    public string? LastCode { get; private set; }

    /// <summary>
    /// آخرین هدف.
    /// </summary>
    public OtpPurpose? LastPurpose { get; private set; }

    /// <inheritdoc />
    public Task SendAsync(OtpPurpose purpose, string destination, string oneTimeCode, CancellationToken cancellationToken)
    {
        LastPurpose = purpose;
        LastCode = oneTimeCode;
        return Task.CompletedTask;
    }
}
