using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;

namespace Tooba.Order.Application.Storefront.Checkout.Queries.GetStorefrontPaymentDraft;

/// <summary>
/// پیش‌نویس پرداخت پیش از سفارش، آدرس‌دهی‌شده با CartId (نه checkoutId):
/// سبد باید فعال و دارای انتخاب ارسال ذخیره‌شده باشد. خروجی همان ارزیابی تجاری authoritative است
/// و <c>CheckoutId = null</c> دارد؛ هیچ سفارش/رزروی در این خواندن ساخته نمی‌شود.
/// </summary>
public sealed record GetStorefrontPaymentDraftQuery(
    Guid CartId,
    string? GuestSecret,
    string? CouponCode) : IRequest<Result<StorefrontCheckoutPage>>;

public sealed class GetStorefrontPaymentDraftHandler(StorefrontShippingService shipping)
    : IRequestHandler<GetStorefrontPaymentDraftQuery, Result<StorefrontCheckoutPage>>
{
    public Task<Result<StorefrontCheckoutPage>> Handle(
        GetStorefrontPaymentDraftQuery request,
        CancellationToken cancellationToken)
        => StorefrontOrderResult.ExecuteAsync(() => shipping.ProjectPaymentDraftAsync(
            request.CartId,
            request.GuestSecret,
            request.CouponCode,
            cancellationToken));
}
