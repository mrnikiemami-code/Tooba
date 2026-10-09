using Tooba.Order.Contracts.Fulfillment;
using System.Linq.Expressions;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Catalog.Contracts.Checkout;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Promotion.Contracts.Checkout;
using Tooba.Promotion.Contracts.Merchandising;
namespace Tooba.Order.Infrastructure.Checkout.Persistence;

public sealed partial class CheckoutDirectory : ICheckoutDirectory
{
    internal static void EnsureAccess(CheckoutGroup group, OrderAccess access)
    {
        if (!group.CanBeViewedBy(access.BuyerPartyId, access.PlacedByUserId))
        {
            throw new InvalidOperationException("دسترسی به سفارش بدون هویت خریدار یا کاربر عامل رد شد؛ شمارهٔ سفارش Bearer نیست.");
        }
    }

    private static string BuildOrderNumber(DateTimeOffset now, int sequence) =>
        $"TB-{now.UtcDateTime:yyyyMMddHHmmss}-{sequence:D2}-{Guid.NewGuid().ToString("N")[..6]}";

    internal static CheckoutSnapshot ToSnapshot(CheckoutGroup group) =>
        new(
            group.CheckoutId,
            group.CartId,
            group.Mode,
            group.BuyerPartyId,
            group.PlacedByUserId,
            group.Market,
            group.Currency,
            group.Channel,
            group.SubmittedAt,
            group.SellerOrders.Select(ToSellerSnapshot).ToList(),
            group.RecipientName,
            group.ContactMobile,
            group.ProvinceName,
            group.CityName,
            group.PostalAddress,
            group.PostalCode,
            group.ShippingMethodCode,
            group.ShippingMethodLabel,
            group.ShippingAmount,
            group.MinimumDeliveryDate,
            group.RequestedDeliveryDate,
            group.RequestedDeliveryTimeWindow,
            group.CustomerNote,
            group.RecipientFirstName,
            group.RecipientLastName);

    private static SellerOrderSnapshot ToSellerSnapshot(SellerOrder order) =>
        new(
            order.SellerOrderId,
            order.OrderNumber,
            order.SellerPartyId,
            order.Status,
            order.SubtotalSnapshot,
            order.TaxSnapshot,
            order.DiscountSnapshot,
            order.GrandTotalSnapshot,
            order.Currency,
            order.Lines.Select(line => new OrderLineSnapshot(
                line.LineId,
                line.OfferId,
                line.CatalogVariantId,
                line.SellerPartyId,
                line.Quantity,
                line.UnitPriceSnapshot,
                line.LineTotalSnapshot,
                line.Currency,
                line.TaxExclusive,
                line.PriceId,
                line.ReservationId,
                line.TaxOutcomeSnapshot,
                line.TaxRateSnapshot,
                line.TaxAmountSnapshot,
                line.TaxInclusiveSnapshot,
                line.TaxRuleIdSnapshot,
                line.DiscountAmountSnapshot,
                line.PromotionIdSnapshot,
                line.PromotionNameSnapshot,
                line.PromotionCodeSnapshot,
                line.DiscountKindSnapshot,
                line.PreDiscountTaxExclusiveSnapshot,
                line.PostDiscountTaxExclusiveSnapshot,
                line.PromotionAppliedAtSnapshot,
                line.UnitOfMeasureIdSnapshot,
                line.UnitCodeSnapshot,
                line.UnitDisplaySnapshot,
                line.QuantityDecimalPlacesSnapshot,
                line.QuantityStepSnapshot)).ToList());

    /// <summary>
    /// نقل‌قول خط checkout: کمپین واجد شرایط در صورت وجود، وگرنه Base.
    /// </summary>
    private async Task<PriceQuote?> ResolveCheckoutLineQuoteAsync(
        CartSnapshot cart,
        CartLineSnapshot cartLine,
        string effectiveCurrency,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (cartLine.MerchandisingCampaignId is Guid campaignId
            && campaignId != Guid.Empty
            && _campaignPrices is not null)
        {
            var campaignQuote = await _campaignPrices.TryResolveEligibleCampaignPriceAsync(
                campaignId,
                cartLine.OfferId,
                cart.Market,
                cart.Channel,
                effectiveCurrency,
                now,
                cancellationToken);
            if (campaignQuote is not null)
            {
                return campaignQuote;
            }
        }

        return await _prices.ResolvePriceAsync(
            new PriceResolutionQuery(
                cartLine.OfferId,
                cart.Market,
                cart.Channel,
                effectiveCurrency,
                now,
                null,
                null,
                cartLine.Quantity),
            cancellationToken);
    }
}

