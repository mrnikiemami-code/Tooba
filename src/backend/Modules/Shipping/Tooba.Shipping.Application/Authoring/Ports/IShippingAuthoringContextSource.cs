using Tooba.BuildingBlocks;

namespace Tooba.Shipping.Application.Authoring.Ports;

/// <summary>
/// درز Shipping به واقعیت‌های authoritative لازم برای زمینهٔ نویسندگی.
/// </summary>
/// <remarks>
/// پیاده‌سازی در Infrastructure همین ماژول است و فقط از Contracts ماژول‌های دیگر
/// (Localization برای زبان، StoreContext برای ارز) و زمینهٔ edition پلتفرم می‌خواند.
/// Application هیچ DbContext، هیچ Application/Domain خارجی و هیچ نوع HTTP نمی‌شناسد.
/// این درز هیچ زبان/ارز/حالتی را نمی‌سازد و هیچ نوشتنی ندارد.
/// </remarks>
public interface IShippingAuthoringContextSource
{
    /// <summary>
    /// واقعیت‌های خام authoritative را می‌خواند؛ اعتبارسنجی readiness در لایهٔ Application انجام می‌شود.
    /// </summary>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<ShippingAuthoringContextSourceSnapshot> ReadAsync(CancellationToken cancellationToken);
}

/// <summary>
/// واقعیت‌های خام زمینهٔ نویسندگی. مقادیر ناقص/تهی عمداً گزارش می‌شوند تا مصرف‌کننده fail-closed
/// کند؛ هیچ مقدار پیش‌فرضی (fa/en فرضی، ارز حدسی یا حالت ساختگی) اینجا تولید نمی‌شود.
/// </summary>
/// <param name="Edition">edition resolve‌شده، یا <see cref="ToobaEdition.Unset"/> وقتی نامعلوم است.</param>
/// <param name="DefaultCurrency">ارز پیش‌فرض فروشگاه، یا <c>null</c> وقتی context نامعتبر است.</param>
/// <param name="Languages">همهٔ زبان‌های شناخته‌شده با flag فعال‌بودن واقعی.</param>
public sealed record ShippingAuthoringContextSourceSnapshot(
    ToobaEdition Edition,
    string? DefaultCurrency,
    IReadOnlyList<ShippingAuthoringLanguageSource> Languages);

/// <summary>
/// یک زبان شناخته‌شده با metadata مالک. مقادیر nullable یعنی «منبع این واقعیت را نداده است»؛
/// هرگز به‌عنوان جهت/نام/ترتیب پیکربندی‌شدهٔ معتبر خوانده نمی‌شوند.
/// </summary>
/// <param name="LanguageId">شناسهٔ پایدار.</param>
/// <param name="Code">کد زبان.</param>
/// <param name="Culture">فرهنگ.</param>
/// <param name="UrlPrefix">پیشوند مسیر.</param>
/// <param name="IsActive">flag فعال‌بودن واقعی ردیف، یا <c>null</c> اگر منبع آن را نداده است.</param>
/// <param name="IsDefault">آیا این ردیف خود را پیش‌فرض اعلام کرده است.</param>
/// <param name="Direction">جهت نوشتار.</param>
/// <param name="DisplayName">نام نمایشی.</param>
/// <param name="NativeName">نام بومی.</param>
/// <param name="SortOrder">ترتیب اعلام‌شدهٔ مالک.</param>
public sealed record ShippingAuthoringLanguageSource(
    Guid LanguageId,
    string Code,
    string Culture,
    string UrlPrefix,
    bool? IsActive,
    bool IsDefault,
    string? Direction,
    string? DisplayName,
    string? NativeName,
    int? SortOrder);
