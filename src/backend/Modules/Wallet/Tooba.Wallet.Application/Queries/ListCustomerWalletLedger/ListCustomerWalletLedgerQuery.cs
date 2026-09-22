using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Errors;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;

namespace Tooba.Wallet.Application.Queries.ListCustomerWalletLedger;

/// <summary>MediatR ListCustomerWalletLedger use case.</summary>
public sealed record ListCustomerWalletLedgerQuery(Guid CustomerActorUserId, int Page, int PageSize)
    : IRequest<Result<WalletLedgerPageDto>>;

/// <summary>Handles ListCustomerWalletLedger.</summary>
public sealed class ListCustomerWalletLedgerHandler(IWalletDirectory directory)
    : IRequestHandler<ListCustomerWalletLedgerQuery, Result<WalletLedgerPageDto>>
{
    public Task<Result<WalletLedgerPageDto>> Handle(ListCustomerWalletLedgerQuery request, CancellationToken cancellationToken) =>
        WalletExceptionMapper.TryAsync(
            () => directory.ListLedgerForCustomerAsync(request.CustomerActorUserId, request.Page, request.PageSize, cancellationToken),
            WalletErrorCodes.WalletRejected);
}
