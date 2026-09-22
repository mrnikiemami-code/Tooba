using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;

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
