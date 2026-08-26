using Tooba.Identity.Application;
using Tooba.Identity.Domain;

namespace Tooba.Identity.Infrastructure;

/// <summary>
/// Production OTP sender placeholder: OTP delivery must be wired via external provider.
/// Fail-closed — does not silently capture codes in memory.
/// </summary>
public sealed class ProductionOtpSender : IOtpSender
{
    /// <inheritdoc />
    public Task SendAsync(OtpPurpose purpose, string destination, string oneTimeCode, CancellationToken cancellationToken) =>
        Task.FromException<InvalidOperationException>(
            new InvalidOperationException("identity.otp.delivery.unconfigured"));
}
