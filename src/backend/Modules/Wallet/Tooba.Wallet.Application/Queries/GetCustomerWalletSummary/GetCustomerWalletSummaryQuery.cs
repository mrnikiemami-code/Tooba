using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Composition;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;
using Tooba.Wallet.Contracts.Errors;

namespace Tooba.Wallet.Application.Queries.GetCustomerWalletSummary;

/// <summary>MediatR GetCustomerWalletSummary use case.</summary>
public sealed record GetCustomerWalletSummaryQuery(Guid CustomerActorUserId)
    : IRequest<Result<WalletSummaryDto>>;

/// <summary>Handles GetCustomerWalletSummary.</summary>
public sealed class GetCustomerWalletSummaryHandler(IWalletDirectory directory)
    : IRequestHandler<GetCustomerWalletSummaryQuery, Result<WalletSummaryDto>>
{
    public Task<Result<WalletSummaryDto>> Handle(GetCustomerWalletSummaryQuery request, CancellationToken cancellationToken) =>
        WalletOperation.ExecuteAsync(
            () => directory.GetOrCreateSummaryForCustomerAsync(request.CustomerActorUserId, cancellationToken),
            WalletErrorCodes.WalletRejected);
}
