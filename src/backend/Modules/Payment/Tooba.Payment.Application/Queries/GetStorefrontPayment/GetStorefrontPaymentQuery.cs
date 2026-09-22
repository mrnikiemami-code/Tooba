using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Models;

namespace Tooba.Payment.Application.Queries.GetStorefrontPayment;

public sealed record GetStorefrontPaymentQuery(
    Guid PaymentId,
    Guid CartId,
    string? GuestSecret,
    Guid? AuthenticatedUserId) : IRequest<Result<StorefrontPaymentDto>>;

public sealed class GetStorefrontPaymentHandler(StorefrontPaymentOrchestrator orchestrator)
    : IRequestHandler<GetStorefrontPaymentQuery, Result<StorefrontPaymentDto>>
{
    public Task<Result<StorefrontPaymentDto>> Handle(
        GetStorefrontPaymentQuery request, CancellationToken cancellationToken) =>
        PaymentExceptionMapper.TryAsync(async () =>
        {
            var page = await orchestrator.GetAsync(
                request.PaymentId, request.CartId, request.GuestSecret, request.AuthenticatedUserId, cancellationToken);
            return page ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);
        });
}
