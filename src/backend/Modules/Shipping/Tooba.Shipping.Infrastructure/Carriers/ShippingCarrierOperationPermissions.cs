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

        return createOp.State switch
        {
            ShippingCarrierShipmentState.Pending => new Permissions(false, false, false),
            ShippingCarrierShipmentState.Succeeded => new Permissions(
                false,
                caps.CanTrack,
                caps.CanCancel && !string.IsNullOrWhiteSpace(createOp.ProviderTrackingCode)),
            ShippingCarrierShipmentState.Unknown => new Permissions(false, caps.CanTrack, false),
            ShippingCarrierShipmentState.Failed => new Permissions(false, caps.CanTrack, false),
            _ => new Permissions(false, false, false),
        };
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
