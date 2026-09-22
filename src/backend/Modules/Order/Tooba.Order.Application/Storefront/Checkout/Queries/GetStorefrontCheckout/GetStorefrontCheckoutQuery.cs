using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Payment.Contracts.Customer;

namespace Tooba.Order.Application.Storefront.Checkout.Queries.GetStorefrontCheckout;

public sealed record GetStorefrontCheckoutQuery(Guid CheckoutId, Guid CartId, string? GuestSecret)
    : IRequest<Result<StorefrontCheckoutPage>>;

public sealed class GetStorefrontCheckoutHandler(
    StorefrontCheckoutService checkout,
    IPaymentCustomerGateway payments)
    : IRequestHandler<GetStorefrontCheckoutQuery, Result<StorefrontCheckoutPage>>
{
    public async Task<Result<StorefrontCheckoutPage>> Handle(GetStorefrontCheckoutQuery request, CancellationToken cancellationToken)
        => await StorefrontOrderResult.ExecuteAsync(async () =>
        {
            var page = await checkout.GetAsync(request.CheckoutId, request.CartId, request.GuestSecret, cancellationToken)
                ?? throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutMissing);
            if (page.CheckoutId is Guid id
                && await payments.HasSucceededPaymentForCheckoutAsync(id, cancellationToken))
            {
                return page with { PaymentState = "Paid", CanInitiatePayment = false };
            }

            return page;
        });
}
