using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Composition;
using Tooba.Payment.Application.Storefront.Models;

namespace Tooba.Payment.Application.Storefront.Commands;

public sealed record RetryManualPaymentCommand(
    Guid PaymentId,
    Guid CartId,
    string? GuestSecret,
    Guid? AuthenticatedUserId) : IRequest<Result<StorefrontPaymentDto>>;

public sealed class RetryManualPaymentHandler(StorefrontPaymentOrchestrator orchestrator)
    : IRequestHandler<RetryManualPaymentCommand, Result<StorefrontPaymentDto>>
{
    public Task<Result<StorefrontPaymentDto>> Handle(
        RetryManualPaymentCommand request, CancellationToken cancellationToken) =>
        PaymentOperation.ExecuteAsync(() => orchestrator.RetryManualAsync(
            request.PaymentId, request.CartId, request.GuestSecret, request.AuthenticatedUserId, cancellationToken));
}
