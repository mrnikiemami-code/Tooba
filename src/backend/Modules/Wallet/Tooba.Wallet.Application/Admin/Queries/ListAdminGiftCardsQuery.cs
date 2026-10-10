using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Composition;
using Tooba.Wallet.Application.Admin.Models;
using Tooba.Wallet.Application.Customer.Models;
using Tooba.Wallet.Application.Ports;
using Tooba.Wallet.Contracts.Errors;

namespace Tooba.Wallet.Application.Admin.Queries;

/// <summary>MediatR ListAdminGiftCards use case.</summary>
public sealed record ListAdminGiftCardsQuery(string? Status, string? Q, int Page, int PageSize)
    : IRequest<Result<GiftCardListPageDto>>;

/// <summary>Handles ListAdminGiftCards.</summary>
public sealed class ListAdminGiftCardsHandler(IWalletDirectory directory)
    : IRequestHandler<ListAdminGiftCardsQuery, Result<GiftCardListPageDto>>
{
    public Task<Result<GiftCardListPageDto>> Handle(ListAdminGiftCardsQuery request, CancellationToken cancellationToken) =>
        WalletOperation.ExecuteAsync(
            () => directory.ListGiftCardsForAdminAsync(
                new AdminGiftCardListQuery(request.Status, request.Q, request.Page, request.PageSize), cancellationToken),
            WalletErrorCodes.GiftCardRejected);
}
