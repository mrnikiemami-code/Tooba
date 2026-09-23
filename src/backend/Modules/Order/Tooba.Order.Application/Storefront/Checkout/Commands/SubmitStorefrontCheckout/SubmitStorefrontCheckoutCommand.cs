using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Application.Storefront.Checkout.Commands.SubmitStorefrontCheckout;

public sealed record SubmitStorefrontCheckoutCommand(
    Guid CartId,
    string? GuestSecret,
    int ExpectedCartVersion,
    string IdempotencyKey,
    StorefrontCheckoutShippingInput Shipping,
    string? CouponCode) : IRequest<Result<StorefrontCheckoutPage>>;

public sealed class SubmitStorefrontCheckoutHandler(StorefrontCheckoutService checkout, ICheckoutAbuseGate abuse)
    : IRequestHandler<SubmitStorefrontCheckoutCommand, Result<StorefrontCheckoutPage>>
{
    public Task<Result<StorefrontCheckoutPage>> Handle(SubmitStorefrontCheckoutCommand request, CancellationToken cancellationToken)
        => StorefrontOrderResult.ExecuteAsync(
            () => checkout.SubmitAsync(
                request.CartId,
                request.GuestSecret,
                request.ExpectedCartVersion,
                request.IdempotencyKey,
                request.Shipping,
                request.CouponCode,
                cancellationToken),
            abuse,
            cancellationToken);
}
