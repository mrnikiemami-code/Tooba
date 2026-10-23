using Tooba.Shipping.Contracts.Authoring;

namespace Tooba.Shipping.Application.Authoring.Models;

/// <summary>Truthful state of one requested destination key.</summary>
public enum ShippingAuthoringDestinationResolution
{
    /// <summary>هویت و زنجیرهٔ والد اثبات شد؛ شکل نوشتن واقعی موجود است.</summary>
    Resolved = 0,

    /// <summary>هویت/والد غایب، غیرفعال، متناقض یا چرخه‌دار است; هیچ شکل نوشتنی جعل نمی‌شود.</summary>
    Unresolved = 1,
}

/// <summary>
/// شکل <b>نوشتنی</b> canonical یک مقصد انتخاب‌شده که فقط با اثبات واقعی والدها ساخته می‌شود.
/// </summary>
/// <remarks>
/// <see cref="AdministrativeAreaId"/> برای شهر بدون ناحیهٔ اداری عمداً تهی است: Geography چنین
/// شهری را مجاز می‌داند و اینجا هیچ والد جعلی ساخته نمی‌شود. برای ناحیهٔ خدمت، ناحیهٔ اداری فقط
/// وقتی پر می‌شود که والد واقعی شهر آن را اثبات کند.
/// </remarks>
/// <param name="Kind">نوع مقصد همان کلید خوانده‌شده.</param>
/// <param name="CountryId">کشور اثبات‌شده.</param>
/// <param name="AdministrativeAreaId">ناحیهٔ اداری اثبات‌شده یا تهی.</param>
/// <param name="CityId">شهر اثبات‌شده یا تهی.</param>
/// <param name="ServiceAreaId">ناحیهٔ خدمت اثبات‌شده یا تهی.</param>
public sealed record ShippingAuthoringDestinationWriteShape(
    ShippingAuthoringAreaKind Kind,
    Guid CountryId,
    Guid? AdministrativeAreaId,
    Guid? CityId,
    Guid? ServiceAreaId);

/// <summary>
/// یک فکت مقصد برای یک کلید <b>متمایز</b> (نوع + شناسه) از انتخاب‌های همان پیش‌نویس.
/// </summary>
/// <remarks>
/// <para>
/// این فکت فقط خواندنی و افزودنی است و <b>مرجعیت</b> ندارد: نوشتن همیشه جغرافیای واقعی را دوباره
/// اعتبارسنجی می‌کند و هیچ فکت hydrate‌شده‌ای از مرورگر پذیرفته نمی‌شود.
/// </para>
/// <para>
/// <see cref="Label"/> برچسب محلی‌شدهٔ واقعی برای زبان پیش‌فرض محتوای authoritative است؛ اگر برچسب
/// واقعی نباشد تهی می‌ماند و هیچ نام جعلی/شناسهٔ خام/حدس locale ساخته نمی‌شود.
/// </para>
/// </remarks>
/// <param name="Kind">نوع مقصد.</param>
/// <param name="Id">شناسهٔ پایدار همان نوع.</param>
/// <param name="Resolution">نتیجهٔ صادقانهٔ اثبات.</param>
/// <param name="Area">شکل نوشتن canonical فقط در حالت اثبات‌شده؛ وگرنه تهی.</param>
/// <param name="Label">برچسب محلی‌شده یا تهی.</param>
public sealed record ShippingAuthoringDestinationFactItem(
    ShippingAuthoringAreaKind Kind,
    Guid Id,
    ShippingAuthoringDestinationResolution Resolution,
    ShippingAuthoringDestinationWriteShape? Area,
    string? Label);
