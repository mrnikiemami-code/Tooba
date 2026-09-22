using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Errors;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;

namespace Tooba.Wallet.Application.Queries.ListAdminGiftCards;

/// <summary>MediatR ListAdminGiftCards use case.</summary>
public sealed record ListAdminGiftCardsQuery(string? Status, string? Q, int Page, int PageSize)
    : IRequest<Result<GiftCardListPageDto>>;

/// <summary>Handles ListAdminGiftCards.</summary>
public sealed class ListAdminGiftCardsHandler(IWalletDirectory directory)
    : IRequestHandler<ListAdminGiftCardsQuery, Result<GiftCardListPageDto>>
{
    public Task<Result<GiftCardListPageDto>> Handle(ListAdminGiftCardsQuery request, CancellationToken cancellationToken) =>
        WalletExceptionMapper.TryAsync(
            () => directory.ListGiftCardsForAdminAsync(new AdminGiftCardListQuery(request.Status, request.Q, request.Page, request.PageSize), cancellationToken),
            WalletErrorCodes.GiftCardRejected);
}
