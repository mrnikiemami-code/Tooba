using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Composition;
using Tooba.Payment.Application.Storefront.Models;

namespace Tooba.Payment.Application.Storefront.Queries;

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
        PaymentOperation.ExecuteAsync(() => orchestrator.GetWalletQuoteAsync(
            request.CheckoutId, request.CartId, request.GuestSecret, request.AuthenticatedUserId, cancellationToken));
}
