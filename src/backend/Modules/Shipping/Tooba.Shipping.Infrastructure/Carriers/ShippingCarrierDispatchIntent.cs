using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tooba.BuildingBlocks;
using Tooba.Order.Contracts.Shipping;
using Tooba.Shipping.Contracts.Carriers;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Infrastructure.Carriers;

/// <summary>
/// TB-249-R6: durable parcel OCCURRENCE identity distinct from payload fingerprint.
/// Operation key = owner-scoped checkout + group + occurrenceId (server-issued UuidV7).
/// Payload fingerprint binds lines + physical dims to that occurrence; conflicting reuse refuses.
/// </summary>
internal static class ShippingCarrierDispatchIntent
{
    /// <summary>Server-issued parcel occurrence / package-dispatch identity.</summary>
    public static Guid MintOccurrenceId() => UuidV7.New();

    /// <summary>
    /// Builds <c>c:{checkout:N}:{group:N}:{occurrence:N}</c> (≤128) and a payload fingerprint
    /// from owner + checkout + group + canonical sorted lines + physical package dims.
    /// </summary>
    public static (string OperationKey, string PayloadFingerprint) Derive(
        Guid occurrenceId,
        ShippingDraftOwner owner,
        Guid checkoutId,
        Guid shippingGroupId,
        IReadOnlyList<ShippingCarrierShipmentLineQuantity> lines,
        ShippingCarrierShipmentPackageInput package)
    {
        if (occurrenceId == Guid.Empty)
        {
            throw new ArgumentException("Occurrence id is required.", nameof(occurrenceId));
        }

        var operationKey = BuildOperationKey(checkoutId, shippingGroupId, occurrenceId);
        var fingerprint = ComputePayloadFingerprint(owner, checkoutId, shippingGroupId, lines, package);
        return (operationKey, fingerprint);
    }

    public static string BuildOperationKey(Guid checkoutId, Guid shippingGroupId, Guid occurrenceId)
        => $"c:{checkoutId:N}:{shippingGroupId:N}:{occurrenceId:N}";

    public static string ComputePayloadFingerprint(
        ShippingDraftOwner owner,
        Guid checkoutId,
        Guid shippingGroupId,
        IReadOnlyList<ShippingCarrierShipmentLineQuantity> lines,
        ShippingCarrierShipmentPackageInput package)
    {
        var material = BuildPayloadMaterial(owner, checkoutId, shippingGroupId, lines, package);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
    }

    public static bool PayloadMatches(
        string expectedFingerprint,
        ShippingDraftOwner owner,
        Guid checkoutId,
        Guid shippingGroupId,
        IReadOnlyList<ShippingCarrierShipmentLineQuantity> lines,
        ShippingCarrierShipmentPackageInput package)
    {
        if (string.IsNullOrWhiteSpace(expectedFingerprint))
        {
            return false;
        }

        var hash = ComputePayloadFingerprint(owner, checkoutId, shippingGroupId, lines, package);
        return string.Equals(expectedFingerprint, hash, StringComparison.Ordinal);
    }

    private static string BuildPayloadMaterial(
        ShippingDraftOwner owner,
        Guid checkoutId,
        Guid shippingGroupId,
        IReadOnlyList<ShippingCarrierShipmentLineQuantity> lines,
        ShippingCarrierShipmentPackageInput package)
    {
        var sb = new StringBuilder(256);
        sb.Append((int)owner.Kind).Append('|')
            .Append(owner.TenantId ?? string.Empty).Append('|')
            .Append(owner.SellerPartyId?.ToString("N") ?? string.Empty).Append('|')
            .Append(checkoutId.ToString("N")).Append('|')
            .Append(shippingGroupId.ToString("N")).Append('|');
        foreach (var line in lines.OrderBy(x => x.OrderLineId))
        {
            sb.Append(line.OrderLineId.ToString("N")).Append(':')
                .Append(line.Quantity.ToString("0.######", CultureInfo.InvariantCulture)).Append(';');
        }

        sb.Append('|')
            .Append(package.WeightGrams.ToString("0.####", CultureInfo.InvariantCulture)).Append('|')
            .Append(package.LengthCentimeters.ToString("0.####", CultureInfo.InvariantCulture)).Append('|')
            .Append(package.WidthCentimeters.ToString("0.####", CultureInfo.InvariantCulture)).Append('|')
            .Append(package.HeightCentimeters.ToString("0.####", CultureInfo.InvariantCulture));
        return sb.ToString();
    }
}

/// <summary>
/// Declared value from exact selected Order line unit prices × quantities.
/// Collect from accepted collectible obligation with currency precision + largest-remainder residuals.
/// </summary>
internal static class ShippingCarrierPackageMoneyAllocator
{
    public static ShippingCarrierShipmentPackageInput Allocate(
        ShippingCarrierShipmentPackageInput physical,
        IReadOnlyList<ShippingCarrierShipmentLineQuantity> selected,
        OrderCommittedCarrierShipmentFacts facts,
        decimal priorDeclaredAllocated,
        decimal priorCollectAllocated,
        bool isFinalRemainingDispatch)
    {
        var scale = CurrencyScale(facts.Currency);
        var selectedDeclared = SumSelectedDeclared(selected, facts.ExecutableLines);
        var groupDeclared = SumGroupDeclared(facts);
        if (groupDeclared <= 0m)
        {
            // Fall closed: without line values do not invent collect/declared by quantity share.
            return physical with { DeclaredValue = 0m, CollectAmount = 0m };
        }

        var remainingDeclaredCap = Math.Max(0m, groupDeclared - priorDeclaredAllocated);
        var declared = RoundMoney(Math.Min(selectedDeclared, remainingDeclaredCap), scale);

        var remainingCollectCap = Math.Max(0m, facts.CarrierCollectAmount - priorCollectAllocated);
        decimal collect;
        if (remainingCollectCap <= 0m || facts.CarrierCollectAmount <= 0m)
        {
            collect = 0m;
        }
        else if (isFinalRemainingDispatch)
        {
            collect = RoundMoney(remainingCollectCap, scale);
        }
        else
        {
            var raw = facts.CarrierCollectAmount * (selectedDeclared / groupDeclared);
            collect = RoundMoney(Math.Min(raw, remainingCollectCap), scale);
        }

        return new ShippingCarrierShipmentPackageInput(
            physical.WeightGrams,
            declared,
            physical.LengthCentimeters,
            physical.WidthCentimeters,
            physical.HeightCentimeters,
            collect);
    }

    public static decimal SumSelectedDeclared(
        IReadOnlyList<ShippingCarrierShipmentLineQuantity> selected,
        IReadOnlyList<OrderCommittedCarrierExecutableLine> executable)
    {
        var prices = executable.ToDictionary(x => x.OrderLineId, x => x.UnitPriceSnapshot);
        decimal total = 0m;
        foreach (var line in selected)
        {
            if (!prices.TryGetValue(line.OrderLineId, out var unit))
            {
                continue;
            }

            total += unit * line.Quantity;
        }

        return total;
    }

    public static decimal SumGroupDeclared(OrderCommittedCarrierShipmentFacts facts)
    {
        if (facts.GroupPackedLineValues is { Count: > 0 } values)
        {
            return values.Sum(x => x.UnitPriceSnapshot * x.PackedQuantity);
        }

        return facts.ExecutableLines.Sum(x => x.UnitPriceSnapshot * (x.PackedQuantity > 0m ? x.PackedQuantity : x.RemainingPackedQuantity));
    }

    public static int CurrencyScale(string? currency)
    {
        var code = (currency ?? string.Empty).Trim().ToUpperInvariant();
        return code is "IRR" or "JPY" or "KRW" ? 0 : 2;
    }

    public static decimal RoundMoney(decimal value, int scale)
        => decimal.Round(value, scale, MidpointRounding.ToZero);
}
