using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Application.Errors;
using Tooba.Settlement.Application.Ports;

namespace Tooba.Settlement.Application.Commands.RequestSellerPayout;

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
        SettlementExceptionMapper.TryAsync(() => settlement.RequestPayoutAsync(
            new RequestPayoutCommand(
                request.SellerPartyId,
                request.Amount,
                request.IdempotencyKey,
                request.ActorUserId),
            cancellationToken));
}
