using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront.Services;

namespace Tooba.Order.Application.Storefront.PendingPayment.Commands.CancelPendingCheckout;

public sealed record CancelPendingCheckoutCommand(Guid CheckoutId, Guid CartId, string? GuestSecret)
    : IRequest<Result<object>>;

public sealed class CancelPendingCheckoutHandler(StorefrontPendingPaymentService pending)
    : IRequestHandler<CancelPendingCheckoutCommand, Result<object>>
{
    public Task<Result<object>> Handle(CancelPendingCheckoutCommand request, CancellationToken cancellationToken)
        => StorefrontOrderResult.ExecuteObjectAsync(
            () => pending.CancelAsync(request.CheckoutId, request.CartId, request.GuestSecret, cancellationToken),
            cancellationToken);
}
