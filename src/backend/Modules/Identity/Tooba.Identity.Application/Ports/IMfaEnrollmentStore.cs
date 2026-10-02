using Tooba.Identity.Domain.Enums;

namespace Tooba.Identity.Application.Ports;

/// <summary>
/// درز ثبت عامل MFA بدون UI.
/// </summary>
public interface IMfaEnrollmentStore
{
    /// <summary>
    /// عامل را برای User علامت می‌زند.
    /// </summary>
    Task EnrollAsync(Guid userId, MfaFactorKind factorKind, CancellationToken cancellationToken);

    /// <summary>
    /// عوامل فعال را می‌خواند.
    /// </summary>
    Task<IReadOnlyList<MfaFactorKind>> ListEnabledAsync(Guid userId, CancellationToken cancellationToken);
}
