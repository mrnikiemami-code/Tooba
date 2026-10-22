using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Application.Authoring.Queries;

/// <summary>
/// خواندن مجموعهٔ ردیف‌های نرخ یک گروه از پیش‌نویس همین مالک. ادمین و فروشنده همین یک use case را
/// صدا می‌زنند و تفاوتشان فقط مجوزدهی سرور است.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
/// <param name="GroupId">شناسهٔ گروه از مسیر.</param>
public sealed record GetShippingAuthoringGroupRateRowsQuery(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId,
    Guid GroupId) : IRequest<Result<ShippingAuthoringGroupRateRowsItem>>;

/// <summary>
/// هندلر خواندن ردیف‌های نرخ. مالکیت پیش‌نویس با فیلتر کامل مالک تعیین می‌شود و گروه فقط
/// <b>داخل همان پیش‌نویس</b> جست‌وجو می‌شود؛ شناسهٔ گروه به‌تنهایی هرگز بارگذاری نمی‌کند.
/// </summary>
/// <remarks>
/// این خواندن هیچ ارزی را از زمینهٔ trusted نمی‌سازد و هیچ‌چیز را تغییر نمی‌دهد: فقط واقعیت
/// ذخیره‌شده (شامل ارز ذخیره‌شده، حتی اگر با ارز فعلی نصب نخواند) را صادقانه برمی‌گرداند.
/// </remarks>
public sealed class GetShippingAuthoringGroupRateRowsHandler(IShippingAuthoringDraftDirectory directory)
    : IRequestHandler<GetShippingAuthoringGroupRateRowsQuery, Result<ShippingAuthoringGroupRateRowsItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringGroupRateRowsItem>> Handle(
        GetShippingAuthoringGroupRateRowsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);

        var draft = await directory.GetAsync(request.Owner, request.DraftId, cancellationToken);
        if (draft.IsFailure)
        {
            return Result.Failure<ShippingAuthoringGroupRateRowsItem>(draft.Errors);
        }

        return draft.Value.FindRegionGroup(request.GroupId) is { } group
            ? Result.Success(ShippingAuthoringDraftWorkflow.MapRateRows(draft.Value, group))
            : Result.Failure<ShippingAuthoringGroupRateRowsItem>(
                new SemanticError(ShippingErrorCodes.AuthoringRegionGroupNotFound));
    }
}
