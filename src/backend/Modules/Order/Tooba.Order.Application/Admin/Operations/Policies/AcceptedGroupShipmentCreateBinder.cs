using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;
using Tooba.Order.Contracts.Shipping;

namespace Tooba.Order.Application.Admin.Operations.Policies;

/// <summary>
/// TB-252 / TB-252-R1: binds admin/seller fulfillment create-shipment to one accepted Order shipping group.
/// Manual/Free only; CarrierApi refuses before allocation. Exact method/option identity; registry map is separate.
/// </summary>
public static class AcceptedGroupShipmentCreateBinder
{
    public const string AcceptedGroupRequired = AcceptedGroupParcelCreateAuthority.AcceptedGroupRequired;
    public const string AcceptedGroupUnavailable = AcceptedGroupParcelCreateAuthority.AcceptedGroupUnavailable;
    public const string AcceptedGroupForeign = AcceptedGroupParcelCreateAuthority.AcceptedGroupForeign;
    public const string AcceptedGroupMethodMismatch = AcceptedGroupParcelCreateAuthority.AcceptedGroupMethodMismatch;
    public const string AcceptedGroupLineForeign = AcceptedGroupParcelCreateAuthority.AcceptedGroupLineForeign;
    public const string AcceptedGroupQuantityExcess = AcceptedGroupParcelCreateAuthority.AcceptedGroupQuantityExcess;
    public const string AcceptedGroupNotExecutable = AcceptedGroupParcelCreateAuthority.AcceptedGroupNotExecutable;
    public const string CarrierApiUseDedicatedPath = AcceptedGroupParcelCreateAuthority.CarrierApiUseDedicatedPath;
    public const string ProviderMetadataForbidden = AcceptedGroupParcelCreateAuthority.ProviderMetadataForbidden;

    /// <summary>
    /// Resolves derived method + group-scoped lines for Manual create, or throws <see cref="AdminOrderOperationsException"/>.
    /// </summary>
    public static BoundCreate Resolve(
        FulfillmentSnapshot snapshot,
        AdminOrderOperationRequest request,
        IReadOnlyList<OrderCommittedCarrierShipmentFacts> checkoutGroups)
    {
        try
        {
            var selections = request.Selections is { Count: > 0 }
                ? request.Selections
                    .Select(x => new AcceptedGroupParcelCreateAuthority.LineQuantity(x.OrderLineId, x.Quantity))
                    .ToArray()
                : null;

            var bound = AcceptedGroupParcelCreateAuthority.Bind(
                snapshot.CheckoutId,
                snapshot.SellerOrderId,
                snapshot.SellerPartyId,
                snapshot.FulfillmentId,
                request.ShippingGroupId,
                request.ShippingMethodCode,
                clientServiceCode: null,
                clientOptionCode: null,
                request.ProviderMetadataJson,
                selections,
                checkoutGroups);

            // Registry mapping is an adapter AFTER exact commitment is locked — never used for identity proof.
            var registryCode = MapAcceptedMethodToFulfillmentRegistry(
                bound.AcceptedMethodCode,
                bound.AcceptedServiceCode,
                bound.AcceptedOptionCode);

            var lines = bound.Lines
                .Select(x => new ShipmentLineCommand(x.OrderLineId, x.Quantity))
                .ToArray();

            return new BoundCreate(bound.ShippingGroupId, registryCode, bound.CarrierDisplayName, lines);
        }
        catch (AcceptedGroupParcelCreateException ex)
        {
            throw new AdminOrderOperationsException(ex.Code);
        }
    }

    /// <summary>
    /// Maps committed method onto Fulfillment registry code when present.
    /// Must not authorize a different catalogue option than the accepted identity.
    /// </summary>
    public static string? MapAcceptedMethodToFulfillmentRegistry(
        string acceptedMethodCode,
        string? acceptedServiceCode,
        string? acceptedOptionCode)
    {
        if (string.IsNullOrWhiteSpace(acceptedMethodCode))
        {
            return null;
        }

        var method = acceptedMethodCode.Trim().Split('\u001f', 2)[0].Trim();
        var service = string.IsNullOrWhiteSpace(acceptedServiceCode)
            ? null
            : acceptedServiceCode.Trim().Split('\u001f', 2)[0].Trim();
        var option = string.IsNullOrWhiteSpace(acceptedOptionCode)
            ? null
            : acceptedOptionCode.Trim().Split('\u001f', 2)[0].Trim();

        // Prefer exact composites that preserve option/service when registered; never strip to a foreign option.
        string[] candidates =
        [
            string.IsNullOrEmpty(service) || string.IsNullOrEmpty(option) ? string.Empty : $"{method}:{service}:{option}",
            string.IsNullOrEmpty(option) ? string.Empty : $"{method}:{option}",
            string.IsNullOrEmpty(service) ? string.Empty : $"{method}:{service}",
            method,
        ];

        foreach (var candidate in candidates)
        {
            if (candidate.Length == 0)
            {
                continue;
            }

            if (ShippingMethodRegistry.Find(candidate) is { } exact)
            {
                return exact.Code;
            }
        }

        return method;
    }

    /// <summary>Obsolete name — exact identity must not use prefix stripping. Prefer <see cref="MapAcceptedMethodToFulfillmentRegistry"/>.</summary>
    [Obsolete("Use MapAcceptedMethodToFulfillmentRegistry after exact commitment bind.")]
    public static string? ResolveFulfillmentMethodCode(string? methodCode)
    {
        if (string.IsNullOrWhiteSpace(methodCode))
        {
            return null;
        }

        return MapAcceptedMethodToFulfillmentRegistry(methodCode, null, null);
    }

    /// <summary>Derived create inputs after group binding.</summary>
    public sealed record BoundCreate(
        Guid ShippingGroupId,
        string? ShippingMethodCode,
        string CarrierDisplayName,
        IReadOnlyList<ShipmentLineCommand> Lines);
}
