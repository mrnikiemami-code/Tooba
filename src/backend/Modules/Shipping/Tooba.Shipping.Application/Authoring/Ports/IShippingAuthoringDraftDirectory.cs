using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Application.Authoring.Ports;

/// <summary>
/// درز ماندگاری پیش‌نویس‌های نویسندگی Shipping. هر متد <b>کل</b> مالک مجاز را می‌گیرد و فیلتر
/// می‌کند؛ هیچ بارگذاری‌ای فقط با شناسه و اعتماد به فراخوان وجود ندارد.
/// </summary>
/// <remarks>
/// نگاشت scope سرورساختهٔ TB-189 به بازنمایی ماندگاری مالک، وظیفهٔ زیرساخت است؛ Application فقط
/// scope مجاز را عبور می‌دهد و هرگز مالک را از transport نمی‌سازد.
/// </remarks>
public interface IShippingAuthoringDraftDirectory
{
    /// <summary>پیش‌نویس‌های همین مالک را می‌خواند.</summary>
    /// <param name="owner">مالک مجاز همین درخواست.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<IReadOnlyList<ShippingAuthoringDraft>>> ListAsync(
        ShippingAuthoringOwnerScope owner,
        CancellationToken cancellationToken);

    /// <summary>یک پیش‌نویس همین مالک را می‌خواند؛ مالک دیگر همان «پیدا نشد» است.</summary>
    /// <param name="owner">مالک مجاز همین درخواست.</param>
    /// <param name="draftId">شناسهٔ پیش‌نویس.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingAuthoringDraft>> GetAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        CancellationToken cancellationToken);

    /// <summary>پیش‌نویس تازه می‌سازد؛ یکتایی Binding مالک، هم پیش‌از‌نوشت و هم در دیتابیس، حفظ می‌شود.</summary>
    /// <param name="owner">مالک مجاز همین درخواست.</param>
    /// <param name="serviceCode">کد خدمت Binding.</param>
    /// <param name="optionCode">گزینهٔ اختیاری Binding.</param>
    /// <param name="regionGroups">درخت اولیه.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingAuthoringDraft>> CreateAsync(
        ShippingAuthoringOwnerScope owner,
        string serviceCode,
        string? optionCode,
        IReadOnlyList<ShippingAuthoringRegionGroupInput> regionGroups,
        CancellationToken cancellationToken);

    /// <summary>
    /// درخت منطقه‌ای یک پیش‌نویس همین مالک را در یک مرز ماندگاری اتمی جایگزین می‌کند؛
    /// <paramref name="expectedRevision"/> شرط همزمانی واقعی است.
    /// </summary>
    /// <param name="owner">مالک مجاز همین درخواست.</param>
    /// <param name="draftId">شناسهٔ پیش‌نویس.</param>
    /// <param name="expectedRevision">بازبینی مورد انتظار کلاینت.</param>
    /// <param name="regionGroups">درخت جدید (جایگزینی کامل).</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingAuthoringDraft>> ReplaceRegionGroupsAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        long expectedRevision,
        IReadOnlyList<ShippingAuthoringRegionGroupInput> regionGroups,
        CancellationToken cancellationToken);

    /// <summary>
    /// مجموعهٔ کامل ردیف‌های نرخ یک گروه همین مالک را در <b>یک</b> SaveChanges ضمنی اتمی جایگزین
    /// می‌کند و بازبینی ریشه را یک واحد بالا می‌برد؛ <paramref name="expectedRevision"/> شرط همزمانی
    /// واقعی و مشترک با جایگزینی درخت است.
    /// </summary>
    /// <param name="owner">مالک مجاز همین درخواست.</param>
    /// <param name="draftId">شناسهٔ پیش‌نویس.</param>
    /// <param name="groupId">شناسهٔ گروه زیر همان پیش‌نویس.</param>
    /// <param name="expectedRevision">بازبینی مورد انتظار کلاینت.</param>
    /// <param name="rows">مجموعهٔ کامل ردیف‌ها (جایگزینی کامل).</param>
    /// <param name="trustedCurrencyCode">ارز معتبر سرورساخته؛ هرگز از transport نمی‌آید.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingAuthoringDraft>> ReplaceGroupRateRowsAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        Guid groupId,
        long expectedRevision,
        IReadOnlyList<ShippingAuthoringDraft.ShippingAuthoringRateRowInput> rows,
        string trustedCurrencyCode,
        CancellationToken cancellationToken);

    /// <summary>
    /// سند ارائهٔ زبان‌محور یک پیش‌نویس همین مالک را در <b>یک</b> SaveChanges ضمنی اتمی می‌نویسد و
    /// بازبینی ریشه را حداکثر یک واحد بالا می‌برد؛ <paramref name="expectedRevision"/> همان نشانگر
    /// همزمانی مشترک با درخت منطقه و ردیف‌های نرخ است.
    /// </summary>
    /// <param name="owner">مالک مجاز همین درخواست.</param>
    /// <param name="draftId">شناسهٔ پیش‌نویس.</param>
    /// <param name="expectedRevision">بازبینی مورد انتظار کلاینت.</param>
    /// <param name="translations">تغییرهای نام/توضیح هر زبان.</param>
    /// <param name="titles">تغییرهای عنوان گروه.</param>
    /// <param name="activeLanguageIds">شناسهٔ زبان‌های فعال authoritative (سرورساخته).</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingAuthoringDraft>> ReplacePresentationAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        long expectedRevision,
        IReadOnlyList<ShippingAuthoringDraft.MethodTranslationInput> translations,
        IReadOnlyList<ShippingAuthoringDraft.GroupTitleInput> titles,
        IReadOnlySet<Guid> activeLanguageIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// برنامهٔ عملیاتی هفتگی یک پیش‌نویس همین مالک را در <b>یک</b> SaveChanges ضمنی اتمی جایگزین
    /// می‌کند و بازبینی ریشه را یک واحد بالا می‌برد؛ <paramref name="expectedRevision"/> همان نشانگر
    /// همزمانی مشترک با منطقه/نرخ/ارائه است.
    /// </summary>
    /// <param name="owner">مالک مجاز همین درخواست.</param>
    /// <param name="draftId">شناسهٔ پیش‌نویس.</param>
    /// <param name="expectedRevision">بازبینی مورد انتظار کلاینت.</param>
    /// <param name="timeZoneId">شناسهٔ منطقهٔ زمانی صریح؛ اعتبارسنجی در دامنه.</param>
    /// <param name="days">دقیقاً هفت روز هفته (جایگزینی کامل).</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingAuthoringDraft>> ReplaceWeeklyScheduleAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        long expectedRevision,
        string? timeZoneId,
        IReadOnlyList<ShippingAuthoringDraft.ScheduleDayInput> days,
        CancellationToken cancellationToken);
}

/// <summary>
/// درز اثبات جغرافیا برای انتخاب‌های یک پیش‌نویس. هر انتخاب به یک زنجیرهٔ <b>اثبات‌شده</b> از
/// کشور تا خود گره تبدیل می‌شود، یا کل درخواست با کد پایدار رد می‌شود.
/// </summary>
/// <remarks>
/// شناسه‌های والد در بدنه، ادعای مالک است؛ این درز آن‌ها را با فکت‌های معتبر Geography می‌سنجد،
/// سلسله‌مراتب واقعی (از جمله عمق دلخواه ناحیهٔ اداری) را خودش می‌پیماید و چرخه/والد غایب را رد
/// می‌کند. نتیجه یک نمایش مسیر محدود برای همان عملیات است، نه یک cache سراسری.
/// </remarks>
public interface IShippingAuthoringDestinationResolver
{
    /// <summary>همهٔ انتخاب‌ها را در همان ترتیب ورودی (گروه‌به‌گروه، سپس داخل گروه) اثبات می‌کند.</summary>
    /// <param name="selections">انتخاب‌های بدنهٔ نوشتن.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<IReadOnlyList<ShippingDestinationSelectionPath>>> ResolveAsync(
        IReadOnlyList<ShippingAuthoringSelectedAreaWrite> selections,
        CancellationToken cancellationToken);
}
