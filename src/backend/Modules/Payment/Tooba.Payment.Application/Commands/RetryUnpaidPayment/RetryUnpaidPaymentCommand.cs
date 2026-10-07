using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Composition;
using Tooba.Payment.Application.Models;

namespace Tooba.Payment.Application.Commands.RetryUnpaidPayment;

public sealed record RetryUnpaidPaymentCommand(
    Guid PaymentId,
    Guid CartId,
    string? GuestSecret,
    Guid? AuthenticatedUserId) : IRequest<Result<StorefrontPaymentDto>>;

public sealed class RetryUnpaidPaymentHandler(StorefrontPaymentOrchestrator orchestrator)
    : IRequestHandler<RetryUnpaidPaymentCommand, Result<StorefrontPaymentDto>>
{
    public Task<Result<StorefrontPaymentDto>> Handle(
        RetryUnpaidPaymentCommand request, CancellationToken cancellationToken) =>
        PaymentOperation.ExecuteAsync(() => orchestrator.RetryUnpaidAsync(
            request.PaymentId, request.CartId, request.GuestSecret, request.AuthenticatedUserId, cancellationToken));
}
