using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront.Services;

namespace Tooba.Order.Application.Storefront.PendingPayment.Commands.HidePendingPaymentCard;

public sealed record HidePendingPaymentCardCommand(Guid CheckoutId, Guid CartId, string? GuestSecret)
    : IRequest<Result<object>>;

public sealed class HidePendingPaymentCardHandler(StorefrontPendingPaymentService pending)
    : IRequestHandler<HidePendingPaymentCardCommand, Result<object>>
{
    public Task<Result<object>> Handle(HidePendingPaymentCardCommand request, CancellationToken cancellationToken)
        => StorefrontOrderResult.ExecuteObjectAsync(
            () => pending.HidePendingCardAsync(request.CheckoutId, request.CartId, request.GuestSecret, cancellationToken),
            cancellationToken);
}
