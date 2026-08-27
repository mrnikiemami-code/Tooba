using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Notification.Application;
using Tooba.Notification.Domain;
using Tooba.Order.Application;
using Tooba.Payment.Application;
using Tooba.Returns.Application;

namespace Tooba.Notification.Infrastructure;

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
        {
            return;
        }

        await CreateCustomerAsync(
            recipients,
            sourceEventId,
            sourceType,
            customerType,
            customerPayload,
            customerTarget,
            cancellationToken);

        if (sellerType is null || sellerPayload is null || sellerTargetFactory is null)
        {
            return;
        }

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
        {
            return;
        }

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
        {
            return;
        }

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
        // هویت پنل مشتری = PlacedByUserId؛ BuyerPartyId در payload نگه داشته می‌شود.
        var recipientPartyId = recipients.PlacedByUserId;
        var created = await _directory.CreateIfAbsentAsync(
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
        _ = created;
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
        var created = await _directory.CreateIfAbsentAsync(
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
        _ = created;
    }

    private static object EnrichBuyer(object payload, Guid? buyerPartyId)
    {
        // payloadهای anonymous از قبل ساخته شده‌اند؛ buyerPartyId جدا در Create ذخیره نمی‌شود مگر در همان object.
        _ = buyerPartyId;
        return payload;
    }
}

/// <summary>مصرف payment.succeeded.v1 → مشتری + فروشندگان.</summary>
public sealed class NotificationPaymentSucceededHandler : IIntegrationEventHandler<PaymentSucceededIntegrationEvent>
{
    private readonly NotificationProjector _projector;

    /// <summary>handler را به پروژکتور وصل می‌کند.</summary>
    public NotificationPaymentSucceededHandler(NotificationProjector projector) => _projector = projector;

    /// <inheritdoc />
    public Task HandleAsync(PaymentSucceededIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var sourceEventId = integrationEvent.Metadata.EventId.ToString("D");
        var payload = new
        {
            checkoutId = integrationEvent.CheckoutId,
            paymentId = integrationEvent.PaymentId,
            amount = integrationEvent.Amount,
            currency = integrationEvent.Currency,
            sellerOrderIds = integrationEvent.SellerOrderIds,
        };
        return _projector.ProjectFromCheckoutAsync(
            integrationEvent.CheckoutId,
            sourceEventId,
            PaymentSucceededIntegrationEvent.EventTypeName,
            NotificationCopy.PaymentSucceeded,
            NotificationCopy.OrderPaidSeller,
            payload,
            payload,
            NotificationTargetRoutes.CustomerOrder(integrationEvent.CheckoutId),
            sellerOrderId => NotificationTargetRoutes.SellerOrder(sellerOrderId),
            cancellationToken);
    }
}

/// <summary>مصرف payment.failed.v1 → فقط مشتری.</summary>
public sealed class NotificationPaymentFailedHandler : IIntegrationEventHandler<PaymentFailedIntegrationEvent>
{
    private readonly NotificationProjector _projector;

    /// <summary>handler را به پروژکتور وصل می‌کند.</summary>
    public NotificationPaymentFailedHandler(NotificationProjector projector) => _projector = projector;

    /// <inheritdoc />
    public Task HandleAsync(PaymentFailedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var sourceEventId = integrationEvent.Metadata.EventId.ToString("D");
        var payload = new
        {
            checkoutId = integrationEvent.CheckoutId,
            paymentId = integrationEvent.PaymentId,
            failureCode = integrationEvent.FailureCode,
        };
        return _projector.ProjectFromCheckoutAsync(
            integrationEvent.CheckoutId,
            sourceEventId,
            PaymentFailedIntegrationEvent.EventTypeName,
            NotificationCopy.PaymentFailed,
            sellerType: null,
            payload,
            sellerPayload: null,
            NotificationTargetRoutes.CustomerPaymentResult(integrationEvent.CheckoutId),
            sellerTargetFactory: null,
            cancellationToken);
    }
}

/// <summary>مصرف fulfillment.created.v1 → مشتری + فروشنده.</summary>
public sealed class NotificationFulfillmentCreatedHandler : IIntegrationEventHandler<FulfillmentCreatedIntegrationEvent>
{
    private readonly NotificationProjector _projector;

    /// <summary>handler را به پروژکتور وصل می‌کند.</summary>
    public NotificationFulfillmentCreatedHandler(NotificationProjector projector) => _projector = projector;

    /// <inheritdoc />
    public Task HandleAsync(FulfillmentCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var sourceEventId = integrationEvent.Metadata.EventId.ToString("D");
        var payload = new
        {
            fulfillmentId = integrationEvent.FulfillmentId,
            sellerOrderId = integrationEvent.SellerOrderId,
            checkoutId = integrationEvent.CheckoutId,
        };
        return _projector.ProjectFromSellerOrderAsync(
            integrationEvent.SellerOrderId,
            sourceEventId,
            FulfillmentCreatedIntegrationEvent.EventTypeName,
            NotificationCopy.FulfillmentCreated,
            NotificationCopy.FulfillmentCreated,
            payload,
            checkoutId => NotificationTargetRoutes.CustomerOrder(checkoutId),
            NotificationTargetRoutes.SellerOrder(integrationEvent.SellerOrderId),
            cancellationToken);
    }
}

/// <summary>مصرف shipment.dispatched.v1 → مشتری + فروشنده.</summary>
public sealed class NotificationShipmentDispatchedHandler : IIntegrationEventHandler<ShipmentDispatchedIntegrationEvent>
{
    private readonly NotificationProjector _projector;

    /// <summary>handler را به پروژکتور وصل می‌کند.</summary>
    public NotificationShipmentDispatchedHandler(NotificationProjector projector) => _projector = projector;

    /// <inheritdoc />
    public Task HandleAsync(ShipmentDispatchedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var sourceEventId = integrationEvent.Metadata.EventId.ToString("D");
        var payload = new
        {
            fulfillmentId = integrationEvent.FulfillmentId,
            shipmentId = integrationEvent.ShipmentId,
            sellerOrderId = integrationEvent.SellerOrderId,
        };
        return _projector.ProjectFromSellerOrderAsync(
            integrationEvent.SellerOrderId,
            sourceEventId,
            ShipmentDispatchedIntegrationEvent.EventTypeName,
            NotificationCopy.ShipmentDispatched,
            NotificationCopy.ShipmentDispatched,
            payload,
            checkoutId => NotificationTargetRoutes.CustomerOrder(checkoutId),
            NotificationTargetRoutes.SellerOrder(integrationEvent.SellerOrderId),
            cancellationToken);
    }
}

/// <summary>مصرف return.requested.v1 → مشتری + فروشنده.</summary>
public sealed class NotificationReturnRequestedHandler : IIntegrationEventHandler<ReturnRequestedIntegrationEvent>
{
    private readonly NotificationProjector _projector;

    /// <summary>handler را به پروژکتور وصل می‌کند.</summary>
    public NotificationReturnRequestedHandler(NotificationProjector projector) => _projector = projector;

    /// <inheritdoc />
    public Task HandleAsync(ReturnRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var sourceEventId = integrationEvent.Metadata.EventId.ToString("D");
        var payload = new
        {
            returnRequestId = integrationEvent.ReturnRequestId,
            sellerOrderId = integrationEvent.SellerOrderId,
            checkoutId = integrationEvent.CheckoutId,
        };
        return _projector.ProjectFromSellerOrderAsync(
            integrationEvent.SellerOrderId,
            sourceEventId,
            ReturnRequestedIntegrationEvent.EventTypeName,
            NotificationCopy.ReturnRequested,
            NotificationCopy.ReturnRequested,
            payload,
            _ => NotificationTargetRoutes.CustomerReturn(integrationEvent.ReturnRequestId),
            NotificationTargetRoutes.SellerReturn(integrationEvent.ReturnRequestId),
            cancellationToken);
    }
}

/// <summary>مصرف return.approved.v1 → مشتری + فروشنده.</summary>
public sealed class NotificationReturnApprovedHandler : IIntegrationEventHandler<ReturnApprovedIntegrationEvent>
{
    private readonly NotificationProjector _projector;

    /// <summary>handler را به پروژکتور وصل می‌کند.</summary>
    public NotificationReturnApprovedHandler(NotificationProjector projector) => _projector = projector;

    /// <inheritdoc />
    public Task HandleAsync(ReturnApprovedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var sourceEventId = integrationEvent.Metadata.EventId.ToString("D");
        var payload = new
        {
            returnRequestId = integrationEvent.ReturnRequestId,
            sellerOrderId = integrationEvent.SellerOrderId,
            checkoutId = integrationEvent.CheckoutId,
            refundAmount = integrationEvent.RefundAmount,
            currency = integrationEvent.Currency,
        };
        return _projector.ProjectFromSellerOrderAsync(
            integrationEvent.SellerOrderId,
            sourceEventId,
            ReturnApprovedIntegrationEvent.EventTypeName,
            NotificationCopy.ReturnApproved,
            NotificationCopy.ReturnApproved,
            payload,
            _ => NotificationTargetRoutes.CustomerReturn(integrationEvent.ReturnRequestId),
            NotificationTargetRoutes.SellerReturn(integrationEvent.ReturnRequestId),
            cancellationToken);
    }
}

/// <summary>مصرف refund.succeeded.v1 → مشتری + فروشنده.</summary>
public sealed class NotificationRefundSucceededHandler : IIntegrationEventHandler<RefundSucceededIntegrationEvent>
{
    private readonly NotificationProjector _projector;

    /// <summary>handler را به پروژکتور وصل می‌کند.</summary>
    public NotificationRefundSucceededHandler(NotificationProjector projector) => _projector = projector;

    /// <inheritdoc />
    public Task HandleAsync(RefundSucceededIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var sourceEventId = integrationEvent.Metadata.EventId.ToString("D");
        var payload = new
        {
            returnRequestId = integrationEvent.ReturnRequestId,
            sellerOrderId = integrationEvent.SellerOrderId,
            paymentId = integrationEvent.PaymentId,
            refundAmount = integrationEvent.RefundAmount,
            currency = integrationEvent.Currency,
        };
        return _projector.ProjectFromSellerOrderAsync(
            integrationEvent.SellerOrderId,
            sourceEventId,
            RefundSucceededIntegrationEvent.EventTypeName,
            NotificationCopy.RefundSucceeded,
            NotificationCopy.RefundSucceeded,
            payload,
            _ => NotificationTargetRoutes.CustomerReturn(integrationEvent.ReturnRequestId),
            NotificationTargetRoutes.SellerReturn(integrationEvent.ReturnRequestId),
            cancellationToken);
    }
}
