using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Models;
using Tooba.Order.Application.ReservationCycle.Contracts;

namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy.Queries;

/// <summary>Admin GET store reservation-policy preview.</summary>
public sealed record GetStoreReservationPolicyQuery : IRequest<Result<ReservationPolicyEditorView>>;

/// <summary>Handler for <see cref="GetStoreReservationPolicyQuery"/>.</summary>
public sealed class GetStoreReservationPolicyHandler
    : IRequestHandler<GetStoreReservationPolicyQuery, Result<ReservationPolicyEditorView>>
{
    private readonly IReservationCyclePolicyResolver _resolver;

    /// <summary>Creates the handler.</summary>
    public GetStoreReservationPolicyHandler(IReservationCyclePolicyResolver resolver) => _resolver = resolver;

    /// <inheritdoc />
    public async Task<Result<ReservationPolicyEditorView>> Handle(
        GetStoreReservationPolicyQuery request,
        CancellationToken cancellationToken)
    {
        var preview = await _resolver.PreviewAsync(null, null, cancellationToken);
        return Result.Success(ReservationPolicyComposer.ForStore(preview, true));
    }
}
