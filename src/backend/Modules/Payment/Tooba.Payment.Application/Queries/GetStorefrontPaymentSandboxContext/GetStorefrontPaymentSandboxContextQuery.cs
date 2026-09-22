using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Models;

namespace Tooba.Payment.Application.Queries.GetStorefrontPaymentSandboxContext;

public sealed record GetStorefrontPaymentSandboxContextQuery(
    Guid PaymentId,
    Guid CartId,
    string? GuestSecret,
    Guid? AuthenticatedUserId) : IRequest<Result<StorefrontSandboxContextDto>>;

public sealed class GetStorefrontPaymentSandboxContextHandler(StorefrontPaymentOrchestrator orchestrator)
    : IRequestHandler<GetStorefrontPaymentSandboxContextQuery, Result<StorefrontSandboxContextDto>>
{
    public Task<Result<StorefrontSandboxContextDto>> Handle(
        GetStorefrontPaymentSandboxContextQuery request, CancellationToken cancellationToken) =>
        PaymentExceptionMapper.TryAsync(() => orchestrator.GetSandboxContextAsync(
            request.PaymentId, request.CartId, request.GuestSecret, request.AuthenticatedUserId, cancellationToken));
}
