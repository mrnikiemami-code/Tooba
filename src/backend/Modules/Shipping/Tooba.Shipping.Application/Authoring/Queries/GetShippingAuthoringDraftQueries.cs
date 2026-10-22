using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;

namespace Tooba.Shipping.Application.Authoring.Queries;

/// <summary>
/// فهرست پیش‌نویس‌های مالک مجاز همین درخواست. مسیر ادمین و فروشنده همین یک use case را صدا
/// می‌زنند و تفاوتشان فقط مجوزدهی سرور است.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
public sealed record GetShippingAuthoringDraftsQuery(ShippingAuthoringOwnerScope Owner)
    : IRequest<Result<ShippingAuthoringDraftList>>;

/// <summary>هندلر فهرست پیش‌نویس‌ها؛ فقط ردیف‌های همین مالک خوانده می‌شوند.</summary>
public sealed class GetShippingAuthoringDraftsHandler(IShippingAuthoringDraftDirectory directory)
    : IRequestHandler<GetShippingAuthoringDraftsQuery, Result<ShippingAuthoringDraftList>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringDraftList>> Handle(
        GetShippingAuthoringDraftsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);

        var drafts = await directory.ListAsync(request.Owner, cancellationToken);
        return drafts.IsFailure
            ? Result.Failure<ShippingAuthoringDraftList>(drafts.Errors)
            : Result.Success(new ShippingAuthoringDraftList(
                drafts.Value
                    .Select(draft => Commands.ShippingAuthoringDraftWorkflow.Map(draft, request.Owner))
                    .ToArray()));
    }
}

/// <summary>خواندن یک پیش‌نویس در محدودهٔ مالک مجاز.</summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
public sealed record GetShippingAuthoringDraftQuery(ShippingAuthoringOwnerScope Owner, Guid DraftId)
    : IRequest<Result<ShippingAuthoringDraftItem>>;

/// <summary>
/// هندلر خواندن پیش‌نویس. شناسهٔ مالک دیگر و شناسهٔ ساختگی همان پاسخ ایمن «پیدا نشد» را می‌گیرند؛
/// هیچ اطلاعاتی دربارهٔ وجود/مالکیت ردیف افشا نمی‌شود.
/// </summary>
public sealed class GetShippingAuthoringDraftHandler(IShippingAuthoringDraftDirectory directory)
    : IRequestHandler<GetShippingAuthoringDraftQuery, Result<ShippingAuthoringDraftItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringDraftItem>> Handle(
        GetShippingAuthoringDraftQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);

        var draft = await directory.GetAsync(request.Owner, request.DraftId, cancellationToken);
        return draft.IsFailure
            ? Result.Failure<ShippingAuthoringDraftItem>(draft.Errors)
            : Result.Success(Commands.ShippingAuthoringDraftWorkflow.Map(draft.Value, request.Owner));
    }
}
