using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Shipping.Contracts.Authoring;

namespace Tooba.Order.Application.Storefront.Shipping;

/// <summary>
/// Order-owned read-only eligibility for the legacy storefront path. The existing group planner
/// remains responsible for membership. A code can be displayed for one line without authorizing it
/// for another line or group; selection and final submission both check every actual group member.
/// </summary>
public sealed class StorefrontOfferShippingRestrictions(
    IOfferFulfillmentAssignmentLookup assignments,
    IOfferShippingMethodLookup methods,
    IShippingTypeReader types,
    ICurrentEdition edition,
    ICurrentTenant tenant)
{
    public async Task<StorefrontOfferShippingEligibility> ResolveAsync(
        IEnumerable<(Guid LineId, Guid OfferId, Guid SellerPartyId)> source,
        CancellationToken cancellationToken)
    {
        var lines = source.ToArray();
        var offerIds = lines.Select(line => line.OfferId).Distinct().ToArray();
        var snapshots = await assignments.GetAssignmentsAsync(offerIds, cancellationToken);
        var typeIds = snapshots.Values
            .Where(row => row.ShippingTypeId is not null)
            .Select(row => row.ShippingTypeId!.Value).Distinct().ToArray();
        var activeTypes = typeIds.Length == 0
            ? new HashSet<Guid>()
            : (await types.GetByIdsAsync(typeIds, cancellationToken))
                .Where(row => row.IsActive).Select(row => row.ShippingTypeId).ToHashSet();

        var cache = new Dictionary<OfferShippingMethodQuery, IReadOnlyList<OfferShippingMethodItem>>();
        var result = new Dictionary<Guid, HashSet<string>>();
        foreach (var line in lines)
        {
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            result.Add(line.LineId, allowed);
            if (!snapshots.TryGetValue(line.OfferId, out var assignment)
                || assignment.OfferId != line.OfferId
                || assignment.SellerPartyId != line.SellerPartyId
                || assignment.ShippingTypeId is not Guid typeId
                || !activeTypes.Contains(typeId)
                || !Enum.IsDefined(assignment.ShippingMode))
            {
                continue;
            }

            var owner = StorefrontCapturedOwnerPolicy.Resolve(
                edition.Current?.Edition, tenant.Current, assignment.Responsibility, line.SellerPartyId).Owner;
            if (owner is null)
            {
                continue;
            }

            var query = new OfferShippingMethodQuery(owner.Kind, owner.TenantId, owner.SellerPartyId, typeId);
            if (!cache.TryGetValue(query, out var candidates))
            {
                candidates = await methods.ListAsync(query, cancellationToken);
                cache.Add(query, candidates);
            }

            var specific = assignment.ShippingMode == OfferShippingSelectionMode.Specific;
            var ids = (assignment.ShippingMethodIds ?? []).ToHashSet();
            foreach (var candidate in candidates)
            {
                if (candidate.MethodId == Guid.Empty || string.IsNullOrWhiteSpace(candidate.ServiceCode)
                    || (specific && !ids.Contains(candidate.MethodId)))
                {
                    continue;
                }

                allowed.Add(candidate.OptionCode is null
                    ? candidate.ServiceCode
                    : $"{candidate.ServiceCode}:{candidate.OptionCode}");
            }
        }

        return new StorefrontOfferShippingEligibility(result);
    }
}

/// <summary>Per-line eligibility, never an authorization inferred from a cart-wide union.</summary>
public sealed class StorefrontOfferShippingEligibility(
    IReadOnlyDictionary<Guid, HashSet<string>> methodsByLine)
{
    public bool AllowsLine(Guid lineId, string? code) =>
        !string.IsNullOrWhiteSpace(code)
        && methodsByLine.TryGetValue(lineId, out var methods)
        && methods.Contains(code.Trim());

    public bool AllowsGroup(IEnumerable<Guid> lineIds, string? code)
    {
        var ids = lineIds.ToArray();
        return ids.Length > 0 && ids.All(id => AllowsLine(id, code));
    }

    public bool AllowsAny(string code) =>
        methodsByLine.Keys.Any(id => AllowsLine(id, code));
}
