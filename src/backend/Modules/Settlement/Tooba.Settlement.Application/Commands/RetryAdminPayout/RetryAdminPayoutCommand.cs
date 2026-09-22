using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Application.Errors;
using Tooba.Settlement.Application.Ports;

namespace Tooba.Settlement.Application.Commands.RetryAdminPayout;

/// <summary>retry payout (admin).</summary>
public sealed record RetryAdminPayoutCommand(Guid PayoutRequestId, Guid ActorUserId)
    : IRequest<Result<PayoutRequestSnapshot>>;

/// <summary>Handler retry payout.</summary>
public sealed class RetryAdminPayoutCommandHandler(ISettlementDirectory settlement)
    : IRequestHandler<RetryAdminPayoutCommand, Result<PayoutRequestSnapshot>>
{
    /// <inheritdoc />
    public Task<Result<PayoutRequestSnapshot>> Handle(
        RetryAdminPayoutCommand request,
        CancellationToken cancellationToken) =>
        SettlementExceptionMapper.TryAsync(() => settlement.RetryPayoutAsync(
            new RetryPayoutCommand(request.PayoutRequestId, request.ActorUserId),
            cancellationToken));
}
