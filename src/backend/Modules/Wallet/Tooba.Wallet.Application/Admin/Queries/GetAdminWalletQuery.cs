using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Composition;
using Tooba.Wallet.Application.Admin.Models;
using Tooba.Wallet.Application.Customer.Models;
using Tooba.Wallet.Application.Ports;
using Tooba.Wallet.Contracts.Errors;

namespace Tooba.Wallet.Application.Admin.Queries;

/// <summary>MediatR GetAdminWallet use case.</summary>
public sealed record GetAdminWalletQuery(Guid CustomerActorUserId)
    : IRequest<Result<WalletSummaryDto>>;

/// <summary>Handles GetAdminWallet.</summary>
public sealed class GetAdminWalletHandler(IWalletDirectory directory)
    : IRequestHandler<GetAdminWalletQuery, Result<WalletSummaryDto>>
{
    public async Task<Result<WalletSummaryDto>> Handle(GetAdminWalletQuery request, CancellationToken cancellationToken)
    {
        var summary = await directory.GetWalletForAdminAsync(request.CustomerActorUserId, cancellationToken);
        return WalletOperation.NotFoundIfNull(summary, WalletErrorCodes.WalletMissing);
    }
}
