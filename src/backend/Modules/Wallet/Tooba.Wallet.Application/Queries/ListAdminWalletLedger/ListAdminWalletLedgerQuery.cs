using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Composition;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;
using Tooba.Wallet.Contracts.Errors;

namespace Tooba.Wallet.Application.Queries.ListAdminWalletLedger;

/// <summary>MediatR ListAdminWalletLedger use case.</summary>
public sealed record ListAdminWalletLedgerQuery(Guid CustomerActorUserId, int Page, int PageSize)
    : IRequest<Result<WalletLedgerPageDto>>;

/// <summary>Handles ListAdminWalletLedger.</summary>
public sealed class ListAdminWalletLedgerHandler(IWalletDirectory directory)
    : IRequestHandler<ListAdminWalletLedgerQuery, Result<WalletLedgerPageDto>>
{
    public Task<Result<WalletLedgerPageDto>> Handle(ListAdminWalletLedgerQuery request, CancellationToken cancellationToken) =>
        WalletOperation.ExecuteAsync(
            () => directory.ListLedgerForAdminAsync(request.CustomerActorUserId, request.Page, request.PageSize, cancellationToken),
            WalletErrorCodes.WalletRejected);
}
