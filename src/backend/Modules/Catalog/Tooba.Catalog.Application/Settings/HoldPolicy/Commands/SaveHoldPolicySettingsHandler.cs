using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Cart.Contracts.Lifetime;
using Tooba.Catalog.Application.Settings.HoldPolicy.Models;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Order.Contracts.Reservation;
using Tooba.Payment.Contracts.Hold;

namespace Tooba.Catalog.Application.Settings.HoldPolicy.Commands;

/// <summary>Handler for <see cref="SaveHoldPolicySettingsCommand"/>.</summary>
public sealed class SaveHoldPolicySettingsHandler
    : IRequestHandler<SaveHoldPolicySettingsCommand, Result<HoldPolicySettingsView>>
{
    private readonly IStoreHoldPolicySettingsPort _storeHours;
    private readonly IStoreReservationPolicySettingsPort _reservationSettings;
    private readonly IPaymentHoldSettingsGateway _paymentHolds;
    private readonly IReservationCyclePolicyPreviewPort _reservationPreview;
    private readonly ICartPersistenceHoursSource _cartPersistence;
    private readonly IClock _clock;

    /// <summary>Creates the handler.</summary>
    public SaveHoldPolicySettingsHandler(
        IStoreHoldPolicySettingsPort storeHours,
        IStoreReservationPolicySettingsPort reservationSettings,
        IPaymentHoldSettingsGateway paymentHolds,
        IReservationCyclePolicyPreviewPort reservationPreview,
        ICartPersistenceHoursSource cartPersistence,
        IClock clock)
    {
        _storeHours = storeHours;
        _reservationSettings = reservationSettings;
        _paymentHolds = paymentHolds;
        _reservationPreview = reservationPreview;
        _cartPersistence = cartPersistence;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<HoldPolicySettingsView>> Handle(
        SaveHoldPolicySettingsCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        await _storeHours.SaveHoursAsync(
            new StoreHoldPolicyHoursWrite(
                request.CartPersistenceHours,
                request.OnlinePaymentHoldHours,
                request.ManualPaymentInitialHoldHours,
                request.ManualPaymentReviewHoldHours),
            cancellationToken);

        await _reservationSettings.SaveStoreOverridesAsync(
            new ReservationPolicyOverrideWrite(
                request.InitialReservationHoldMinutes,
                request.RetryReservationHoldMinutes,
                request.MaxReservationCycles),
            request.ActorUserId,
            cancellationToken);

        foreach (var method in request.Methods ?? [])
        {
            await _paymentHolds.UpsertMethodOverrideAsync(
                method.ProviderCode,
                method.OnlinePaymentHoldHours,
                method.ManualPaymentInitialHoldHours,
                method.ManualPaymentReviewHoldHours,
                now,
                cancellationToken);
        }

        return Result.Success(await HoldPolicySettingsComposer.BuildAsync(
            _storeHours,
            _paymentHolds,
            _reservationPreview,
            _cartPersistence,
            cancellationToken));
    }
}
