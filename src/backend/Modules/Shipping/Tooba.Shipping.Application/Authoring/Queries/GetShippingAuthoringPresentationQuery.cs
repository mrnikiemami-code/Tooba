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
/// خواندن سند ارائهٔ یک پیش‌نویس مالک: متن ذخیره‌شده به‌ازای زبان + registry فعال + زبان پیش‌فرض.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
public sealed record GetShippingAuthoringPresentationQuery(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId)
    : IRequest<Result<ShippingAuthoringPresentationItem>>;

/// <summary>
/// هندلر خواندن ارائه. زبان هرگز از locale رابط یا query string انتخاب نمی‌شود: پاسخ همهٔ متن‌های
/// ذخیره‌شده را خام برمی‌گرداند و زبان‌های فعال را از همان منبع authoritative TB-188 می‌آورد.
/// </summary>
public sealed class GetShippingAuthoringPresentationHandler(
    IShippingAuthoringDraftDirectory directory,
    IShippingAuthoringContextSource contextSource)
    : IRequestHandler<GetShippingAuthoringPresentationQuery, Result<ShippingAuthoringPresentationItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringPresentationItem>> Handle(
        GetShippingAuthoringPresentationQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);

        var draft = await directory.GetAsync(request.Owner, request.DraftId, cancellationToken);
        if (draft.IsFailure)
        {
            return Result.Failure<ShippingAuthoringPresentationItem>(draft.Errors);
        }

        var snapshot = await contextSource.ReadAsync(cancellationToken);
        var context = ShippingAuthoringContextProjection.Project(snapshot);
        if (context.IsFailure)
        {
            // Invalid/unavailable language/default/direction configuration fails closed; stored text is
            // never presented against a fabricated registry.
            return Result.Failure<ShippingAuthoringPresentationItem>(context.Errors);
        }

        return Result.Success(ShippingAuthoringPresentationWorkflow.Map(draft.Value, context.Value));
    }
}
