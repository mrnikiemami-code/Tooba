using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Models;

namespace Tooba.Payment.Application.Commands.CompleteSandboxPayment;

public sealed record CompleteSandboxPaymentCommand(
    Guid PaymentId,
    Guid CartId,
    string? GuestSecret,
    Guid AttemptId,
    string ProviderRequestReference,
    string Outcome,
    Guid? AuthenticatedUserId) : IRequest<Result<StorefrontPaymentDto>>;

public sealed class CompleteSandboxPaymentHandler(StorefrontPaymentOrchestrator orchestrator)
    : IRequestHandler<CompleteSandboxPaymentCommand, Result<StorefrontPaymentDto>>
{
    public Task<Result<StorefrontPaymentDto>> Handle(
        CompleteSandboxPaymentCommand request, CancellationToken cancellationToken) =>
        PaymentExceptionMapper.TryAsync(() => orchestrator.CompleteSandboxAsync(
            request.PaymentId,
            request.CartId,
            request.GuestSecret,
            request.AttemptId,
            request.ProviderRequestReference,
            request.Outcome,
            request.AuthenticatedUserId,
            cancellationToken));
}
