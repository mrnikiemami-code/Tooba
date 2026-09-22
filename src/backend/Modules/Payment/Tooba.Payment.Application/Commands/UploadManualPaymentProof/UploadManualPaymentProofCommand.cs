using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Models;

namespace Tooba.Payment.Application.Commands.UploadManualPaymentProof;

public sealed record UploadManualPaymentProofCommand(
    Guid PaymentId,
    Guid CartId,
    string? GuestSecret,
    Stream Content,
    string FileName,
    string ContentType,
    Guid? AuthenticatedUserId) : IRequest<Result<Guid>>;

public sealed class UploadManualPaymentProofHandler(StorefrontPaymentOrchestrator orchestrator)
    : IRequestHandler<UploadManualPaymentProofCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(
        UploadManualPaymentProofCommand request, CancellationToken cancellationToken) =>
        PaymentExceptionMapper.TryAsync(() => orchestrator.UploadManualProofAsync(
            request.PaymentId,
            request.CartId,
            request.GuestSecret,
            request.Content,
            request.FileName,
            request.ContentType,
            request.AuthenticatedUserId,
            cancellationToken));
}
