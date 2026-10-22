using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Application.Authoring.Commands;

/// <summary>
/// به‌روزرسانی زبان‌محور نام/توضیح روش و عنوان‌های اختیاری گروه‌ها با بازبینی ریشهٔ مورد انتظار.
/// مالک/حالت/ارز هرگز از body نمی‌آیند.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
/// <param name="Request">بدنهٔ تغییرهای زبان‌محور.</param>
public sealed record ReplaceShippingAuthoringPresentationCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId,
    ShippingAuthoringPresentationPatchRequest Request)
    : IRequest<Result<ShippingAuthoringPresentationItem>>;

/// <summary>
/// هندلر PATCH ارائه. زبان‌های فعال <b>پیش از هر نوشتن</b> از درز canonical زمینه خوانده می‌شوند؛ اگر
/// زمینهٔ trusted resolve نشود، نوشتن fail-closed رد می‌شود و هیچ متنی تغییر نمی‌کند.
/// </summary>
/// <remarks>
/// کل سند در دامنه پیش‌سنجی می‌شود (شکل، یکتایی جفت‌ها، عضویت گروه، زبان فعال و طول متن) و سپس در
/// <b>یک</b> SaveChanges ضمنی همراه با یک واحد افزایش بازبینی ریشه نوشته می‌شود؛ هیچ تراکنش دست‌سندی
/// و هیچ جایگزینی دیکشنری‌ای وجود ندارد.
/// </remarks>
public sealed class ReplaceShippingAuthoringPresentationHandler(
    IShippingAuthoringDraftDirectory directory,
    IShippingAuthoringContextSource contextSource)
    : IRequestHandler<ReplaceShippingAuthoringPresentationCommand, Result<ShippingAuthoringPresentationItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringPresentationItem>> Handle(
        ReplaceShippingAuthoringPresentationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);
        ArgumentNullException.ThrowIfNull(request.Request);

        // Defense in depth for direct (non-HTTP) invocation: an explicit null list is never "no change";
        // [] stays a valid no-op document.
        if (request.Request.MethodTranslations is null || request.Request.GroupTitles is null)
        {
            return Result.Failure<ShippingAuthoringPresentationItem>(
                new SemanticError(ShippingErrorCodes.AuthoringPresentationPayloadInvalid));
        }

        var snapshot = await contextSource.ReadAsync(cancellationToken);
        var context = ShippingAuthoringContextProjection.Project(snapshot);
        if (context.IsFailure)
        {
            return Result.Failure<ShippingAuthoringPresentationItem>(context.Errors);
        }

        IReadOnlyList<ShippingAuthoringDraft.MethodTranslationInput> translations;
        IReadOnlyList<ShippingAuthoringDraft.GroupTitleInput> titles;
        try
        {
            translations = ShippingAuthoringPresentationWorkflow.ToDomainTranslations(
                request.Request.MethodTranslations);
            titles = ShippingAuthoringPresentationWorkflow.ToDomainTitles(request.Request.GroupTitles);
        }
        catch (SemanticException exception)
        {
            return Result.Failure<ShippingAuthoringPresentationItem>(exception.Error);
        }

        try
        {
            var replaced = await directory.ReplacePresentationAsync(
                request.Owner,
                request.DraftId,
                request.Request.ExpectedRevision,
                translations,
                titles,
                context.Value.ContentLanguages.Select(language => language.LanguageId).ToHashSet(),
                cancellationToken);
            return replaced.IsFailure
                ? Result.Failure<ShippingAuthoringPresentationItem>(replaced.Errors)
                : Result.Success(ShippingAuthoringPresentationWorkflow.Map(replaced.Value, context.Value));
        }
        catch (SemanticException exception)
        {
            return Result.Failure<ShippingAuthoringPresentationItem>(exception.Error);
        }
    }
}

/// <summary>
/// نگاشت شکل انتقال به شکل دامنه و پاسخ. هیچ قاعدهٔ تجاری اینجا نیست؛ عنصر null پیش از هر نوشتن با
/// کد پایدار رد می‌شود.
/// </summary>
internal static class ShippingAuthoringPresentationWorkflow
{
    /// <summary>فهرست ترجمهٔ نام را به ورودی دامنه تبدیل می‌کند.</summary>
    /// <param name="writes">تغییرهای بدنه.</param>
    public static IReadOnlyList<ShippingAuthoringDraft.MethodTranslationInput> ToDomainTranslations(
        IReadOnlyList<ShippingAuthoringMethodTranslationWrite> writes)
    {
        ArgumentNullException.ThrowIfNull(writes);
        var result = new List<ShippingAuthoringDraft.MethodTranslationInput>(writes.Count);
        foreach (var write in writes)
        {
            if (write is null)
            {
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringPresentationPayloadInvalid));
            }

            result.Add(new ShippingAuthoringDraft.MethodTranslationInput(write.LanguageId, write.Name, write.Description));
        }

        return result;
    }

    /// <summary>فهرست عنوان گروه را به ورودی دامنه تبدیل می‌کند.</summary>
    /// <param name="writes">تغییرهای بدنه.</param>
    public static IReadOnlyList<ShippingAuthoringDraft.GroupTitleInput> ToDomainTitles(
        IReadOnlyList<ShippingAuthoringGroupTitleWrite> writes)
    {
        ArgumentNullException.ThrowIfNull(writes);
        var result = new List<ShippingAuthoringDraft.GroupTitleInput>(writes.Count);
        foreach (var write in writes)
        {
            if (write is null)
            {
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringPresentationPayloadInvalid));
            }

            result.Add(new ShippingAuthoringDraft.GroupTitleInput(write.GroupId, write.LanguageId, write.Title));
        }

        return result;
    }

    /// <summary>
    /// پیش‌نویس + registry فعلی را به سند ارائه نگاشت می‌کند. متن‌ها خام و مرتب بر اساس شناسهٔ زبان
    /// برمی‌گردند؛ هیچ fallback یا نمایش «ترجمه‌شده» ساخته نمی‌شود.
    /// </summary>
    /// <param name="draft">پیش‌نویس مالک.</param>
    /// <param name="context">زمینهٔ فعال authoritative.</param>
    public static ShippingAuthoringPresentationItem Map(
        ShippingAuthoringDraft draft,
        ShippingAuthoringContextView context)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(context);

        var defaultLanguageId = context.DefaultContentLanguageId;
        var translations = draft.Translations
            .OrderBy(translation => translation.LanguageId)
            .Select(translation => new ShippingAuthoringMethodTranslationItem(
                translation.LanguageId,
                translation.Name,
                translation.Description))
            .ToArray();

        return new ShippingAuthoringPresentationItem(
            draft.DraftId,
            draft.Revision,
            translations,
            draft.RegionGroups
                .OrderBy(group => group.Order)
                .SelectMany(group => group.Titles
                    .OrderBy(title => title.LanguageId)
                    .Select(title => new ShippingAuthoringGroupTitleItem(
                        group.GroupId,
                        title.LanguageId,
                        title.Title)))
                .ToArray(),
            context.ContentLanguages,
            defaultLanguageId,
            translations.Any(translation => translation.LanguageId == defaultLanguageId));
    }
}
