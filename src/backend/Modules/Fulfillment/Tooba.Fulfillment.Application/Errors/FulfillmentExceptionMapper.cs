using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Fulfillment.Application.Errors;

/// <summary>
/// Maps known Fulfillment/Shipping domain/directory stable machine codes to SemanticError.
/// Exact message match only — no Contains, no StartsWith prose heuristics; unknowns rethrow.
/// </summary>
public static class FulfillmentExceptionMapper
{
    /// <summary>Converts a known Fulfillment InvalidOperationException into a SemanticError. Unknowns rethrow.</summary>
    public static SemanticError ToSemanticError(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (TryMapExact(exception.Message, out var error))
        {
            return error;
        }

        throw exception;
    }

    /// <summary>Runs a Fulfillment directory action and maps only known expected failures to Result.</summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex) when (TryMapExact(ex.Code, out var error))
        {
            return Result.Failure<T>(error);
        }
        catch (InvalidOperationException ex) when (TryMapExact(ex.Message, out var error))
        {
            return Result.Failure<T>(error);
        }
    }

    /// <summary>Runs a void Fulfillment action and maps only known expected failures to Result.</summary>
    public static async Task<Result> TryAsync(Func<Task> action)
    {
        try
        {
            await action();
            return Result.Success();
        }
        catch (ContractOperationException ex) when (TryMapExact(ex.Code, out var error))
        {
            return Result.Failure(error);
        }
        catch (InvalidOperationException ex) when (TryMapExact(ex.Message, out var error))
        {
            return Result.Failure(error);
        }
    }

    /// <summary>Exact stable-code mapping only.</summary>
    public static bool TryMapExact(string? message, out SemanticError error)
    {
        switch (message)
        {
            case "fulfillment.missing":
            case "fulfillment.rejected":
            case "seller.order.handle.denied":
            case "seller.order.missing":
            case "seller.order.handle.scope_denied":
            case "customer.actor.missing":
            case "customer.order.missing":
            case "fulfillment.work_queue.bulk_failed":
            case "fulfillment.work_queue.bulk_unsupported":
            case "fulfillment.work_queue.bulk_empty":
            case "fulfillment.work_queue.cross_seller":
            case "fulfillment.work_queue.row_mismatch":
            case "fulfillment.work_queue.incompatible":
            case "fulfillment.work_queue.shipment_missing":
            case "shipping_service.not_found":
            case "shipping_service.code_duplicate":
            case "shipping_service.code.required":
            case "shipping_service.name.required":
            case "shipping_service.language_invalid":
            case "shipping_service_option.code.required":
            case "shipping_service_option.name.required":
            case "fulfillment.cancel.already_dispatched":
            case "fulfillment.deliver.invalid_status":
            case "fulfillment.dispatch.invalid_status":
            case "fulfillment.dispatch.tracking_required":
            case "fulfillment.not_found":
            case "fulfillment.order.not_found":
            case "fulfillment.order.not_paid":
            case "fulfillment.order_line.not_found":
            case "fulfillment.outbox.unmapped_event":
            case "fulfillment.pack.after_delivered":
            case "fulfillment.pack.qty_exceeds":
            case "fulfillment.pack.qty_positive":
            case "fulfillment.pack.release_allocated":
            case "fulfillment.pack.release_exceeds":
            case "fulfillment.pack.release_qty_positive":
            case "fulfillment.pack.requires_processing":
            case "fulfillment.package.cancel_after_dispatch":
            case "fulfillment.package.checkout_required":
            case "fulfillment.package.deliver_before_dispatch":
            case "fulfillment.package.dispatch_invalid_state":
            case "fulfillment.package.duplicate_shipment":
            case "fulfillment.package.member_state_changed":
            case "fulfillment.package.mixed_checkout":
            case "fulfillment.package.not_found":
            case "fulfillment.package.requires_multi_seller":
            case "fulfillment.package.shipment_already_member":
            case "fulfillment.package.shipment_not_eligible":
            case "fulfillment.package.shipping_method_mismatch":
            case "fulfillment.package.shipping_method_required":
            case "fulfillment.package.tracking_locked":
            case "fulfillment.package.tracking_required":
            case "fulfillment.process.after_delivered":
            case "fulfillment.processing.qty_exceeds":
            case "fulfillment.processing.qty_positive":
            case "fulfillment.processing.release_invalid":
            case "fulfillment.processing.release_qty_positive":
            case "fulfillment.qty.positive":
            case "fulfillment.restore.already_dispatched":
            case "fulfillment.selection.required":
            case "fulfillment.ship.qty_exceeds":
            case "fulfillment.ship.qty_positive":
            case "fulfillment.shipment.cancel_invalid":
            case "fulfillment.shipment.carrier_required":
            case "fulfillment.shipment.create_terminal":
            case "fulfillment.shipment.locked_by_consolidated_package":
            case "fulfillment.shipment.not_found":
            case "fulfillment.shipment.qty_exceeds_ordered":
            case "fulfillment.shipment.qty_exceeds_packed":
            case "fulfillment.shipping.courier.address_required":
            case "fulfillment.shipping.mobile_invalid":
            case "fulfillment.shipping.pickup.location_required":
            case "fulfillment.shipping.post.address_required":
            case "fulfillment.shipping.post.recipient_required":
            case "fulfillment.shipping.postal_invalid":
            case "fulfillment.shipping.tipax.address_required":
            case "fulfillment.shipping_method.unsupported":
            case "fulfillment.status.processing_invalid":
            case "fulfillment.status.terminal":
            case "fulfillment.tracking.already_set":
            case "fulfillment.tracking.duplicate":
            case "fulfillment.tracking.invalid_state":
            case "fulfillment.tracking.locked_after_dispatch":
            case "fulfillment.tracking.nothing_to_correct":
            case "fulfillment.tracking.required":
            case "fulfillment.unconfirm.already_started":
                error = new SemanticError(message);
                return true;

            default:
                error = default!;
                return false;
        }
    }
}
