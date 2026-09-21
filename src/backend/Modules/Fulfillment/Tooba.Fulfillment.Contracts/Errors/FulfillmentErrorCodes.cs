namespace Tooba.Fulfillment.Contracts.Errors;

/// <summary>Stable semantic error codes owned by Fulfillment.</summary>
public static class FulfillmentErrorCodes
{
    /// <summary>Fulfillment unit was not found.</summary>
    public const string Missing = "fulfillment.missing";

    /// <summary>Mutation rejected by domain rules.</summary>
    public const string Rejected = "fulfillment.rejected";

    /// <summary>Seller lacks order.handle permission.</summary>
    public const string SellerOrderHandleDenied = "seller.order.handle.denied";

    /// <summary>Seller order was not found for auth.</summary>
    public const string SellerOrderMissing = "seller.order.missing";

    /// <summary>Category-scoped order.handle does not cover all lines.</summary>
    public const string SellerOrderHandleScopeDenied = "seller.order.handle.scope_denied";

    /// <summary>Customer actor missing.</summary>
    public const string CustomerActorMissing = "customer.actor.missing";

    /// <summary>Customer order missing / not owned.</summary>
    public const string CustomerOrderMissing = "customer.order.missing";

    /// <summary>Work-queue bulk failed.</summary>
    public const string WorkQueueBulkFailed = "fulfillment.work_queue.bulk_failed";

    /// <summary>Unsupported bulk action.</summary>
    public const string WorkQueueBulkUnsupported = "fulfillment.work_queue.bulk_unsupported";

    /// <summary>Bulk selection empty.</summary>
    public const string WorkQueueBulkEmpty = "fulfillment.work_queue.bulk_empty";

    /// <summary>Bulk spans multiple sellers.</summary>
    public const string WorkQueueCrossSeller = "fulfillment.work_queue.cross_seller";

    /// <summary>Bulk row mismatch with server state.</summary>
    public const string WorkQueueRowMismatch = "fulfillment.work_queue.row_mismatch";

    /// <summary>Bulk selection incompatible for shared action.</summary>
    public const string WorkQueueIncompatible = "fulfillment.work_queue.incompatible";

    /// <summary>Shipment missing for bulk dispatch/deliver.</summary>
    public const string WorkQueueShipmentMissing = "fulfillment.work_queue.shipment_missing";
}
