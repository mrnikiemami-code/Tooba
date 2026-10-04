namespace Tooba.Fulfillment.Contracts.Errors;

/// <summary>
/// Stable semantic error codes owned by Fulfillment.
/// Values are the machine-stable wire contract; do not rename or repurpose a published value.
/// </summary>
public static class FulfillmentErrorCodes
{
    // ---- Fulfillment unit / lifecycle -------------------------------------------------
    /// <summary>Fulfillment unit was not found.</summary>
    public const string Missing = "fulfillment.missing";

    /// <summary>Fulfillment unit was not found (legacy alias value of <see cref="Missing"/>).</summary>
    public const string NotFound = "fulfillment.not_found";

    /// <summary>Mutation rejected by domain rules.</summary>
    public const string Rejected = "fulfillment.rejected";

    /// <summary>Source order was not found.</summary>
    public const string OrderNotFound = "fulfillment.order.not_found";

    /// <summary>Source order is not paid.</summary>
    public const string OrderNotPaid = "fulfillment.order.not_paid";

    /// <summary>Order line was not found.</summary>
    public const string OrderLineNotFound = "fulfillment.order_line.not_found";

    /// <summary>Requested quantity must be positive.</summary>
    public const string QuantityPositive = "fulfillment.qty.positive";

    /// <summary>Status transition is terminal.</summary>
    public const string StatusTerminal = "fulfillment.status.terminal";

    /// <summary>Processing status transition is invalid.</summary>
    public const string StatusProcessingInvalid = "fulfillment.status.processing_invalid";

    /// <summary>Selection payload is required.</summary>
    public const string SelectionRequired = "fulfillment.selection.required";

    /// <summary>Outbox event has no registered mapping.</summary>
    public const string OutboxUnmappedEvent = "fulfillment.outbox.unmapped_event";

    // ---- Processing -------------------------------------------------------------------
    /// <summary>Processing quantity exceeds the sellable amount.</summary>
    public const string ProcessingQuantityExceeds = "fulfillment.processing.qty_exceeds";

    /// <summary>Processing quantity must be positive.</summary>
    public const string ProcessingQuantityPositive = "fulfillment.processing.qty_positive";

    /// <summary>Processing release is invalid.</summary>
    public const string ProcessingReleaseInvalid = "fulfillment.processing.release_invalid";

    /// <summary>Processing release quantity must be positive.</summary>
    public const string ProcessingReleaseQuantityPositive = "fulfillment.processing.release_qty_positive";

    /// <summary>Cannot process after delivery.</summary>
    public const string ProcessAfterDelivered = "fulfillment.process.after_delivered";

    /// <summary>Cannot unconfirm a started fulfillment.</summary>
    public const string UnconfirmAlreadyStarted = "fulfillment.unconfirm.already_started";

    // ---- Packing ----------------------------------------------------------------------
    /// <summary>Packing quantity exceeds the processed amount.</summary>
    public const string PackQuantityExceeds = "fulfillment.pack.qty_exceeds";

    /// <summary>Packing quantity must be positive.</summary>
    public const string PackQuantityPositive = "fulfillment.pack.qty_positive";

    /// <summary>Release exceeds the packed amount.</summary>
    public const string PackReleaseExceeds = "fulfillment.pack.release_exceeds";

    /// <summary>Release quantity must be positive.</summary>
    public const string PackReleaseQuantityPositive = "fulfillment.pack.release_qty_positive";

    /// <summary>Cannot release an already allocated line.</summary>
    public const string PackReleaseAllocated = "fulfillment.pack.release_allocated";

    /// <summary>Packing requires the line to be processed first.</summary>
    public const string PackRequiresProcessing = "fulfillment.pack.requires_processing";

    /// <summary>Cannot pack after delivery.</summary>
    public const string PackAfterDelivered = "fulfillment.pack.after_delivered";

    /// <summary>Ship quantity exceeds the packed amount.</summary>
    public const string ShipQuantityExceeds = "fulfillment.ship.qty_exceeds";

    /// <summary>Ship quantity must be positive.</summary>
    public const string ShipQuantityPositive = "fulfillment.ship.qty_positive";

    /// <summary>Cannot cancel an already dispatched fulfillment.</summary>
    public const string CancelAlreadyDispatched = "fulfillment.cancel.already_dispatched";

    /// <summary>Cannot restore an already dispatched fulfillment.</summary>
    public const string RestoreAlreadyDispatched = "fulfillment.restore.already_dispatched";

    // ---- Shipment ---------------------------------------------------------------------
    /// <summary>Shipment was not found.</summary>
    public const string ShipmentNotFound = "fulfillment.shipment.not_found";

    /// <summary>Shipment quantity exceeds the ordered amount.</summary>
    public const string ShipmentQuantityExceedsOrdered = "fulfillment.shipment.qty_exceeds_ordered";

    /// <summary>Shipment quantity exceeds the packed amount.</summary>
    public const string ShipmentQuantityExceedsPacked = "fulfillment.shipment.qty_exceeds_packed";

    /// <summary>Shipment cancellation is invalid in this state.</summary>
    public const string ShipmentCancelInvalid = "fulfillment.shipment.cancel_invalid";

    /// <summary>Carrier display name is required.</summary>
    public const string ShipmentCarrierRequired = "fulfillment.shipment.carrier_required";

    /// <summary>Cannot create a shipment for a terminal fulfillment.</summary>
    public const string ShipmentCreateTerminal = "fulfillment.shipment.create_terminal";

    /// <summary>Shipment is locked by a consolidated package.</summary>
    public const string ShipmentLockedByConsolidatedPackage = "fulfillment.shipment.locked_by_consolidated_package";

    // ---- Dispatch / deliver / tracking ------------------------------------------------
    /// <summary>Dispatch is invalid in this state.</summary>
    public const string DispatchInvalidStatus = "fulfillment.dispatch.invalid_status";

    /// <summary>Dispatch requires a tracking reference.</summary>
    public const string DispatchTrackingRequired = "fulfillment.dispatch.tracking_required";

    /// <summary>Delivery is invalid in this state.</summary>
    public const string DeliverInvalidStatus = "fulfillment.deliver.invalid_status";

    /// <summary>Tracking reference is required.</summary>
    public const string TrackingRequired = "fulfillment.tracking.required";

    /// <summary>Tracking reference was already set.</summary>
    public const string TrackingAlreadySet = "fulfillment.tracking.already_set";

    /// <summary>Duplicate tracking reference.</summary>
    public const string TrackingDuplicate = "fulfillment.tracking.duplicate";

    /// <summary>Tracking state does not allow this change.</summary>
    public const string TrackingInvalidState = "fulfillment.tracking.invalid_state";

    /// <summary>Tracking is locked after dispatch.</summary>
    public const string TrackingLockedAfterDispatch = "fulfillment.tracking.locked_after_dispatch";

    /// <summary>There is nothing to correct on the tracking reference.</summary>
    public const string TrackingNothingToCorrect = "fulfillment.tracking.nothing_to_correct";

    // ---- Consolidated package ---------------------------------------------------------
    /// <summary>Consolidated package was not found.</summary>
    public const string PackageNotFound = "fulfillment.package.not_found";

    /// <summary>Consolidation requires multiple sellers.</summary>
    public const string PackageRequiresMultiSeller = "fulfillment.package.requires_multi_seller";

    /// <summary>Consolidation cannot span checkouts.</summary>
    public const string PackageMixedCheckout = "fulfillment.package.mixed_checkout";

    /// <summary>Checkout is required for consolidation.</summary>
    public const string PackageCheckoutRequired = "fulfillment.package.checkout_required";

    /// <summary>Shipping method is required for consolidation.</summary>
    public const string PackageShippingMethodRequired = "fulfillment.package.shipping_method_required";

    /// <summary>Consolidated members must share one shipping method.</summary>
    public const string PackageShippingMethodMismatch = "fulfillment.package.shipping_method_mismatch";

    /// <summary>Shipment is already a package member.</summary>
    public const string PackageShipmentAlreadyMember = "fulfillment.package.shipment_already_member";

    /// <summary>Shipment is not eligible for consolidation.</summary>
    public const string PackageShipmentNotEligible = "fulfillment.package.shipment_not_eligible";

    /// <summary>Duplicate shipment in the package.</summary>
    public const string PackageDuplicateShipment = "fulfillment.package.duplicate_shipment";

    /// <summary>Package member state changed underneath the operation.</summary>
    public const string PackageMemberStateChanged = "fulfillment.package.member_state_changed";

    /// <summary>Package dispatch is invalid in this state.</summary>
    public const string PackageDispatchInvalidState = "fulfillment.package.dispatch_invalid_state";

    /// <summary>Package cannot be delivered before dispatch.</summary>
    public const string PackageDeliverBeforeDispatch = "fulfillment.package.deliver_before_dispatch";

    /// <summary>Package cannot be cancelled after dispatch.</summary>
    public const string PackageCancelAfterDispatch = "fulfillment.package.cancel_after_dispatch";

    /// <summary>Package tracking is locked.</summary>
    public const string PackageTrackingLocked = "fulfillment.package.tracking_locked";

    /// <summary>Package tracking is required.</summary>
    public const string PackageTrackingRequired = "fulfillment.package.tracking_required";

    // ---- Shipping method -----------------------------------------------------------------
    /// <summary>Shipping method is unsupported.</summary>
    public const string ShippingMethodUnsupported = "fulfillment.shipping_method.unsupported";

    /// <summary>Post provider requires a recipient name.</summary>
    public const string ShippingPostRecipientRequired = "fulfillment.shipping.post.recipient_required";

    /// <summary>Post provider requires a full address.</summary>
    public const string ShippingPostAddressRequired = "fulfillment.shipping.post.address_required";

    /// <summary>Postal code is invalid.</summary>
    public const string ShippingPostalInvalid = "fulfillment.shipping.postal_invalid";

    /// <summary>Tipax provider requires a full address.</summary>
    public const string ShippingTipaxAddressRequired = "fulfillment.shipping.tipax.address_required";

    /// <summary>Courier provider requires a full address.</summary>
    public const string ShippingCourierAddressRequired = "fulfillment.shipping.courier.address_required";

    /// <summary>Pickup provider requires a location.</summary>
    public const string ShippingPickupLocationRequired = "fulfillment.shipping.pickup.location_required";

    /// <summary>Mobile number is invalid.</summary>
    public const string ShippingMobileInvalid = "fulfillment.shipping.mobile_invalid";

    // ---- Seller / customer authorization -------------------------------------------------
    /// <summary>Seller lacks order.handle permission.</summary>
    public const string SellerOrderHandleDenied = "seller.order.handle.denied";

    /// <summary>Category-scoped order.handle does not cover all lines.</summary>
    public const string SellerOrderHandleScopeDenied = "seller.order.handle.scope_denied";

    /// <summary>Seller order was not found for authorization.</summary>
    public const string SellerOrderMissing = "seller.order.missing";

    /// <summary>Customer actor missing.</summary>
    public const string CustomerActorMissing = "customer.actor.missing";

    /// <summary>Customer order missing / not owned.</summary>
    public const string CustomerOrderMissing = "customer.order.missing";

    // ---- Admin work queue ------------------------------------------------------------------
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

    // ---- Shipping service catalog -----------------------------------------------------------
    /// <summary>Shipping service was not found.</summary>
    public const string ShippingServiceNotFound = "shipping_service.not_found";

    /// <summary>Duplicate shipping service code.</summary>
    public const string ShippingServiceCodeDuplicate = "shipping_service.code_duplicate";

    /// <summary>Shipping service code required.</summary>
    public const string ShippingServiceCodeRequired = "shipping_service.code.required";

    /// <summary>Shipping service name required.</summary>
    public const string ShippingServiceNameRequired = "shipping_service.name.required";

    /// <summary>Unknown language id on shipping write.</summary>
    public const string ShippingServiceLanguageInvalid = "shipping_service.language_invalid";

    /// <summary>Shipping service option code required.</summary>
    public const string ShippingServiceOptionCodeRequired = "shipping_service_option.code.required";

    /// <summary>Shipping service option name required.</summary>
    public const string ShippingServiceOptionNameRequired = "shipping_service_option.name.required";
}
