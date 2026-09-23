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

namespace Tooba.Order.Application.Storefront.Shipping.Commands.CommitStorefrontShipping;

public sealed record CommitStorefrontShippingCommand(
    Guid CartId,
    string? GuestSecret,
    int ExpectedCartVersion,
    string IdempotencyKey,
    string? CouponCode) : IRequest<Result<StorefrontCheckoutPage>>;

public sealed class CommitStorefrontShippingHandler(StorefrontShippingService shipping, ICheckoutAbuseGate abuse)
    : IRequestHandler<CommitStorefrontShippingCommand, Result<StorefrontCheckoutPage>>
{
    public Task<Result<StorefrontCheckoutPage>> Handle(CommitStorefrontShippingCommand request, CancellationToken cancellationToken)
        => StorefrontOrderResult.ExecuteAsync(
            () => shipping.CommitAsync(
                request.CartId,
                request.GuestSecret,
                request.ExpectedCartVersion,
                request.IdempotencyKey,
                request.CouponCode,
                cancellationToken),
            abuse,
            cancellationToken);
}
