using Tooba.Shipping.Contracts.Authoring;

namespace Tooba.Shipping.Application.Authoring.Models;

/// <summary>یک انتخاب مقصد در پاسخ نویسندگی: گونهٔ گره و شناسهٔ پایدار.</summary>
/// <param name="Kind">گونهٔ گره.</param>
/// <param name="Id">شناسهٔ پایدار گره.</param>
public sealed record ShippingAuthoringSelectedAreaItem(ShippingAuthoringAreaKind Kind, Guid Id);

/// <summary>یک گروه منطقه‌ای در پاسخ: هویت پایدار، ترتیب و انتخاب‌ها.</summary>
/// <param name="GroupId">شناسهٔ پایدار گروه (مرجع آیندهٔ ردیف‌های نرخ).</param>
/// <param name="Order">ترتیب صریح گروه.</param>
/// <param name="Areas">انتخاب‌های گروه.</param>
public sealed record ShippingAuthoringRegionGroupItem(
    Guid GroupId,
    int Order,
    IReadOnlyList<ShippingAuthoringSelectedAreaItem> Areas);

/// <summary>
/// پاسخ یک پیش‌نویس نویسندگی. صادقانه <b>Draft</b> است: هیچ نرخی، فعال‌سازی یا مصرف‌کنندهٔ
/// استعلامی در این سطح وجود ندارد و نبود گروه یعنی پیکربندی ناقص.
/// </summary>
/// <param name="DraftId">شناسهٔ پایدار پیش‌نویس.</param>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="ServiceCode">کد خدمت Binding.</param>
/// <param name="OptionCode">گزینهٔ Binding؛ تهی یعنی سطح خدمت.</param>
/// <param name="Revision">بازبینی فعلی برای درخواست جایگزینی بعدی.</param>
/// <param name="CreatedAt">زمان ایجاد.</param>
/// <param name="UpdatedAt">زمان آخرین جایگزینی.</param>
/// <param name="RegionGroups">گروه‌های منطقه‌ای.</param>
public sealed record ShippingAuthoringDraftItem(
    Guid DraftId,
    ShippingAuthoringOwnerView Owner,
    string ServiceCode,
    string? OptionCode,
    long Revision,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ShippingAuthoringRegionGroupItem> RegionGroups);

/// <summary>فهرست پیش‌نویس‌های مالک مجاز همین درخواست.</summary>
/// <param name="Drafts">پیش‌نویس‌ها؛ فهرست خالی یعنی هنوز پیش‌نویسی ساخته نشده است.</param>
public sealed record ShippingAuthoringDraftList(IReadOnlyList<ShippingAuthoringDraftItem> Drafts);
