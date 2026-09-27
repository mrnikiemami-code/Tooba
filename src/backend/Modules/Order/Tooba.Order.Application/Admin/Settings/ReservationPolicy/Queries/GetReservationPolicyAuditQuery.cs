using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Models;

namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy.Queries;

/// <summary>Admin GET reservation-policy settings audit.</summary>
public sealed record GetReservationPolicyAuditQuery(int? Take)
    : IRequest<Result<ReservationPolicyAuditListView>>;

/// <summary>Handler for <see cref="GetReservationPolicyAuditQuery"/>.</summary>
public sealed class GetReservationPolicyAuditHandler
    : IRequestHandler<GetReservationPolicyAuditQuery, Result<ReservationPolicyAuditListView>>
{
    private readonly IStoreReservationPolicySettingsPort _settings;

    /// <summary>Creates the handler.</summary>
    public GetReservationPolicyAuditHandler(IStoreReservationPolicySettingsPort settings) => _settings = settings;

    /// <inheritdoc />
    public async Task<Result<ReservationPolicyAuditListView>> Handle(
        GetReservationPolicyAuditQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _settings.ListAuditAsync(request.Take ?? 50, cancellationToken);
        var items = rows.Select(x => new ReservationPolicyAuditView(
            x.EventId,
            x.Level,
            x.ScopeId,
            x.Field,
            x.OldOverride,
            x.NewOverride,
            x.ActorUserId,
            x.OccurredAt)).ToList();
        return Result.Success(new ReservationPolicyAuditListView(items));
    }
}
