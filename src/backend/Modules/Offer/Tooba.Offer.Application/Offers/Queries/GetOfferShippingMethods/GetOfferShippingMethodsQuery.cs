using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Application.Offers.Shipping;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Shipping.Contracts.Authoring;

namespace Tooba.Offer.Application.Offers.Queries.GetOfferShippingMethods;

/// <summary>Reads the current selection and the published active eligible methods of one existing offer.</summary>
/// <param name="OfferId">Existing offer identity.</param>
/// <param name="LanguageCode">Active UI language code for the stored display name.</param>
public sealed record GetOfferShippingMethodsQuery(Guid OfferId, string? LanguageCode = null)
    : IRequest<Result<OfferShippingMethodOptionsPage>>;

/// <summary>
/// Admin-only read orchestration; no write and no publication. The eligible list is bounded to the offer's
/// own trusted owner and real shipping type; an unresolved owner/type yields a true empty list, not an error.
/// </summary>
public sealed class GetOfferShippingMethodsHandler(
    IOfferStore store,
    IOfferShippingMethodStore shippingMethods,
    IShippingTypeReader shippingTypes,
    IOfferShippingMethodLookup methodCatalog,
    ICurrentEdition edition,
    ICurrentTenant tenant)
    : IRequestHandler<GetOfferShippingMethodsQuery, Result<OfferShippingMethodOptionsPage>>
{
    /// <inheritdoc />
    public async Task<Result<OfferShippingMethodOptionsPage>> Handle(
        GetOfferShippingMethodsQuery request,
        CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null)
        {
            return OfferReadModelComposer.NotFound<OfferShippingMethodOptionsPage>();
        }

        var responsibilities = offer.FulfillmentResponsibility is null
            ? (OfferFulfillmentResponsibility?)null
            : (OfferFulfillmentResponsibility)(int)offer.FulfillmentResponsibility.Value;
        var mode = offer.ShippingSelectionMode is null
            ? OfferShippingSelectionMode.Automatic
            : (OfferShippingSelectionMode)(int)offer.ShippingSelectionMode.Value;
        var selected = await shippingMethods.ListMethodIdsAsync(offer.OfferId, cancellationToken);

        var methods = await OfferShippingMethodOptionsReader.ListAsync(
            methodCatalog, shippingTypes, edition, tenant,
            responsibilities, offer.FulfillmentShippingTypeId, offer.SellerPartyId,
            request.LanguageCode, cancellationToken);

        return Result.Success(new OfferShippingMethodOptionsPage(
            responsibilities,
            offer.FulfillmentShippingTypeId,
            mode,
            selected,
            methods));
    }
}

/// <summary>
/// Reads the published active eligible methods for a would-be offer (admin create surface, before an Offer
/// exists). No write; the owner is still derived from the trusted edition/tenant context and the selected real
/// seller of the offer being composed.
/// </summary>
/// <param name="Responsibility">Selected responsibility or <c>null</c>.</param>
/// <param name="ShippingTypeId">Selected real shipping type id or <c>null</c>.</param>
/// <param name="SellerPartyId">Selected selling party (the offer's own seller), or <c>null</c>.</param>
/// <param name="LanguageCode">Active UI language code for the stored display name.</param>
public sealed record ListOfferShippingMethodOptionsQuery(
    OfferFulfillmentResponsibility? Responsibility,
    Guid? ShippingTypeId,
    Guid? SellerPartyId,
    string? LanguageCode = null) : IRequest<Result<OfferShippingMethodOptionsPage>>;

/// <summary>Admin-only create-surface read of eligible methods; no persistence.</summary>
public sealed class ListOfferShippingMethodOptionsHandler(
    IShippingTypeReader shippingTypes,
    IOfferShippingMethodLookup methodCatalog,
    ICurrentEdition edition,
    ICurrentTenant tenant)
    : IRequestHandler<ListOfferShippingMethodOptionsQuery, Result<OfferShippingMethodOptionsPage>>
{
    /// <inheritdoc />
    public async Task<Result<OfferShippingMethodOptionsPage>> Handle(
        ListOfferShippingMethodOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var methods = await OfferShippingMethodOptionsReader.ListAsync(
            methodCatalog, shippingTypes, edition, tenant,
            request.Responsibility, request.ShippingTypeId, request.SellerPartyId ?? Guid.Empty,
            request.LanguageCode, cancellationToken);

        return Result.Success(new OfferShippingMethodOptionsPage(
            request.Responsibility,
            request.ShippingTypeId,
            OfferShippingSelectionMode.Automatic,
            [],
            methods));
    }
}

/// <summary>
/// Shared read of eligible published methods; used by both the edit and create surfaces.
/// </summary>
internal static class OfferShippingMethodOptionsReader
{
    internal static async Task<IReadOnlyList<OfferShippingMethodOption>> ListAsync(
        IOfferShippingMethodLookup methodCatalog,
        IShippingTypeReader shippingTypes,
        ICurrentEdition edition,
        ICurrentTenant tenant,
        OfferFulfillmentResponsibility? responsibility,
        Guid? shippingTypeId,
        Guid sellerPartyId,
        string? languageCode,
        CancellationToken cancellationToken)
    {
        if (responsibility is null || shippingTypeId is not Guid typeId)
        {
            return [];
        }

        var types = await shippingTypes.GetByIdsAsync([typeId], cancellationToken);
        var type = types.SingleOrDefault(candidate => candidate.ShippingTypeId == typeId);
        if (type is null || !type.IsActive)
        {
            return [];
        }

        var owner = OfferShippingOwnerResolver.Resolve(edition, tenant, responsibility, sellerPartyId);
        if (owner is null)
        {
            return [];
        }

        var rows = await methodCatalog.ListAsync(
            new OfferShippingMethodQuery(
                owner.Value.Kind, owner.Value.TenantId, owner.Value.SellerPartyId, typeId, languageCode),
            cancellationToken);

        return rows
            .GroupBy(row => row.MethodId)
            .Select(group => group.First())
            .OrderBy(row => row.ServiceCode, StringComparer.Ordinal)
            .ThenBy(row => row.OptionCode, StringComparer.Ordinal)
            .Select(row => new OfferShippingMethodOption(
                row.MethodId,
                row.ServiceCode,
                row.OptionCode,
                row.Label))
            .ToArray();
    }
}
