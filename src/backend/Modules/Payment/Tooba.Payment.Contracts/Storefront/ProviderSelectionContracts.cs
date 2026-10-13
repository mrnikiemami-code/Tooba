namespace Tooba.Payment.Contracts.Storefront;

/// <summary>
/// نتیجهٔ تایپ‌شدهٔ رزولوشن انتخاب‌گر روش پرداخت: هم انتخاب‌گر اصلی مورداعتماد فراخوان و هم کد Provider
/// متعارف نهایی که Payment واقعاً استفاده می‌کند. هیچ قاعدهٔ کسب‌وکاری/providerی اینجا بازتعریف نمی‌شود.
/// </summary>
/// <param name="RequestedSelector">
/// انتخاب‌گر اصلی فراخوان بدون تغییر (برای ارسال دوباره به Payment تا بررسی صلاحیت معتبر همان authority
/// دست‌نخورده بماند). خالی یعنی روش مشخصی انتخاب نشده است.
/// </param>
/// <param name="CanonicalProviderCode">
/// کد Provider متعارف نهایی برای سیاست مهلت رزرو (۰۱۸)؛ باید با Provider نهایی Payment یکی باشد.
/// </param>
public sealed record StorefrontProviderSelection(
    string RequestedSelector,
    string CanonicalProviderCode);

/// <summary>
/// رزولوشن انتخاب‌گر روش پرداخت به Provider متعارف <b>پیش از</b> commit/آماده‌سازی رزرو.
/// پیاده‌سازی در مالکیت ماژول Payment و روی همان کاتالوگ درگاه (<c>IPaymentGatewayCatalogPort</c>) است که
/// مسیر شروع پرداخت استفاده می‌کند؛ ماژول‌های دیگر فقط Contracts را می‌بینند و هیچ سیاست/پیش‌فرض/مدتی
/// تکرار نمی‌شود. عدم امکان رزولوشن (انتخاب‌گر ناشناخته/مسدود/روش خاموش) با <c>null</c> امن‌بسته گزارش می‌شود
/// تا هیچ Provider جایگزینی «ساکت» انتخاب نشود.
/// </summary>
public interface IPaymentProviderSelectionPort
{
    /// <summary>
    /// انتخاب‌گر را به Provider متعارف حل می‌کند. <c>null</c> = امن‌بسته: نه commit، نه تمدید مهلت، نه پول.
    /// </summary>
    /// <param name="requestedProviderCode">انتخاب‌گر خام فراخوان (کد درگاه، <c>manual</c> یا خالی).</param>
    /// <param name="useWallet">آیا روش انتخاب‌شده کیف پول است (کد پایدار مستقل خودش).</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<StorefrontProviderSelection?> ResolveAsync(
        string? requestedProviderCode,
        bool useWallet,
        CancellationToken cancellationToken);
}
