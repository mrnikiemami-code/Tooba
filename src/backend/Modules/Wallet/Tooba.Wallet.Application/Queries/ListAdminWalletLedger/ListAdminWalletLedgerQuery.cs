using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Errors;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;

namespace Tooba.Wallet.Application.Queries.ListAdminWalletLedger;

/// <summary>MediatR ListAdminWalletLedger use case.</summary>
public sealed record ListAdminWalletLedgerQuery(Guid CustomerActorUserId, int Page, int PageSize)
    : IRequest<Result<WalletLedgerPageDto>>;

/// <summary>Handles ListAdminWalletLedger.</summary>
public sealed class ListAdminWalletLedgerHandler(IWalletDirectory directory)
    : IRequestHandler<ListAdminWalletLedgerQuery, Result<WalletLedgerPageDto>>
{
    public Task<Result<WalletLedgerPageDto>> Handle(ListAdminWalletLedgerQuery request, CancellationToken cancellationToken) =>
        WalletExceptionMapper.TryAsync(
            () => directory.ListLedgerForAdminAsync(request.CustomerActorUserId, request.Page, request.PageSize, cancellationToken),
            WalletErrorCodes.WalletRejected);
}
