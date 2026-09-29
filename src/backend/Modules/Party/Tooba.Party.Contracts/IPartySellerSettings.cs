namespace Tooba.Party.Contracts;

/// <summary>
/// درز پایدار خواندن/نوشتن پروفایل عملیاتی Organization برای پنل فروشنده.
/// <para>
/// مالکیت کسب‌وکار پروفایل سازمانی Party است؛ این قرارداد فقط نمایهٔ بدون credential ورود را
/// عبور می‌دهد. Actor مبدأ مجوز نیست؛ مجوز مؤثر فروشنده در مرز پلتفرم امنیت Host بررسی می‌شود.
/// </para>
/// </summary>
public interface IPartySellerSettings
{
    /// <summary>
    /// نمایهٔ عملیاتی Organization را می‌خواند؛ برای Person یا نبود Party تهی است.
    /// </summary>
    Task<PartySellerSettingsSnapshot?> GetAsync(Guid partyId, CancellationToken cancellationToken);

    /// <summary>
    /// نمایهٔ عملیاتی Organization را به‌روز می‌کند؛ Person مقصد رد می‌شود.
    /// </summary>
    Task<PartySellerSettingsSnapshot> UpdateAsync(
        Guid partyId,
        PartySellerSettingsWrite input,
        CancellationToken cancellationToken);
}

/// <summary>
/// نمایهٔ پایدار پروفایل عملیاتی Organization بدون credential ورود؛ مالکیت Party.
/// </summary>
public sealed record PartySellerSettingsSnapshot(
    Guid PartyId,
    string DisplayName,
    string? LegalName,
    string? Description,
    string? SupportPhone,
    string? SupportEmail,
    string? AddressLine,
    DateTimeOffset UpdatedAt);

/// <summary>
/// ورودی پایدار نوشتن پروفایل عملیاتی Organization؛ مالکیت Party.
/// </summary>
public sealed record PartySellerSettingsWrite(
    string DisplayName,
    string? LegalName,
    string? Description,
    string? SupportPhone,
    string? SupportEmail,
    string? AddressLine);
