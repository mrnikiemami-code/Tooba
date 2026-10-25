using System.Text.Json.Serialization;

namespace Tooba.Shipping.Contracts.Authoring;

/// <summary>
/// وضعیت صریح «انتشار» یک روش ارسالِ موجود: فقط قصد انتخاب یک capture تغییرناپذیر.
/// </summary>
/// <remarks>
/// هیچ حالت ضمنی مثل «جدیدترین» وجود ندارد و هیچ‌کدام از این سه مقدار به‌تنهایی مجوز مشتری،
/// آمادگی عملیاتی یا قیمت نیست.
/// </remarks>
public enum ShippingAuthoringPublicationState
{
    /// <summary>هیچ اشاره‌گری برای این مالک + Binding ذخیره نشده است.</summary>
    Absent = 0,

    /// <summary>اشاره‌گر وجود دارد و صریحاً هیچ capture‌ای انتخاب نشده است.</summary>
    Cleared = 1,

    /// <summary>یک capture تغییرناپذیر دقیق انتخاب شده است.</summary>
    Selected = 2,
}

/// <summary>
/// درخواست انتشار یک پیش‌نویس موجود: بازبینی ذخیره‌شدهٔ پیش‌نویس و نسخهٔ مورد انتظار اشاره‌گر.
/// </summary>
/// <remarks>
/// <para>
/// <b>غیبت ≠ صفر.</b> هر دو فیلد <see cref="JsonRequiredAttribute"/> هستند و
/// <see cref="ExpectedSelectionVersion"/> عمداً <c>long?</c> است: نبودِ کلید در JSON یک بدنهٔ نامعتبر
/// است، نه «نسخهٔ صفر» که ممکن است یک انتشار ناخواسته بسازد.
/// </para>
/// <para>
/// مالک، فروشنده، tenant، Binding سرویس/گزینه، ارز، snapshot و هیچ پرچم فعال‌سازی در این بدنه پذیرفته
/// نمی‌شود؛ هویت مالک از مجوزدهی سرور می‌آید و Binding از پیش‌نویسِ همان مالک مشتق می‌شود.
/// </para>
/// </remarks>
public sealed record ShippingAuthoringPublicationPublishRequest
{
    /// <summary>بازبینی ذخیره‌شدهٔ پیش‌نویس که کلاینت دیده است (باید مثبت باشد).</summary>
    [JsonRequired]
    public long? ExpectedRevision { get; init; }

    /// <summary>نسخهٔ مورد انتظار اشاره‌گر انتخاب؛ صفر یعنی «هنوز ردیفی نیست».</summary>
    [JsonRequired]
    public long? ExpectedSelectionVersion { get; init; }
}

/// <summary>بدنهٔ برداشتن انتشار: فقط نسخهٔ مورد انتظار اشاره‌گر، بدون هیچ capture.</summary>
public sealed record ShippingAuthoringPublicationWithdrawRequest
{
    /// <summary>نسخهٔ مورد انتظار اشاره‌گر انتخاب؛ صفر یعنی «هنوز ردیفی نیست».</summary>
    [JsonRequired]
    public long? ExpectedSelectionVersion { get; init; }
}

/// <summary>
/// خواندن/نتیجهٔ انتشار برای یک پیش‌نویس موجود — بدون هیچ payload دامنه یا بدنهٔ JSONB.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SelectionVersion"/> تنها برای <see cref="ShippingAuthoringPublicationState.Absent"/> صفر
/// است: در آن حالت واقعاً هیچ ردیفی و هیچ نسخه‌ای وجود ندارد. برای Cleared/Selected همیشه نسخهٔ واقعی
/// ذخیره‌شده برمی‌گردد و هیچ نسخه‌ای اختراع نمی‌شود.
/// </para>
/// <para>
/// <see cref="SelectedMatchesCurrentDraftRevision"/> فقط یک برابری <b>داده</b> است (پیش‌نویس و بازبینی
/// مبدأ همان capture برابر با پیش‌نویس/بازبینی ذخیره‌شدهٔ فعلی)؛ هیچ ادعای آمادگی، قیمت یا واجدشرط‌بودن
/// مشتری از آن ساخته نمی‌شود.
/// </para>
/// </remarks>
/// <param name="DraftId">پیش‌نویس خوانده‌شده در محدودهٔ مالک.</param>
/// <param name="CurrentDraftRevision">بازبینی ذخیره‌شدهٔ فعلی پیش‌نویس.</param>
/// <param name="ServiceCode">کد سرویس ذخیره‌شدهٔ پیش‌نویس.</param>
/// <param name="OptionCode">گزینهٔ ذخیره‌شدهٔ پیش‌نویس یا تهی.</param>
/// <param name="State">وضعیت صریح اشاره‌گر.</param>
/// <param name="SelectionVersion">نسخهٔ اشاره‌گر؛ صفر فقط در حالت Absent.</param>
/// <param name="SelectedSnapshotId">capture انتخاب‌شده یا تهی.</param>
/// <param name="SelectedDraftId">پیش‌نویس مبدأ همان capture یا تهی.</param>
/// <param name="SelectedSourceRevision">بازبینی مبدأ همان capture یا تهی.</param>
/// <param name="SelectedFormatVersion">نسخهٔ قالب همان capture یا تهی.</param>
/// <param name="SelectedMatchesCurrentDraftRevision">آیا مبدأ capture برابر پیش‌نویس/بازبینی فعلی است.</param>
public sealed record ShippingAuthoringPublicationView(
    Guid DraftId,
    long CurrentDraftRevision,
    string ServiceCode,
    string? OptionCode,
    ShippingAuthoringPublicationState State,
    long SelectionVersion,
    Guid? SelectedSnapshotId,
    Guid? SelectedDraftId,
    long? SelectedSourceRevision,
    int? SelectedFormatVersion,
    bool SelectedMatchesCurrentDraftRevision);
