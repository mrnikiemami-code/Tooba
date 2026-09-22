using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Ports;

namespace Tooba.Payment.Application.Commands.RejectAdminDeposit;

/// <summary>MediatR admin reject deposit command.</summary>
public sealed record RejectAdminDepositCommand(Guid PaymentId)
    : IRequest<Result<PaymentVerificationResult>>;

/// <summary>Rejects manual deposit.</summary>
public sealed class RejectAdminDepositHandler(IPaymentAdminDirectory payments)
    : IRequestHandler<RejectAdminDepositCommand, Result<PaymentVerificationResult>>
{
    public Task<Result<PaymentVerificationResult>> Handle(
        RejectAdminDepositCommand request, CancellationToken cancellationToken) =>
        PaymentExceptionMapper.TryAsync(() => payments.RejectDepositAsync(request.PaymentId, cancellationToken));
}
