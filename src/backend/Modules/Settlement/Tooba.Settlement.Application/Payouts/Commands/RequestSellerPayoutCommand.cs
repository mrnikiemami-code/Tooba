using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Application.Composition;
using Tooba.Settlement.Application.Payouts.Ports;

namespace Tooba.Settlement.Application.Payouts.Commands;

/// <summary>درخواست payout فروشنده.</summary>
public sealed record RequestSellerPayoutCommand(
    Guid SellerPartyId,
    Guid ActorUserId,
    decimal Amount,
    string IdempotencyKey) : IRequest<Result<PayoutRequestSnapshot>>;

/// <summary>Handler درخواست payout.</summary>
public sealed class RequestSellerPayoutCommandHandler(ISettlementDirectory settlement)
    : IRequestHandler<RequestSellerPayoutCommand, Result<PayoutRequestSnapshot>>
{
    /// <inheritdoc />
    public Task<Result<PayoutRequestSnapshot>> Handle(
        RequestSellerPayoutCommand request,
        CancellationToken cancellationToken) =>
        SettlementOperation.ExecuteAsync(() => settlement.RequestPayoutAsync(
            new RequestPayoutCommand(
                request.SellerPartyId,
                request.Amount,
                request.IdempotencyKey,
                request.ActorUserId),
            cancellationToken));
}
