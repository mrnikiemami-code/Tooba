using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Models;
using Tooba.Order.Application.ReservationCycle.Contracts;

namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy.Commands;

/// <summary>Admin PUT store reservation-policy overrides.</summary>
public sealed record SaveStoreReservationPolicyCommand(
    int? InitialReservationHoldMinutes,
    int? RetryReservationHoldMinutes,
    int? MaxReservationCycles,
    Guid ActorUserId) : IRequest<Result<ReservationPolicyEditorView>>;

/// <summary>Handler for <see cref="SaveStoreReservationPolicyCommand"/>.</summary>
public sealed class SaveStoreReservationPolicyHandler
    : IRequestHandler<SaveStoreReservationPolicyCommand, Result<ReservationPolicyEditorView>>
{
    private readonly IStoreReservationPolicySettingsPort _settings;
    private readonly IReservationCyclePolicyResolver _resolver;

    /// <summary>Creates the handler.</summary>
    public SaveStoreReservationPolicyHandler(
        IStoreReservationPolicySettingsPort settings,
        IReservationCyclePolicyResolver resolver)
    {
        _settings = settings;
        _resolver = resolver;
    }

    /// <inheritdoc />
    public async Task<Result<ReservationPolicyEditorView>> Handle(
        SaveStoreReservationPolicyCommand request,
        CancellationToken cancellationToken)
    {
        await _settings.SaveStoreOverridesAsync(
            new ReservationPolicyOverrideWrite(
                request.InitialReservationHoldMinutes,
                request.RetryReservationHoldMinutes,
                request.MaxReservationCycles),
            request.ActorUserId,
            cancellationToken);
        var preview = await _resolver.PreviewAsync(null, null, cancellationToken);
        return Result.Success(ReservationPolicyComposer.ForStore(preview, true));
    }
}
