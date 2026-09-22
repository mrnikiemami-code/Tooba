using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;

namespace Tooba.Order.Application.Storefront.Checkout.Queries.PreviewStorefrontCheckout;

public sealed record PreviewStorefrontCheckoutQuery(Guid CartId, string? GuestSecret, string? CouponCode)
    : IRequest<Result<StorefrontCheckoutPage>>;

public sealed class PreviewStorefrontCheckoutHandler(StorefrontCheckoutService checkout)
    : IRequestHandler<PreviewStorefrontCheckoutQuery, Result<StorefrontCheckoutPage>>
{
    public async Task<Result<StorefrontCheckoutPage>> Handle(PreviewStorefrontCheckoutQuery request, CancellationToken cancellationToken)
        => await StorefrontOrderResult.ExecuteAsync(() => checkout.PreviewAsync(request.CartId, request.GuestSecret, request.CouponCode, cancellationToken));
}
