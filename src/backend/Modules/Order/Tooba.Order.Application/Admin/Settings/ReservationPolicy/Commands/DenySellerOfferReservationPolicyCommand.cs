using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Models;

namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy.Commands;

/// <summary>Seller PUT offer reservation-policy — always denied (Host parity).</summary>
public sealed record DenySellerOfferReservationPolicyCommand(Guid OfferId)
    : IRequest<Result<ReservationPolicyEditorView>>;

/// <summary>Handler for <see cref="DenySellerOfferReservationPolicyCommand"/>.</summary>
public sealed class DenySellerOfferReservationPolicyHandler
    : IRequestHandler<DenySellerOfferReservationPolicyCommand, Result<ReservationPolicyEditorView>>
{
    /// <inheritdoc />
    public Task<Result<ReservationPolicyEditorView>> Handle(
        DenySellerOfferReservationPolicyCommand request,
        CancellationToken cancellationToken)
    {
        _ = request.OfferId;
        _ = ReservationPolicyErrors.SellerMutatePermission;
        return Task.FromResult(Result.Failure<ReservationPolicyEditorView>(
            new SemanticError(ReservationPolicyErrors.SellerDenied)));
    }
}
