namespace Tooba.Shipping.Contracts.Rates;

/// <summary>
/// وضعیت صریح capture انتخاب‌شده برای یک سرویس/گزینه؛ هیچ حالت ضمنی «جدیدترین» وجود ندارد.
/// </summary>
public enum ShippingSelectedCaptureReadState
{
    /// <summary>هیچ اشاره‌گری برای این مالک + Binding ذخیره نشده است.</summary>
    Absent = 0,

    /// <summary>اشاره‌گر وجود دارد و صریحاً هیچ capture‌ای انتخاب نشده است.</summary>
    Cleared = 1,

    /// <summary>یک capture تغییرناپذیر دقیق انتخاب شده است.</summary>
    Selected = 2,
}

/// <summary>نتیجهٔ خواندن انتخاب؛ هر مقدار غیر از <see cref="Selected"/> هیچ provenance ندارد.</summary>
public enum ShippingSelectedCaptureVerdict
{
    /// <summary>یک capture مشخص انتخاب شده و متادیتای همان capture برگشت.</summary>
    Selected = 0,

    /// <summary>اشاره‌گر پاک‌شده است؛ هیچ fallback به جدیدترین/پیکربندی وجود ندارد.</summary>
    Cleared = 1,

    /// <summary>اشاره‌گری ثبت نشده است.</summary>
    Absent = 2,

    /// <summary>هویت مالک درخواستی با زمینهٔ نصب/تجارت جاری نمی‌خواند.</summary>
    OwnerScopeUnavailable = 3,

    /// <summary>capture انتخاب‌شده موجود نیست، مالک دیگر است یا payload آن ناسازگار است.</summary>
    SnapshotUnavailable = 4,

    /// <summary>درخواست شکل معتبر ندارد (Binding خالی)؛ هیچ اشاره‌گری خوانده نشد.</summary>
    InvalidRequest = 5,
}

/// <summary>کدهای پایدار دلیل امتناع خواندن انتخاب.</summary>
public static class ShippingSelectedCaptureVerdictCodes
{
    /// <summary>هویت مالک قابل استفاده نیست.</summary>
    public const string OwnerScopeUnavailable = "shipping.selected_capture.owner_scope_unavailable";

    /// <summary>capture انتخاب‌شده قابل اثبات نیست.</summary>
    public const string SnapshotUnavailable = "shipping.selected_capture.snapshot_unavailable";

    /// <summary>درخواست شکل معتبر ندارد.</summary>
    public const string InvalidRequest = "shipping.selected_capture.invalid";
}

/// <summary>
/// درخواست خروجی‌محور برای خواندن انتخاب: هویت کامل مالک و Binding دقیق سرویس/گزینه.
/// </summary>
/// <remarks>
/// هیچ فیلد «پیش‌نویس»، «آخرین» یا «فعال» از مشتری پذیرفته نمی‌شود؛ این خواندن فقط اشاره‌گر ذخیره‌شده
/// را حل می‌کند. هویت مالک <b>داده</b> است، نه مجوز actor.
/// </remarks>
/// <param name="OwnerKind">گونهٔ مالک.</param>
/// <param name="OwnerTenantId">tenant مالک Store یا تهی.</param>
/// <param name="OwnerSellerPartyId">فروشندهٔ مالک Seller یا تهی.</param>
/// <param name="ServiceCode">کد سرویس (مقایسهٔ trim + بی‌توجه به بزرگی حروف).</param>
/// <param name="OptionCode">گزینه یا تهی.</param>
public sealed record ShippingSelectedCapturedRevisionRequest(
    ShippingCapturedQuoteOwnerKind OwnerKind,
    string? OwnerTenantId,
    Guid? OwnerSellerPartyId,
    string ServiceCode,
    string? OptionCode);

/// <summary>نام/توضیح ذخیره‌شده برای برچسب مشتری؛ شکل Contracts، بدون payload دامنه.</summary>
/// <param name="LanguageId">شناسهٔ زبان محتوا.</param>
/// <param name="Name">نام ذخیره‌شده در همان capture.</param>
/// <param name="Description">توضیح ذخیره‌شده یا تهی.</param>
public sealed record ShippingSelectedCapturedTranslation(
    Guid LanguageId,
    string Name,
    string? Description);

/// <summary>
/// نتیجهٔ فقط‌خواندنی انتخاب: نسخه و وضعیت صریح، و در حالت Selected متادیتای دقیق همان capture.
/// </summary>
/// <param name="Verdict">حکم خواندن.</param>
/// <param name="ReasonCode">کد پایدار دلیل در حالت امتناع.</param>
/// <param name="State">وضعیت اشاره‌گر.</param>
/// <param name="SelectionVersion">نسخهٔ اشاره‌گر یا تهی وقتی اشاره‌گری نیست.</param>
/// <param name="SnapshotId">capture انتخاب‌شده.</param>
/// <param name="DraftId">پیش‌نویس مبدأ همان capture.</param>
/// <param name="SourceRevision">بازبینی مبدأ همان capture.</param>
/// <param name="FormatVersion">نسخهٔ قالب همان capture.</param>
/// <param name="ServiceCode">کد سرویس استخراج‌شده از capture.</param>
/// <param name="OptionCode">گزینهٔ استخراج‌شده از capture.</param>
/// <param name="ShippingTypeId">شناسهٔ خام نوع ذخیره‌شده در همان capture یا تهی.</param>
/// <param name="Translations">نام/توضیح‌های ذخیره‌شدهٔ همان capture.</param>
public sealed record ShippingSelectedCapturedRevisionResult(
    ShippingSelectedCaptureVerdict Verdict,
    string? ReasonCode,
    ShippingSelectedCaptureReadState State,
    long? SelectionVersion,
    Guid? SnapshotId,
    Guid? DraftId,
    long? SourceRevision,
    int? FormatVersion,
    string? ServiceCode,
    string? OptionCode,
    Guid? ShippingTypeId,
    IReadOnlyList<ShippingSelectedCapturedTranslation> Translations);

/// <summary>
/// درز Contracts برای خواندن capture انتخاب‌شدهٔ یک مالک + Binding — بدون هیچ fallback به پیش‌نویس
/// جاری، «جدیدترین» snapshot، پیکربندی یا مالک دیگر.
/// </summary>
public interface IShippingSelectedCapturedRevisionLookup
{
    /// <summary>اشاره‌گر ذخیره‌شده را می‌خواند؛ فقط‌خواندنی و بدون هیچ نوشتن.</summary>
    /// <param name="request">هویت مالک و Binding دقیق.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<ShippingSelectedCapturedRevisionResult> ReadAsync(
        ShippingSelectedCapturedRevisionRequest request,
        CancellationToken cancellationToken);
}
