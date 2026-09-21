using Tooba.BuildingBlocks;
using Tooba.Notification.Application.Ports;
using Tooba.Notification.Application.Rendering;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Dtos;
using Tooba.Notification.Contracts.Routes;
using Tooba.Order.Application;

namespace Tooba.Notification.Infrastructure.Projectors;

/// <summary>
/// پروجکشن اعلان از snapshot گیرندگان Order بدون cross-DbContext.
/// </summary>
public sealed class NotificationProjector
{
    private readonly INotificationDirectory _directory;
    private readonly IOrderNotificationReader _orders;

    /// <summary>پروژکتور را به دایرکتوری و Order reader وصل می‌کند.</summary>
    public NotificationProjector(
        INotificationDirectory directory,
        IOrderNotificationReader orders)
    {
        _directory = directory;
        _orders = orders;
    }

    /// <summary>اعلان مشتری و فروشندگان checkout را از CheckoutId می‌سازد.</summary>
    public async Task ProjectFromCheckoutAsync(
        Guid checkoutId,
        string sourceEventId,
        string sourceType,
        string customerType,
        string? sellerType,
        object customerPayload,
        object? sellerPayload,
        string customerTarget,
        Func<Guid, string>? sellerTargetFactory,
        CancellationToken cancellationToken)
    {
        var recipients = await _orders.GetByCheckoutIdAsync(checkoutId, cancellationToken);
        if (recipients is null)
            return;

        await CreateCustomerAsync(
            recipients,
            sourceEventId,
            sourceType,
            customerType,
            customerPayload,
            customerTarget,
            cancellationToken);

        if (sellerType is null || sellerPayload is null || sellerTargetFactory is null)
            return;

        foreach (var seller in recipients.Sellers)
        {
            await CreateSellerAsync(
                seller.SellerPartyId,
                sourceEventId,
                sourceType,
                sellerType,
                sellerPayload,
                sellerTargetFactory(seller.SellerOrderId),
                cancellationToken);
        }
    }

    /// <summary>اعلان مشتری و فروشندهٔ یک SellerOrder را می‌سازد.</summary>
    public async Task ProjectFromSellerOrderAsync(
        Guid sellerOrderId,
        string sourceEventId,
        string sourceType,
        string customerType,
        string sellerType,
        object payload,
        Func<Guid, string> customerTargetFactory,
        string sellerTarget,
        CancellationToken cancellationToken)
    {
        var recipients = await _orders.GetBySellerOrderIdAsync(sellerOrderId, cancellationToken);
        if (recipients is null)
            return;

        await CreateCustomerAsync(
            recipients,
            sourceEventId,
            sourceType,
            customerType,
            payload,
            customerTargetFactory(recipients.CheckoutId),
            cancellationToken);

        var seller = recipients.Sellers.FirstOrDefault(x => x.SellerOrderId == sellerOrderId)
            ?? recipients.Sellers.FirstOrDefault();
        if (seller is null)
            return;

        await CreateSellerAsync(
            seller.SellerPartyId,
            sourceEventId,
            sourceType,
            sellerType,
            payload,
            sellerTarget,
            cancellationToken);
    }

    private async Task CreateCustomerAsync(
        OrderNotificationRecipientSnapshot recipients,
        string sourceEventId,
        string sourceType,
        string type,
        object payload,
        string targetRoute,
        CancellationToken cancellationToken)
    {
        var recipientPartyId = recipients.PlacedByUserId;
        await _directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Customer,
                recipientPartyId,
                recipients.PlacedByUserId,
                type,
                EnrichBuyer(payload, recipients.BuyerPartyId),
                targetRoute,
                sourceEventId,
                sourceType),
            cancellationToken);
    }

    private async Task CreateSellerAsync(
        Guid sellerPartyId,
        string sourceEventId,
        string sourceType,
        string type,
        object payload,
        string targetRoute,
        CancellationToken cancellationToken)
    {
        await _directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Seller,
                sellerPartyId,
                null,
                type,
                payload,
                targetRoute,
                sourceEventId,
                sourceType),
            cancellationToken);
    }

    private static object EnrichBuyer(object payload, Guid? buyerPartyId)
    {
        _ = buyerPartyId;
        return payload;
    }
}
