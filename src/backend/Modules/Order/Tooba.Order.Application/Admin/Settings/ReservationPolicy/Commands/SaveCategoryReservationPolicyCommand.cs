using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Models;
using Tooba.Order.Application.ReservationCycle.Contracts;

namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy.Commands;

/// <summary>Admin PUT category reservation-policy override.</summary>
public sealed record SaveCategoryReservationPolicyCommand(
    Guid CategoryId,
    int? InitialReservationHoldMinutes,
    int? RetryReservationHoldMinutes,
    int? MaxReservationCycles,
    Guid ActorUserId) : IRequest<Result<ReservationPolicyEditorView>>;

/// <summary>Handler for <see cref="SaveCategoryReservationPolicyCommand"/>.</summary>
public sealed class SaveCategoryReservationPolicyHandler
    : IRequestHandler<SaveCategoryReservationPolicyCommand, Result<ReservationPolicyEditorView>>
{
    private readonly IStoreReservationPolicySettingsPort _settings;
    private readonly IReservationCyclePolicyResolver _resolver;

    /// <summary>Creates the handler.</summary>
    public SaveCategoryReservationPolicyHandler(
        IStoreReservationPolicySettingsPort settings,
        IReservationCyclePolicyResolver resolver)
    {
        _settings = settings;
        _resolver = resolver;
    }

    /// <inheritdoc />
    public async Task<Result<ReservationPolicyEditorView>> Handle(
        SaveCategoryReservationPolicyCommand request,
        CancellationToken cancellationToken)
    {
        await _settings.SaveCategoryOverrideAsync(
            request.CategoryId,
            new ReservationPolicyOverrideWrite(
                request.InitialReservationHoldMinutes,
                request.RetryReservationHoldMinutes,
                request.MaxReservationCycles),
            request.ActorUserId,
            cancellationToken);
        var preview = await _resolver.PreviewAsync(null, request.CategoryId, cancellationToken);
        return Result.Success(ReservationPolicyComposer.ForCategory(request.CategoryId, preview, true));
    }
}
