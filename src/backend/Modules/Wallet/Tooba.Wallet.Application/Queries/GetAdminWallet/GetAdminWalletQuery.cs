using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Errors;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;

namespace Tooba.Wallet.Application.Queries.GetAdminWallet;

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
        return summary is null
            ? Result.Failure<WalletSummaryDto>(new SemanticError(WalletErrorCodes.WalletMissing))
            : Result.Success(summary);
    }
}
