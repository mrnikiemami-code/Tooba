using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Events;
using Tooba.Notification.Application.Rendering;
using Tooba.Notification.Contracts.Routes;
using Tooba.Notification.Infrastructure.Projectors;
using Tooba.Payment.Contracts.Events;
using Tooba.Returns.Contracts.Events;

namespace Tooba.Notification.Infrastructure.Handlers;

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
