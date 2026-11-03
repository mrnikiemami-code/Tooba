using Tooba.Shipping.Application.Carriers;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Domain.Carriers;

namespace Tooba.Shipping.Infrastructure.Carriers;

/// <summary>Single authority for carrier operation mutations and capability flags.</summary>
internal static class ShippingCarrierOperationPermissions
{
    internal sealed record Permissions(bool CanCreate, bool CanTrack, bool CanCancel);

    internal static bool AccountReadyForProvider(ShippingCarrierAccountExecutionContext? context)
        => context is { IsActive: true } && !string.IsNullOrWhiteSpace(context.Username);

    /// <summary>
    /// Create is account/capability gated; prior Succeeded packages do not permanently close the group.
    /// Pending/Unknown on any open package still refuse a fresh create (caller adds those reasons).
    /// Track/Cancel reflect the latest create operation.
    /// </summary>
    internal static Permissions ForCreateOperation(
        CarrierShipmentCapabilities caps,
        ShippingCarrierAccountExecutionContext? context,
        ShippingCarrierShipmentOperation? createOp)
    {
        if (!AccountReadyForProvider(context))
        {
            return new Permissions(false, false, false);
        }

        if (createOp is null)
        {
            return new Permissions(caps.CanCreate, false, false);
        }

        var (canTrack, canCancel) = createOp.State switch
        {
            ShippingCarrierShipmentState.Succeeded => (
                caps.CanTrack,
                caps.CanCancel && !string.IsNullOrWhiteSpace(createOp.ProviderTrackingCode)),
            ShippingCarrierShipmentState.Unknown => (caps.CanTrack, false),
            ShippingCarrierShipmentState.Failed => (caps.CanTrack, false),
            ShippingCarrierShipmentState.Pending => (false, false),
            _ => (false, false),
        };

        return new Permissions(caps.CanCreate, canTrack, canCancel);
    }

    internal static bool CanTrackMutation(
        CarrierShipmentCapabilities caps,
        ShippingCarrierAccountExecutionContext? context,
        ShippingCarrierShipmentOperation row)
        => AccountReadyForProvider(context)
           && caps.CanTrack
           && row.State is ShippingCarrierShipmentState.Succeeded or ShippingCarrierShipmentState.Unknown;

    internal static bool CanCancelMutation(
        CarrierShipmentCapabilities caps,
        ShippingCarrierAccountExecutionContext? context,
        ShippingCarrierShipmentOperation row)
        => AccountReadyForProvider(context)
           && caps.CanCancel
           && row.State == ShippingCarrierShipmentState.Succeeded
           && !string.IsNullOrWhiteSpace(row.ProviderTrackingCode);
}
