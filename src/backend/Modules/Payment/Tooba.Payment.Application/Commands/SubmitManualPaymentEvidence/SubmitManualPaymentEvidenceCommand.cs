using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Models;

namespace Tooba.Payment.Application.Commands.SubmitManualPaymentEvidence;

public sealed record SubmitManualPaymentEvidenceCommand(
    Guid PaymentId,
    Guid CartId,
    string? GuestSecret,
    string TransferReference,
    Guid? ProofMediaAssetId,
    Guid? AuthenticatedUserId) : IRequest<Result<StorefrontPaymentDto>>;

public sealed class SubmitManualPaymentEvidenceHandler(StorefrontPaymentOrchestrator orchestrator)
    : IRequestHandler<SubmitManualPaymentEvidenceCommand, Result<StorefrontPaymentDto>>
{
    public Task<Result<StorefrontPaymentDto>> Handle(
        SubmitManualPaymentEvidenceCommand request, CancellationToken cancellationToken) =>
        PaymentExceptionMapper.TryAsync(() => orchestrator.SubmitManualEvidenceAsync(
            request.PaymentId,
            request.CartId,
            request.GuestSecret,
            request.TransferReference,
            request.ProofMediaAssetId,
            request.AuthenticatedUserId,
            cancellationToken));
}
