using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Errors;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;

namespace Tooba.Wallet.Application.Queries.GetWalletDemoPreview;

/// <summary>MediatR GetWalletDemoPreview use case.</summary>
public sealed record GetWalletDemoPreviewQuery
    : IRequest<Result<WalletDemoPreviewDto>>;

/// <summary>Handles GetWalletDemoPreview.</summary>
public sealed class GetWalletDemoPreviewHandler(IWalletDemoPreviewPort demo)
    : IRequestHandler<GetWalletDemoPreviewQuery, Result<WalletDemoPreviewDto>>
{
    public Task<Result<WalletDemoPreviewDto>> Handle(GetWalletDemoPreviewQuery request, CancellationToken cancellationToken)
    {
        var snapshot = demo.TryGetCurrent();
        return Task.FromResult(snapshot is null
            ? Result.Failure<WalletDemoPreviewDto>(new SemanticError(WalletErrorCodes.DemoNotReady))
            : Result.Success(snapshot));
    }
}
