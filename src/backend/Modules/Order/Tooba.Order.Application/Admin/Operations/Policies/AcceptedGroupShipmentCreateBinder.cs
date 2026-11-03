using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;
using Tooba.Order.Contracts.Shipping;

namespace Tooba.Order.Application.Admin.Operations.Policies;

/// <summary>
/// TB-252: binds admin/seller fulfillment create-shipment to one accepted Order shipping group.
/// Derives method from committed facts; refuses override, foreign group, excess qty, and legacy absence.
/// </summary>
public static class AcceptedGroupShipmentCreateBinder
{
    public const string AcceptedGroupRequired = "order.shipment.accepted_group_required";
    public const string AcceptedGroupUnavailable = "order.shipment.accepted_group_unavailable";
    public const string AcceptedGroupForeign = "order.shipment.accepted_group_foreign";
    public const string AcceptedGroupMethodMismatch = "order.shipment.accepted_group_method_mismatch";
    public const string AcceptedGroupLineForeign = "order.shipment.accepted_group_line_foreign";
    public const string AcceptedGroupQuantityExcess = "order.shipment.accepted_group_quantity_excess";
    public const string AcceptedGroupNotExecutable = "order.shipment.accepted_group_not_executable";

    /// <summary>
    /// Resolves derived method + group-scoped lines for create, or throws <see cref="AdminOrderOperationsException"/>.
    /// </summary>
    public static BoundCreate Resolve(
        FulfillmentSnapshot snapshot,
        AdminOrderOperationRequest request,
        IReadOnlyList<OrderCommittedCarrierShipmentFacts> checkoutGroups)
    {
        var sellerGroups = checkoutGroups
            .Where(g => g.SellerOrderId == snapshot.SellerOrderId)
            .ToList();

        if (sellerGroups.Count == 0)
        {
            throw new AdminOrderOperationsException(AcceptedGroupUnavailable);
        }

        if (request.ShippingGroupId is null || request.ShippingGroupId == Guid.Empty)
        {
            throw new AdminOrderOperationsException(AcceptedGroupRequired);
        }

        var groupId = request.ShippingGroupId.Value;
        var facts = sellerGroups.FirstOrDefault(g => g.ShippingGroupId == groupId)
            ?? throw new AdminOrderOperationsException(AcceptedGroupForeign);

        if (facts.SellerPartyId != snapshot.SellerPartyId
            || facts.CheckoutId != snapshot.CheckoutId
            || (facts.FulfillmentId is Guid fid && fid != snapshot.FulfillmentId))
        {
            throw new AdminOrderOperationsException(AcceptedGroupForeign);
        }

        if (facts.IsCancelled || facts.PaymentLocked)
        {
            throw new AdminOrderOperationsException(AcceptedGroupNotExecutable);
        }

        if (facts.ExecutableLines.Count == 0)
        {
            throw new AdminOrderOperationsException(AcceptedGroupNotExecutable);
        }

        var derivedMethod = ResolveFulfillmentMethodCode(facts.MethodCode);
        var clientMethod = request.ShippingMethodCode?.Trim();
        if (!string.IsNullOrWhiteSpace(clientMethod))
        {
            var normalizedClient = ResolveFulfillmentMethodCode(clientMethod) ?? clientMethod;
            var normalizedDerived = derivedMethod ?? facts.MethodCode.Trim();
            if (!string.Equals(normalizedClient, normalizedDerived, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(clientMethod, facts.MethodCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new AdminOrderOperationsException(AcceptedGroupMethodMismatch);
            }
        }

        var executableByLine = facts.ExecutableLines.ToDictionary(x => x.OrderLineId);
        var membership = facts.OrderLineIds.ToHashSet();
        ShipmentLineCommand[] lines;
        if (request.Selections is { Count: > 0 })
        {
            var built = new List<ShipmentLineCommand>(request.Selections.Count);
            foreach (var selection in request.Selections)
            {
                if (!membership.Contains(selection.OrderLineId)
                    || !executableByLine.TryGetValue(selection.OrderLineId, out var exec))
                {
                    throw new AdminOrderOperationsException(AcceptedGroupLineForeign);
                }

                if (selection.Quantity <= 0 || selection.Quantity > exec.RemainingPackedQuantity)
                {
                    throw new AdminOrderOperationsException(AcceptedGroupQuantityExcess);
                }

                built.Add(new ShipmentLineCommand(selection.OrderLineId, selection.Quantity));
            }

            lines = built.ToArray();
        }
        else
        {
            lines = facts.ExecutableLines
                .Where(x => x.RemainingPackedQuantity > 0)
                .Select(x => new ShipmentLineCommand(x.OrderLineId, x.RemainingPackedQuantity))
                .ToArray();
        }

        if (lines.Length == 0)
        {
            throw new AdminOrderOperationsException(AcceptedGroupNotExecutable);
        }

        var carrier = string.IsNullOrWhiteSpace(facts.MethodLabel)
            ? (derivedMethod ?? facts.MethodCode)
            : facts.MethodLabel.Trim();

        return new BoundCreate(groupId, derivedMethod, carrier, lines);
    }

    /// <summary>Maps committed method onto Fulfillment registry code when present.</summary>
    public static string? ResolveFulfillmentMethodCode(string? methodCode)
    {
        if (string.IsNullOrWhiteSpace(methodCode))
        {
            return null;
        }

        var trimmed = methodCode.Trim().Split('\u001f', 2)[0].Trim();
        if (ShippingMethodRegistry.Find(trimmed) is { } exact)
        {
            return exact.Code;
        }

        var head = trimmed.Split(':', 2)[0].Trim();
        return ShippingMethodRegistry.Find(head)?.Code;
    }

    /// <summary>Derived create inputs after group binding.</summary>
    public sealed record BoundCreate(
        Guid ShippingGroupId,
        string? ShippingMethodCode,
        string CarrierDisplayName,
        IReadOnlyList<ShipmentLineCommand> Lines);
}
