using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Fulfillment.Contracts;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Infrastructure.Errors;

/// <summary>کاتالوگ صریح کدهای خطای Fulfillment.</summary>
public sealed class FulfillmentErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(FulfillmentErrorCodes.Missing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Fulfillment was not found."),
        D(FulfillmentErrorCodes.Rejected, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Fulfillment mutation was rejected."),
        D(FulfillmentErrorCodes.SellerOrderHandleDenied, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden,
            "Seller lacks order.handle permission."),
        D(FulfillmentErrorCodes.SellerOrderMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Seller order was not found."),
        D(FulfillmentErrorCodes.SellerOrderHandleScopeDenied, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden,
            "Order.handle category scope does not cover all lines."),
        D(FulfillmentErrorCodes.CustomerActorMissing, ErrorClassification.Forbidden, StatusCodes.Status401Unauthorized,
            "Customer actor is missing."),
        D(FulfillmentErrorCodes.CustomerOrderMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Customer order was not found."),
        D(FulfillmentErrorCodes.WorkQueueBulkFailed, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Fulfillment work-queue bulk operation failed."),
        D(FulfillmentErrorCodes.WorkQueueBulkUnsupported, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Fulfillment work-queue bulk action is unsupported."),
        D(FulfillmentErrorCodes.WorkQueueBulkEmpty, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Fulfillment work-queue bulk selection is empty."),
        D(FulfillmentErrorCodes.WorkQueueCrossSeller, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Fulfillment work-queue bulk cannot span sellers."),
        D(FulfillmentErrorCodes.WorkQueueRowMismatch, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Fulfillment work-queue row does not match server state."),
        D(FulfillmentErrorCodes.WorkQueueIncompatible, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Fulfillment work-queue bulk selection is incompatible."),
        D(FulfillmentErrorCodes.WorkQueueShipmentMissing, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Fulfillment work-queue shipment is missing."),
    ];

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(code, classification, status, code, ErrorSeverity.Warning, fallback);
}
