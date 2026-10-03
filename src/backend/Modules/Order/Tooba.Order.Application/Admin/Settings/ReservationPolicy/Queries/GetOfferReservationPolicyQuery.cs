using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Models;
using Tooba.Order.Application.ReservationCycle.Contracts;

namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy.Queries;

/// <summary>Admin GET offer reservation-policy preview.</summary>
public sealed record GetOfferReservationPolicyQuery(Guid OfferId)
    : IRequest<Result<ReservationPolicyEditorView>>;

/// <summary>Handler for <see cref="GetOfferReservationPolicyQuery"/>.</summary>
public sealed class GetOfferReservationPolicyHandler
    : IRequestHandler<GetOfferReservationPolicyQuery, Result<ReservationPolicyEditorView>>
{
    private readonly IReservationCyclePolicyResolver _resolver;
    private readonly IOfferQueryGateway _offers;
    private readonly ICatalogVariantLookup _variants;

    /// <summary>Creates the handler.</summary>
    public GetOfferReservationPolicyHandler(
        IReservationCyclePolicyResolver resolver,
        IOfferQueryGateway offers,
        ICatalogVariantLookup variants)
    {
        _resolver = resolver;
        _offers = offers;
        _variants = variants;
    }

    /// <inheritdoc />
    public async Task<Result<ReservationPolicyEditorView>> Handle(
        GetOfferReservationPolicyQuery request,
        CancellationToken cancellationToken)
    {
        var categoryId = await ReservationPolicyOfferCategoryResolver.ResolveAsync(
            _offers, _variants, request.OfferId, cancellationToken);
        var preview = await _resolver.PreviewAsync(request.OfferId, categoryId, cancellationToken);
        return Result.Success(ReservationPolicyComposer.ForOffer(request.OfferId, preview, true));
    }
}
