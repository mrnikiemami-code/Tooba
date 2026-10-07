using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Composition;
using Tooba.Payment.Application.Ports;

namespace Tooba.Payment.Application.Admin.Commands;

/// <summary>MediatR admin reconcile payment command.</summary>
public sealed record ReconcileAdminPaymentCommand(Guid PaymentId)
    : IRequest<Result<PaymentVerificationResult>>;

/// <summary>Reconciles one admin payment.</summary>
public sealed class ReconcileAdminPaymentHandler(IPaymentAdminDirectory payments)
    : IRequestHandler<ReconcileAdminPaymentCommand, Result<PaymentVerificationResult>>
{
    public Task<Result<PaymentVerificationResult>> Handle(
        ReconcileAdminPaymentCommand request, CancellationToken cancellationToken) =>
        PaymentOperation.ExecuteAsync(() => payments.ReconcileAsync(request.PaymentId, cancellationToken));
}
