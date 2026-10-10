using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Composition;
using Tooba.Wallet.Application.Customer.Models;
using Tooba.Wallet.Application.Ports;
using Tooba.Wallet.Contracts.Errors;

namespace Tooba.Wallet.Application.Customer.Queries;

/// <summary>MediatR ListCustomerWalletLedger use case.</summary>
public sealed record ListCustomerWalletLedgerQuery(Guid CustomerActorUserId, int Page, int PageSize)
    : IRequest<Result<WalletLedgerPageDto>>;

/// <summary>Handles ListCustomerWalletLedger.</summary>
public sealed class ListCustomerWalletLedgerHandler(IWalletDirectory directory)
    : IRequestHandler<ListCustomerWalletLedgerQuery, Result<WalletLedgerPageDto>>
{
    public Task<Result<WalletLedgerPageDto>> Handle(ListCustomerWalletLedgerQuery request, CancellationToken cancellationToken) =>
        WalletOperation.ExecuteAsync(
            () => directory.ListLedgerForCustomerAsync(request.CustomerActorUserId, request.Page, request.PageSize, cancellationToken),
            WalletErrorCodes.WalletRejected);
}
