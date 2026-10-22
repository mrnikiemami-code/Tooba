using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Application.Authoring.Queries;

/// <summary>
/// خواندن انتخاب‌های نوع ارسال فعال برای پنل نویسندگی: یک query برای هر دو مخاطب ادمین و فروشنده.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
public sealed record GetShippingAuthoringTypeChoicesQuery(ShippingAuthoringOwnerScope Owner)
    : IRequest<Result<ShippingAuthoringTypeChoicesItem>>;

/// <summary>
/// هندلر فهرست انتخاب‌ها. تنها فراخوانی مجاز <c>ListActiveAsync</c> است؛ هیچ مسیر ایجاد/ویرایش
/// کاتالوگ در Shipping وجود ندارد.
/// </summary>
/// <remarks>
/// فهرست خالی یک کاتالوگ واقعاً خالی است و موفق برگردانده می‌شود. شکست/لغو خواندن مقصد منتشر
/// می‌شود و هرگز به «کاتالوگ خالی» تبدیل نمی‌گردد.
/// </remarks>
public sealed class GetShippingAuthoringTypeChoicesHandler(IShippingTypeCatalogReader catalog)
    : IRequestHandler<GetShippingAuthoringTypeChoicesQuery, Result<ShippingAuthoringTypeChoicesItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringTypeChoicesItem>> Handle(
        GetShippingAuthoringTypeChoicesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);

        var facts = await catalog.ListActiveAsync(cancellationToken);
        return Result.Success(new ShippingAuthoringTypeChoicesItem(
            facts.Select(ShippingTypeProjection.Map).ToArray()));
    }
}

/// <summary>
/// خواندن انتخاب فعلی نوع ارسال یک پیش‌نویس مالک.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
public sealed record GetShippingAuthoringDraftTypeAssignmentQuery(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId)
    : IRequest<Result<ShippingAuthoringDraftTypeAssignmentItem>>;

/// <summary>
/// هندلر خواندن انتخاب فعلی. ابتدا فیلتر کامل مالک اجرا می‌شود، پس پیش‌نویس غایب/مالک دیگر همان پاسخ
/// ایمن «پیدا نشد» را می‌گیرد و <b>هیچ</b> فراخوانی کاتالوگ بیرونی رخ نمی‌دهد.
/// </summary>
/// <remarks>
/// شناسهٔ ذخیره‌شده هرگز پاک، جایگزین یا حدس زده نمی‌شود: وضعیت فعال/غیرفعال/یافت‌نشده صادقانه
/// گزارش می‌شود و خطای زیرساخت به «یافت‌نشد» تبدیل نمی‌گردد.
/// </remarks>
public sealed class GetShippingAuthoringDraftTypeAssignmentHandler(
    IShippingAuthoringDraftDirectory directory,
    IShippingTypeCatalogReader catalog)
    : IRequestHandler<GetShippingAuthoringDraftTypeAssignmentQuery, Result<ShippingAuthoringDraftTypeAssignmentItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringDraftTypeAssignmentItem>> Handle(
        GetShippingAuthoringDraftTypeAssignmentQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);

        var draft = await directory.GetAsync(request.Owner, request.DraftId, cancellationToken);
        if (draft.IsFailure)
        {
            return Result.Failure<ShippingAuthoringDraftTypeAssignmentItem>(draft.Errors);
        }

        return await ShippingTypeAssignmentComposition.ComposeAsync(
            draft.Value,
            catalog,
            cancellationToken);
    }
}

/// <summary>
/// ترکیب انتخاب ذخیره‌شدهٔ پیش‌نویس با فکت‌های همان لحظهٔ کاتالوگ برای یک پاسخ صادقانه.
/// </summary>
/// <remarks>
/// «یافت‌نشد» فقط وقتی است که پاسخ <b>واقعاً خالی</b> باشد. پاسخ غیرخالیِ ناسازگار (شناسهٔ دیگر یا
/// رکورد تکراری برای همان شناسه) «نبود» نیست: با کد canonical موجود
/// <c>shipping.authoring.configuration_unavailable</c> به‌شکل بسته شکست می‌خورد و شناسهٔ ذخیره‌شده
/// دست‌نخورده می‌ماند. برای نوشتن، پاسخ از همان یک فکت اعتبارسنجی‌شده و ریشهٔ ذخیره‌شده ساخته می‌شود و
/// <b>هیچ</b> فراخوانی بیرونی پس از commit رخ نمی‌دهد.
/// </remarks>
internal static class ShippingTypeAssignmentComposition
{
    /// <summary>پاسخ انتخاب فعلی را می‌سازد؛ بدون نوع انتخاب‌شده هیچ فراخوانی بیرونی رخ نمی‌دهد.</summary>
    /// <param name="draft">پیش‌نویس مالک.</param>
    /// <param name="catalog">درز کاتالوگ نوع.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public static async Task<Result<ShippingAuthoringDraftTypeAssignmentItem>> ComposeAsync(
        Domain.Authoring.ShippingAuthoringDraft draft,
        IShippingTypeCatalogReader catalog,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (draft.ShippingTypeId is not { } selected)
        {
            // UNCONFIGURED is a real state and needs no foreign read at all.
            return Result.Success(Unconfigured(draft));
        }

        var facts = await catalog.GetByIdsAsync([selected], cancellationToken);
        if (facts.Count == 0)
        {
            // A genuinely empty set is the ONLY truthful "missing"; the stored id is never cleared.
            return Result.Success(new ShippingAuthoringDraftTypeAssignmentItem(
                draft.DraftId,
                draft.Revision,
                selected,
                ShippingAuthoringTypeResolution.Missing,
                null));
        }

        if (facts.Count != 1 || facts[0].ShippingTypeId != selected)
        {
            // Wrong-id/duplicate/inconsistent facts are a dependency defect, not absence.
            return Result.Failure<ShippingAuthoringDraftTypeAssignmentItem>(
                new SemanticError(ShippingErrorCodes.AuthoringConfigurationUnavailable));
        }

        return Result.Success(Resolved(draft.DraftId, draft.Revision, facts[0]));
    }

    /// <summary>
    /// پاسخ را تنها از ریشهٔ واقعاً ذخیره‌شده و فکت واحدی که پیش از نوشتن تأیید شده ساخته می‌شود؛
    /// هیچ فراخوانی بیرونی یا وابستگی شکست‌پذیر پس از commit اجرا نمی‌شود.
    /// </summary>
    /// <param name="saved">ریشهٔ ذخیره‌شده (شناسه/بازبینی واقعی).</param>
    /// <param name="resolved">فکت فعال تأییدشده پیش از نوشتن، یا تهی برای پاک‌کردن.</param>
    public static ShippingAuthoringDraftTypeAssignmentItem ComposeResolved(
        Domain.Authoring.ShippingAuthoringDraft saved,
        ShippingTypeFact? resolved)
    {
        ArgumentNullException.ThrowIfNull(saved);

        return saved.ShippingTypeId is { } savedSelection && resolved is not null
            ? Resolved(saved.DraftId, saved.Revision, resolved)
            : Unconfigured(saved);
    }

    /// <summary>
    /// پاسخ را از ریشهٔ ذخیره‌شده و یک مشاهدهٔ <b>صادق</b> از کاتالوگ می‌سازد: یک فکت موجود یعنی
    /// Active/Inactive، نبودِ فکت برای شناسهٔ ذخیره‌شده یعنی <see cref="ShippingAuthoringTypeResolution.Missing"/>
    /// (هرگز «انتخاب‌نشده»ی جعلی). این متد هیچ فراخوانی بیرونی ندارد.
    /// </summary>
    /// <param name="saved">ریشهٔ واقعاً ذخیره‌شده.</param>
    /// <param name="observed">فکت مشاهده‌شده پیش از نوشتن، یا تهی وقتی کاتالوگ آن شناسه را ندارد.</param>
    public static ShippingAuthoringDraftTypeAssignmentItem ComposeObserved(
        Domain.Authoring.ShippingAuthoringDraft saved,
        ShippingTypeFact? observed)
    {
        ArgumentNullException.ThrowIfNull(saved);

        if (saved.ShippingTypeId is not { } savedSelection)
        {
            return Unconfigured(saved);
        }

        return observed is { } fact
            ? Resolved(saved.DraftId, saved.Revision, fact)
            : new ShippingAuthoringDraftTypeAssignmentItem(
                saved.DraftId,
                saved.Revision,
                savedSelection,
                ShippingAuthoringTypeResolution.Missing,
                null);
    }

    private static ShippingAuthoringDraftTypeAssignmentItem Unconfigured(Domain.Authoring.ShippingAuthoringDraft draft)
        => new(
            draft.DraftId,
            draft.Revision,
            null,
            ShippingAuthoringTypeResolution.Unconfigured,
            null);

    private static ShippingAuthoringDraftTypeAssignmentItem Resolved(
        Guid draftId,
        long revision,
        ShippingTypeFact fact)
        => new(
            draftId,
            revision,
            fact.ShippingTypeId,
            fact.IsActive ? ShippingAuthoringTypeResolution.Active : ShippingAuthoringTypeResolution.Inactive,
            ShippingTypeProjection.Map(fact));
}

/// <summary>نگاشت فکت کاتالوگ به قرارداد عمومی؛ هیچ ترجمه/جایگزینی متنی انجام نمی‌شود.</summary>
internal static class ShippingTypeProjection
{
    /// <summary>فکت را به قرارداد انتخاب نگاشت می‌کند.</summary>
    /// <param name="fact">فکت کاتالوگ.</param>
    public static ShippingAuthoringTypeItem Map(ShippingTypeFact fact)
    {
        ArgumentNullException.ThrowIfNull(fact);

        return new ShippingAuthoringTypeItem(
            fact.ShippingTypeId,
            fact.Code,
            fact.SortOrder,
            fact.Revision,
            fact.Translations
                .Select(translation => new ShippingAuthoringTypeTranslation(
                    translation.LanguageId,
                    translation.Name,
                    translation.Description))
                .ToArray());
    }
}
