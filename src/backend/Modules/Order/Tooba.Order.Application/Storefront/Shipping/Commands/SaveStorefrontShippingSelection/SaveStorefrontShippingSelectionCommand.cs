using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;

namespace Tooba.Order.Application.Storefront.Shipping.Commands.SaveStorefrontShippingSelection;

public sealed record SaveStorefrontShippingSelectionCommand(
    StorefrontShippingSelectionRequest Body,
    string? GuestSecret) : IRequest<Result<StorefrontShippingDraftView>>;

public sealed class SaveStorefrontShippingSelectionHandler(StorefrontShippingService shipping)
    : IRequestHandler<SaveStorefrontShippingSelectionCommand, Result<StorefrontShippingDraftView>>
{
    public Task<Result<StorefrontShippingDraftView>> Handle(SaveStorefrontShippingSelectionCommand request, CancellationToken cancellationToken)
        => StorefrontOrderResult.ExecuteAsync(() => shipping.SaveSelectionAsync(request.Body, request.GuestSecret, cancellationToken));
}
