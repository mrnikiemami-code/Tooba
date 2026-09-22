using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Ports;

namespace Tooba.Payment.Application.Commands.ConfirmAdminDeposit;

/// <summary>MediatR admin confirm deposit command.</summary>
public sealed record ConfirmAdminDepositCommand(Guid PaymentId)
    : IRequest<Result<PaymentVerificationResult>>;

/// <summary>Confirms manual deposit.</summary>
public sealed class ConfirmAdminDepositHandler(IPaymentAdminDirectory payments)
    : IRequestHandler<ConfirmAdminDepositCommand, Result<PaymentVerificationResult>>
{
    public Task<Result<PaymentVerificationResult>> Handle(
        ConfirmAdminDepositCommand request, CancellationToken cancellationToken) =>
        PaymentExceptionMapper.TryAsync(() => payments.ConfirmDepositAsync(request.PaymentId, cancellationToken));
}
