using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Models;
using Tooba.Order.Application.ReservationCycle.Contracts;

namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy.Commands;

/// <summary>Admin PUT offer reservation-policy override.</summary>
public sealed record SaveOfferReservationPolicyCommand(
    Guid OfferId,
    int? InitialReservationHoldMinutes,
    int? RetryReservationHoldMinutes,
    int? MaxReservationCycles,
    Guid ActorUserId) : IRequest<Result<ReservationPolicyEditorView>>;

/// <summary>Handler for <see cref="SaveOfferReservationPolicyCommand"/>.</summary>
public sealed class SaveOfferReservationPolicyHandler
    : IRequestHandler<SaveOfferReservationPolicyCommand, Result<ReservationPolicyEditorView>>
{
    private readonly IStoreReservationPolicySettingsPort _settings;
    private readonly IReservationCyclePolicyResolver _resolver;
    private readonly IOfferQueryGateway _offers;
    private readonly ICatalogVariantLookup _variants;

    /// <summary>Creates the handler.</summary>
    public SaveOfferReservationPolicyHandler(
        IStoreReservationPolicySettingsPort settings,
        IReservationCyclePolicyResolver resolver,
        IOfferQueryGateway offers,
        ICatalogVariantLookup variants)
    {
        _settings = settings;
        _resolver = resolver;
        _offers = offers;
        _variants = variants;
    }

    /// <inheritdoc />
    public async Task<Result<ReservationPolicyEditorView>> Handle(
        SaveOfferReservationPolicyCommand request,
        CancellationToken cancellationToken)
    {
        await _settings.SaveOfferOverrideAsync(
            request.OfferId,
            new ReservationPolicyOverrideWrite(
                request.InitialReservationHoldMinutes,
                request.RetryReservationHoldMinutes,
                request.MaxReservationCycles),
            request.ActorUserId,
            cancellationToken);
        var categoryId = await ReservationPolicyOfferCategoryResolver.ResolveAsync(
            _offers, _variants, request.OfferId, cancellationToken);
        var preview = await _resolver.PreviewAsync(request.OfferId, categoryId, cancellationToken);
        return Result.Success(ReservationPolicyComposer.ForOffer(request.OfferId, preview, true));
    }
}
