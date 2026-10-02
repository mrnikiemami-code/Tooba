namespace Tooba.Identity.Domain.Enums;

/// <summary>
/// گونهٔ عامل MFA آینده. سیاست MFA نقش کسب‌وکار نیست.
/// </summary>
public enum MfaFactorKind
{
    /// <summary>
    /// چالش یک‌بارمصرف پیامکی/ایمیلی به‌عنوان عامل دوم.
    /// </summary>
    Otp = 1,

    /// <summary>
    /// رمز زمان‌محور Authenticator.
    /// </summary>
    Totp = 2,

    /// <summary>
    /// Passkey / WebAuthn.
    /// </summary>
    WebAuthn = 3,

    /// <summary>
    /// ارتقای جلسه از طریق IdP خارجی.
    /// </summary>
    ExternalIdpStepUp = 4,
}
