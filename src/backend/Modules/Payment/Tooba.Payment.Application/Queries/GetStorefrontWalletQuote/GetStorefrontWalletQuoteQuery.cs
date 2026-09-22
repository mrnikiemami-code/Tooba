using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Models;

namespace Tooba.Payment.Application.Queries.GetStorefrontWalletQuote;

public sealed record GetStorefrontWalletQuoteQuery(
    Guid CheckoutId,
    Guid CartId,
    string? GuestSecret,
    Guid? AuthenticatedUserId) : IRequest<Result<StorefrontWalletQuoteDto>>;

public sealed class GetStorefrontWalletQuoteHandler(StorefrontPaymentOrchestrator orchestrator)
    : IRequestHandler<GetStorefrontWalletQuoteQuery, Result<StorefrontWalletQuoteDto>>
{
    public Task<Result<StorefrontWalletQuoteDto>> Handle(
        GetStorefrontWalletQuoteQuery request, CancellationToken cancellationToken) =>
        PaymentExceptionMapper.TryAsync(() => orchestrator.GetWalletQuoteAsync(
            request.CheckoutId, request.CartId, request.GuestSecret, request.AuthenticatedUserId, cancellationToken));
}
