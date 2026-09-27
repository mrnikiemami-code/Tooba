using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Cart.Contracts.Lifetime;
using Tooba.Catalog.Application.Settings.HoldPolicy.Models;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Order.Contracts.Reservation;
using Tooba.Payment.Contracts.Hold;

namespace Tooba.Catalog.Application.Settings.HoldPolicy.Queries;

/// <summary>Handler for <see cref="GetHoldPolicySettingsQuery"/>.</summary>
public sealed class GetHoldPolicySettingsHandler
    : IRequestHandler<GetHoldPolicySettingsQuery, Result<HoldPolicySettingsView>>
{
    private readonly IStoreHoldPolicySettingsPort _storeHours;
    private readonly IPaymentHoldSettingsGateway _paymentHolds;
    private readonly IReservationCyclePolicyPreviewPort _reservationPreview;
    private readonly ICartPersistenceHoursSource _cartPersistence;

    /// <summary>Creates the handler.</summary>
    public GetHoldPolicySettingsHandler(
        IStoreHoldPolicySettingsPort storeHours,
        IPaymentHoldSettingsGateway paymentHolds,
        IReservationCyclePolicyPreviewPort reservationPreview,
        ICartPersistenceHoursSource cartPersistence)
    {
        _storeHours = storeHours;
        _paymentHolds = paymentHolds;
        _reservationPreview = reservationPreview;
        _cartPersistence = cartPersistence;
    }

    /// <inheritdoc />
    public async Task<Result<HoldPolicySettingsView>> Handle(
        GetHoldPolicySettingsQuery request,
        CancellationToken cancellationToken)
        => Result.Success(await HoldPolicySettingsComposer.BuildAsync(
            _storeHours,
            _paymentHolds,
            _reservationPreview,
            _cartPersistence,
            cancellationToken));
}
