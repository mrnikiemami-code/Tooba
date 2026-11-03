namespace Tooba.Order.Contracts.Shipping;

/// <summary>
/// TB-252-R1: shared Manual/Free parcel-create authority for accepted shipping groups.
/// CarrierApi must use the dedicated Shipping execution path — never this binder.
/// </summary>
public static class AcceptedGroupParcelCreateAuthority
{
    /// <summary>Shipping group id was omitted.</summary>
    public const string AcceptedGroupRequired = "order.shipment.accepted_group_required";
    /// <summary>Checkout has no accepted shipping groups for this seller.</summary>
    public const string AcceptedGroupUnavailable = "order.shipment.accepted_group_unavailable";
    /// <summary>Group does not belong to this seller/checkout/fulfillment.</summary>
    public const string AcceptedGroupForeign = "order.shipment.accepted_group_foreign";
    /// <summary>Client method/service/option does not exactly match accepted identity.</summary>
    public const string AcceptedGroupMethodMismatch = "order.shipment.accepted_group_method_mismatch";
    /// <summary>Selected line is outside the accepted group.</summary>
    public const string AcceptedGroupLineForeign = "order.shipment.accepted_group_line_foreign";
    /// <summary>Requested quantity exceeds remaining packed quantity.</summary>
    public const string AcceptedGroupQuantityExcess = "order.shipment.accepted_group_quantity_excess";
    /// <summary>Group cannot create a Manual parcel in the current state.</summary>
    public const string AcceptedGroupNotExecutable = "order.shipment.accepted_group_not_executable";
    /// <summary>CarrierApi groups must use the dedicated Shipping execution path.</summary>
    public const string CarrierApiUseDedicatedPath = "order.shipment.carrier_api_use_dedicated_path";
    /// <summary>Client ProviderMetadataJson is not accepted on Manual parcel create.</summary>
    public const string ProviderMetadataForbidden = "order.shipment.provider_metadata_forbidden";

    /// <summary>Normalized line selection after aggregate.</summary>
    public sealed record LineQuantity(Guid OrderLineId, decimal Quantity);

    /// <summary>Bound Manual/Free create inputs derived only from committed facts.</summary>
    public sealed record BoundCreate(
        Guid ShippingGroupId,
        string AcceptedMethodCode,
        string? AcceptedServiceCode,
        string? AcceptedOptionCode,
        string CarrierDisplayName,
        IReadOnlyList<LineQuantity> Lines);

    /// <summary>
    /// Validates group membership, Manual transport, exact method identity, and remaining quantities.
    /// Does not map to Fulfillment registry codes — that is a separate adapter step after bind.
    /// </summary>
    public static BoundCreate Bind(
        Guid checkoutId,
        Guid sellerOrderId,
        Guid sellerPartyId,
        Guid? fulfillmentId,
        Guid? shippingGroupId,
        string? clientMethodCode,
        string? clientServiceCode,
        string? clientOptionCode,
        string? providerMetadataJson,
        IReadOnlyList<LineQuantity>? selections,
        IReadOnlyList<OrderCommittedCarrierShipmentFacts> checkoutGroups)
    {
        var sellerGroups = checkoutGroups
            .Where(g => g.SellerOrderId == sellerOrderId)
            .ToList();

        if (sellerGroups.Count == 0)
        {
            throw new AcceptedGroupParcelCreateException(AcceptedGroupUnavailable);
        }

        if (shippingGroupId is null || shippingGroupId == Guid.Empty)
        {
            throw new AcceptedGroupParcelCreateException(AcceptedGroupRequired);
        }

        var groupId = shippingGroupId.Value;
        var facts = sellerGroups.FirstOrDefault(g => g.ShippingGroupId == groupId)
            ?? throw new AcceptedGroupParcelCreateException(AcceptedGroupForeign);

        if (facts.SellerPartyId != sellerPartyId
            || facts.CheckoutId != checkoutId
            || (facts.FulfillmentId is Guid fid && fulfillmentId is Guid reqFid && fid != reqFid))
        {
            throw new AcceptedGroupParcelCreateException(AcceptedGroupForeign);
        }

        if (string.Equals(facts.TransportMode, "CarrierApi", StringComparison.Ordinal))
        {
            throw new AcceptedGroupParcelCreateException(CarrierApiUseDedicatedPath);
        }

        if (!string.IsNullOrWhiteSpace(providerMetadataJson))
        {
            throw new AcceptedGroupParcelCreateException(ProviderMetadataForbidden);
        }

        if (facts.IsCancelled || facts.PaymentLocked)
        {
            throw new AcceptedGroupParcelCreateException(AcceptedGroupNotExecutable);
        }

        if (facts.ExecutableLines.Count == 0)
        {
            throw new AcceptedGroupParcelCreateException(AcceptedGroupNotExecutable);
        }

        if (!MatchesAcceptedMethodIdentity(
                facts.MethodCode,
                facts.ServiceCode,
                facts.OptionCode,
                clientMethodCode,
                clientServiceCode,
                clientOptionCode))
        {
            throw new AcceptedGroupParcelCreateException(AcceptedGroupMethodMismatch);
        }

        var executableByLine = facts.ExecutableLines.ToDictionary(x => x.OrderLineId);
        var membership = facts.OrderLineIds.ToHashSet();
        LineQuantity[] lines;
        if (selections is { Count: > 0 })
        {
            var aggregated = new Dictionary<Guid, decimal>();
            foreach (var selection in selections)
            {
                if (!membership.Contains(selection.OrderLineId)
                    || !executableByLine.ContainsKey(selection.OrderLineId))
                {
                    throw new AcceptedGroupParcelCreateException(AcceptedGroupLineForeign);
                }

                if (selection.Quantity <= 0)
                {
                    throw new AcceptedGroupParcelCreateException(AcceptedGroupQuantityExcess);
                }

                aggregated[selection.OrderLineId] = aggregated.TryGetValue(selection.OrderLineId, out var prior)
                    ? prior + selection.Quantity
                    : selection.Quantity;
            }

            var built = new List<LineQuantity>(aggregated.Count);
            foreach (var (orderLineId, quantity) in aggregated)
            {
                var exec = executableByLine[orderLineId];
                if (quantity > exec.RemainingPackedQuantity)
                {
                    throw new AcceptedGroupParcelCreateException(AcceptedGroupQuantityExcess);
                }

                built.Add(new LineQuantity(orderLineId, quantity));
            }

            lines = built.ToArray();
        }
        else
        {
            lines = facts.ExecutableLines
                .Where(x => x.RemainingPackedQuantity > 0)
                .Select(x => new LineQuantity(x.OrderLineId, x.RemainingPackedQuantity))
                .ToArray();
        }

        if (lines.Length == 0)
        {
            throw new AcceptedGroupParcelCreateException(AcceptedGroupNotExecutable);
        }

        var carrier = string.IsNullOrWhiteSpace(facts.MethodLabel)
            ? facts.MethodCode.Trim()
            : facts.MethodLabel.Trim();

        return new BoundCreate(
            groupId,
            facts.MethodCode.Trim(),
            NormalizeOptional(facts.ServiceCode),
            NormalizeOptional(facts.OptionCode),
            carrier,
            lines);
    }

    /// <summary>
    /// Exact accepted method/service/option identity. Prefix registry equality is NOT used here.
    /// Empty client method means derive from facts (no override).
    /// </summary>
    public static bool MatchesAcceptedMethodIdentity(
        string acceptedMethodCode,
        string? acceptedServiceCode,
        string? acceptedOptionCode,
        string? clientMethodCode,
        string? clientServiceCode,
        string? clientOptionCode)
    {
        if (string.IsNullOrWhiteSpace(clientMethodCode)
            && string.IsNullOrWhiteSpace(clientServiceCode)
            && string.IsNullOrWhiteSpace(clientOptionCode))
        {
            return true;
        }

        var acceptedMethod = NormalizeToken(acceptedMethodCode);
        var acceptedService = NormalizeToken(acceptedServiceCode);
        var acceptedOption = NormalizeToken(acceptedOptionCode);
        var clientMethod = NormalizeToken(clientMethodCode);
        var clientService = NormalizeToken(clientServiceCode);
        var clientOption = NormalizeToken(clientOptionCode);

        if (clientMethod.Contains(':', StringComparison.Ordinal)
            && clientService.Length == 0
            && clientOption.Length == 0)
        {
            var acceptedForms = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { acceptedMethod };
            if (acceptedOption.Length > 0)
            {
                acceptedForms.Add($"{acceptedMethod}:{acceptedOption}");
            }

            if (acceptedService.Length > 0)
            {
                acceptedForms.Add($"{acceptedMethod}:{acceptedService}");
            }

            if (acceptedService.Length > 0 && acceptedOption.Length > 0)
            {
                acceptedForms.Add($"{acceptedMethod}:{acceptedService}:{acceptedOption}");
            }

            return acceptedForms.Contains(clientMethod);
        }

        if (!string.Equals(clientMethod, acceptedMethod, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (clientService.Length > 0
            && !string.Equals(clientService, acceptedService, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (clientOption.Length > 0
            && !string.Equals(clientOption, acceptedOption, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static string NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().Split('\u001f', 2)[0].Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        var token = NormalizeToken(value);
        return token.Length == 0 ? null : token;
    }
}

/// <summary>Typed refusal for accepted-group parcel create (Contracts-layer; mapped to module exceptions at edges).</summary>
public sealed class AcceptedGroupParcelCreateException : Exception
{
    /// <summary>Creates a refusal carrying a stable order.shipment.* code.</summary>
    public AcceptedGroupParcelCreateException(string code)
        : base(code)
    {
        Code = code;
    }

    /// <summary>Stable refusal code.</summary>
    public string Code { get; }
}
