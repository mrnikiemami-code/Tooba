using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront.Services;

namespace Tooba.Order.Application.Storefront.PendingPayment.Commands.RetryPendingCheckoutReservation;

/// <summary>
/// «بررسی موجودی و پرداخت مجدد» روی checkout مالک — بدون نیاز به paymentId.
/// سفارش unpaid ممکن است هنوز هیچ ردیف پرداختی نداشته باشد.
/// </summary>
public sealed record RetryPendingCheckoutReservationCommand(Guid CheckoutId, Guid CartId, string? GuestSecret)
    : IRequest<Result<object>>;

public sealed class RetryPendingCheckoutReservationHandler(StorefrontPendingPaymentService pending)
    : IRequestHandler<RetryPendingCheckoutReservationCommand, Result<object>>
{
    public Task<Result<object>> Handle(
        RetryPendingCheckoutReservationCommand request,
        CancellationToken cancellationToken)
        => StorefrontOrderResult.ExecuteObjectAsync(
            () => pending.RetryExpiredReservationAsync(
                request.CheckoutId,
                request.CartId,
                request.GuestSecret,
                cancellationToken),
            cancellationToken);
}
