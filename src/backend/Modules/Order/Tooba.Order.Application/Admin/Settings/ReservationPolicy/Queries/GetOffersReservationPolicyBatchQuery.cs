using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Models;
using Tooba.Order.Application.ReservationCycle.Contracts;

namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy.Queries;

/// <summary>Admin GET batch offer reservation-policy preview.</summary>
public sealed record GetOffersReservationPolicyBatchQuery(IReadOnlyList<Guid> OfferIds)
    : IRequest<Result<ReservationPolicyOfferBatchView>>;

/// <summary>Handler for <see cref="GetOffersReservationPolicyBatchQuery"/>.</summary>
public sealed class GetOffersReservationPolicyBatchHandler
    : IRequestHandler<GetOffersReservationPolicyBatchQuery, Result<ReservationPolicyOfferBatchView>>
{
    private readonly IReservationCyclePolicyResolver _resolver;
    private readonly IOfferQueryGateway _offers;
    private readonly ICatalogVariantLookup _variants;

    /// <summary>Creates the handler.</summary>
    public GetOffersReservationPolicyBatchHandler(
        IReservationCyclePolicyResolver resolver,
        IOfferQueryGateway offers,
        ICatalogVariantLookup variants)
    {
        _resolver = resolver;
        _offers = offers;
        _variants = variants;
    }

    /// <inheritdoc />
    public async Task<Result<ReservationPolicyOfferBatchView>> Handle(
        GetOffersReservationPolicyBatchQuery request,
        CancellationToken cancellationToken)
    {
        var ids = request.OfferIds;
        var categories = await ReservationPolicyOfferCategoryResolver.ResolveManyAsync(
            _offers, _variants, ids, cancellationToken);
        var lines = ids.Select(id => (id, categories.GetValueOrDefault(id))).ToList();
        var previews = await _resolver.PreviewManyAsync(lines, cancellationToken);
        var items = ids.Select((offerId, index) =>
                ReservationPolicyComposer.ForOffer(offerId, previews[index], true))
            .ToList();
        return Result.Success(new ReservationPolicyOfferBatchView(items));
    }
}
