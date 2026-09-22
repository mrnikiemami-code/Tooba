using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Application.Errors;
using Tooba.Settlement.Application.Ports;

namespace Tooba.Settlement.Application.Commands.ProcessAdminPayout;

/// <summary>پردازش payout (admin).</summary>
public sealed record ProcessAdminPayoutCommand(Guid PayoutRequestId, Guid ActorUserId)
    : IRequest<Result<PayoutRequestSnapshot>>;

/// <summary>Handler پردازش payout.</summary>
public sealed class ProcessAdminPayoutCommandHandler(ISettlementDirectory settlement)
    : IRequestHandler<ProcessAdminPayoutCommand, Result<PayoutRequestSnapshot>>
{
    /// <inheritdoc />
    public Task<Result<PayoutRequestSnapshot>> Handle(
        ProcessAdminPayoutCommand request,
        CancellationToken cancellationToken) =>
        SettlementExceptionMapper.TryAsync(() => settlement.ProcessPayoutAsync(
            new ProcessPayoutCommand(request.PayoutRequestId, request.ActorUserId),
            cancellationToken));
}
