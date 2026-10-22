using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Application.Authoring.Commands;

/// <summary>
/// تعیین یا پاک‌کردن اختیاری نوع ارسال روی ریشهٔ روش یک پیش‌نویس مالک، با بازبینی ریشهٔ مورد انتظار.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
/// <param name="Request">بدنهٔ بازبینی و انتخاب صریح (شناسه یا پاک‌کردن).</param>
public sealed record ReplaceShippingAuthoringDraftTypeAssignmentCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId,
    ShippingAuthoringDraftTypeAssignmentReplaceRequest Request)
    : IRequest<Result<ShippingAuthoringDraftTypeAssignmentItem>>;

    /// <summary>
    /// هندلر PUT انتخاب نوع. ترتیب تصمیم‌ها عمدی و ثابت است:
    /// (۱) پیش‌نویس فقط داخل محدودهٔ کامل مالک، (۲) بازبینی کهنهٔ کلاینت <b>پیش از</b> هر خواندن
    /// بیرونی رد می‌شود (حتی برای درخواست یکسان یا پاک‌کردن)، (۳) فقط برای شناسهٔ غیرتهی، تعریف دقیق
    /// باید <b>فعال</b> باشد، (۴) سپس یک نوشتن اتمی روی ریشه.
    /// </summary>
    /// <remarks>
    /// <b>پاک‌کردن</b> هیچ فراخوانی بیرونی نمی‌خواهد و حتی وقتی کاتالوگ در دسترس نیست کار می‌کند.
    /// <b>نوشتن یکسان</b> روی انتخاب فعالِ فعلی یک no-op صادقانه است: بازبینی و زمان دست‌نخورده
    /// می‌مانند، اما بازبینی کهنه همچنان 409 است. شاخه‌های زودگذر (پیش‌نویس غایب، بازبینی کهنه،
    /// پاک‌کردن، no-op پیش از فراخوانی) هیچ span فراخوانی ماژولی نمی‌سازند.
    /// <para>
    /// <b>یک خواندن پیش از نوشتن.</b> فکت فعالِ تأییدشده همان لحظه در همین درخواست نگه داشته می‌شود و
    /// پاسخ پس از ذخیره <b>بدون هیچ فراخوانی بیرونی</b> از ریشهٔ واقعاً ذخیره‌شده ساخته می‌شود. بنابراین
    /// قطعی کاتالوگ بعد از commit نمی‌تواند موفقیت ذخیره‌شده را به شکست تبدیل کند؛ این فکت یک واقعیت
    /// همان لحظهٔ اعتبارسنجی است، نه تضمین اتمی در برابر تغییر بعدی کاتالوگ.
    /// </para>
    /// </remarks>
public sealed class ReplaceShippingAuthoringDraftTypeAssignmentHandler(
    IShippingAuthoringDraftDirectory directory,
    IShippingTypeCatalogReader catalog)
    : IRequestHandler<ReplaceShippingAuthoringDraftTypeAssignmentCommand, Result<ShippingAuthoringDraftTypeAssignmentItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringDraftTypeAssignmentItem>> Handle(
        ReplaceShippingAuthoringDraftTypeAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);
        ArgumentNullException.ThrowIfNull(request.Request);

        try
        {
            // (1) The authorized owner filter runs FIRST: a foreign/unknown draft must never cause a
            // foreign catalog read, and it returns the same safe not-found as a missing one.
            var draft = await directory.GetAsync(request.Owner, request.DraftId, cancellationToken);
            if (draft.IsFailure)
            {
                return Result.Failure<ShippingAuthoringDraftTypeAssignmentItem>(draft.Errors);
            }

            // (2) A stale concurrency token wins before ANY external lookup, including identical and
            // clearing requests.
            if (draft.Value.Revision != request.Request.ExpectedRevision)
            {
                return Result.Failure<ShippingAuthoringDraftTypeAssignmentItem>(
                    new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
            }

            ShippingTypeFact? resolved = null;
            if (request.Request.ShippingTypeId is { } selected)
            {
                if (selected == Guid.Empty)
                {
                    // Defense in depth for direct (non-HTTP) invocation: an empty id is never "clear".
                    return Result.Failure<ShippingAuthoringDraftTypeAssignmentItem>(
                        new SemanticError(ShippingErrorCodes.AuthoringTypeAssignmentInvalid));
                }

                // (3) Every non-null selection — including the id already stored — must resolve EXACTLY
                // one ACTIVE definition. Unknown/inactive/inconsistent facts reject before any write.
                var facts = await catalog.GetByIdsAsync([selected], cancellationToken);
                resolved = facts.Count == 1 && facts[0].ShippingTypeId == selected ? facts[0] : null;
                if (resolved is null || !resolved.IsActive)
                {
                    return Result.Failure<ShippingAuthoringDraftTypeAssignmentItem>(
                        new SemanticError(ShippingErrorCodes.AuthoringTypeSelectionUnavailable));
                }
            }

            // (4) ONE implicit SaveChanges publishes the assignment and the shared root revision.
            var replaced = await directory.ReplaceTypeAssignmentAsync(
                request.Owner,
                request.DraftId,
                request.Request.ExpectedRevision,
                request.Request.ShippingTypeId,
                cancellationToken);
            if (replaced.IsFailure)
            {
                return Result.Failure<ShippingAuthoringDraftTypeAssignmentItem>(replaced.Errors);
            }

            // The response is composed ONLY from the actual saved root plus the single fact resolved
            // before the write: no foreign lookup (and no other fallible dependency) runs after commit,
            // so a catalog outage can never turn a persisted assignment into a reported failure.
            return Result.Success(ShippingTypeAssignmentComposition.ComposeResolved(replaced.Value, resolved));
        }
        catch (SemanticException exception)
        {
            return Result.Failure<ShippingAuthoringDraftTypeAssignmentItem>(exception.Error);
        }
    }
}
