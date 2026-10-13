using Tooba.BuildingBlocks;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Contracts.Payments;

namespace Tooba.Order.Infrastructure.Integrations.Payment;

/// <summary>
/// Contracts-only adapter that values the pre-order payable of one Active cart for tender quotation.
/// Reuses the authoritative 011 payment-draft evaluation (persisted shipping selection + effective
/// commerce context); it never creates a checkout/order/reservation, never converts the cart and
/// never recomputes amounts. Every cart fault is mapped to a typed,
/// fail-closed <see cref="CartPaymentQuoteStatus"/> — no Order code leaks across the boundary.
/// </summary>
public sealed class CartPaymentQuoteBridge(StorefrontShippingService shipping) : ICartPaymentQuoteReader
{
    /// <inheritdoc />
    public async Task<CartPaymentQuoteResult> GetForCartAsync(
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        if (cartId == Guid.Empty)
        {
            return CartPaymentQuoteResult.Failed(CartPaymentQuoteStatus.CartMissing);
        }

        try
        {
            var page = await shipping.ProjectPaymentDraftAsync(
                cartId,
                guestSecret,
                couponCode: null,
                cancellationToken);

            return CartPaymentQuoteResult.Ready(new CartPaymentQuoteSnapshot(
                page.CartId,
                page.CartVersion,
                page.Currency,
                page.SubtotalExclusiveOfTax,
                page.DiscountAmount,
                page.TaxAmount,
                page.ShippingAmount,
                page.PayableAmount,
                page.ShippingMethodCode,
                page.ShippingMethodLabel,
                page.SellerOrders.Count));
        }
        catch (StorefrontOrderException exception)
        {
            return CartPaymentQuoteResult.Failed(Map(exception.Code));
        }
        catch (ContractOperationException exception)
        {
            return CartPaymentQuoteResult.Failed(Map(exception.Code));
        }
    }

    private static CartPaymentQuoteStatus Map(string? code) => code switch
    {
        StorefrontOrderErrors.ShippingCartMissing => CartPaymentQuoteStatus.CartMissing,
        StorefrontOrderErrors.CheckoutCartMissing => CartPaymentQuoteStatus.CartMissing,
        StorefrontOrderErrors.ShippingCartForbidden => CartPaymentQuoteStatus.CartForbidden,
        StorefrontOrderErrors.ShippingAddressForbidden => CartPaymentQuoteStatus.CartForbidden,
        StorefrontOrderErrors.CheckoutAccessDenied => CartPaymentQuoteStatus.CartForbidden,
        StorefrontOrderErrors.ShippingCartEmpty => CartPaymentQuoteStatus.CartEmpty,
        StorefrontOrderErrors.CheckoutCartEmpty => CartPaymentQuoteStatus.CartEmpty,
        StorefrontOrderErrors.ShippingSelectionRequired => CartPaymentQuoteStatus.ShippingSelectionRequired,
        StorefrontOrderErrors.CheckoutShippingIncomplete => CartPaymentQuoteStatus.ShippingSelectionRequired,
        StorefrontOrderErrors.ShippingCartStale => CartPaymentQuoteStatus.CartNotActive,
        StorefrontOrderErrors.CheckoutCartExpired => CartPaymentQuoteStatus.CartNotActive,
        StorefrontOrderErrors.CheckoutVersionConflict => CartPaymentQuoteStatus.CartNotActive,
        _ => CartPaymentQuoteStatus.Unavailable,
    };
}
