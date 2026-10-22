using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Authoring;

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

        return Result.Success(await ShippingTypeAssignmentComposition.ComposeAsync(
            draft.Value,
            catalog,
            cancellationToken));
    }
}

/// <summary>
/// ترکیب انتخاب ذخیره‌شدهٔ پیش‌نویس با فکت‌های همان لحظهٔ کاتالوگ برای یک پاسخ صادقانه.
/// </summary>
internal static class ShippingTypeAssignmentComposition
{
    /// <summary>پاسخ انتخاب فعلی را می‌سازد؛ بدون نوع انتخاب‌شده هیچ فراخوانی بیرونی رخ نمی‌دهد.</summary>
    /// <param name="draft">پیش‌نویس مالک.</param>
    /// <param name="catalog">درز کاتالوگ نوع.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public static async Task<ShippingAuthoringDraftTypeAssignmentItem> ComposeAsync(
        Domain.Authoring.ShippingAuthoringDraft draft,
        IShippingTypeCatalogReader catalog,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (draft.ShippingTypeId is not { } selected)
        {
            // UNCONFIGURED is a real state and needs no foreign read at all.
            return new ShippingAuthoringDraftTypeAssignmentItem(
                draft.DraftId,
                draft.Revision,
                null,
                ShippingAuthoringTypeResolution.Unconfigured,
                null);
        }

        var facts = await catalog.GetByIdsAsync([selected], cancellationToken);
        var fact = facts.Count == 1 && facts[0].ShippingTypeId == selected ? facts[0] : null;
        if (fact is null)
        {
            // A missing definition is explicit and truthful; it is never silently cleared or replaced.
            return new ShippingAuthoringDraftTypeAssignmentItem(
                draft.DraftId,
                draft.Revision,
                selected,
                ShippingAuthoringTypeResolution.Missing,
                null);
        }

        return new ShippingAuthoringDraftTypeAssignmentItem(
            draft.DraftId,
            draft.Revision,
            selected,
            fact.IsActive ? ShippingAuthoringTypeResolution.Active : ShippingAuthoringTypeResolution.Inactive,
            ShippingTypeProjection.Map(fact));
    }
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
