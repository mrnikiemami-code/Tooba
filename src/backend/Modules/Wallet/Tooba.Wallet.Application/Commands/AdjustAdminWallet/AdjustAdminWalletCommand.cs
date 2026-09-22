using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Errors;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;

namespace Tooba.Wallet.Application.Commands.AdjustAdminWallet;

/// <summary>MediatR AdjustAdminWallet use case.</summary>
public sealed record AdjustAdminWalletCommand(Guid CustomerActorUserId, Guid AdminActorUserId, decimal Amount, string Direction, string Reason, string IdempotencyKey)
    : IRequest<Result<AdminWalletAdjustmentResultDto>>;

/// <summary>Handles AdjustAdminWallet.</summary>
public sealed class AdjustAdminWalletHandler(IWalletDirectory directory)
    : IRequestHandler<AdjustAdminWalletCommand, Result<AdminWalletAdjustmentResultDto>>
{
    public Task<Result<AdminWalletAdjustmentResultDto>> Handle(AdjustAdminWalletCommand request, CancellationToken cancellationToken) =>
        WalletExceptionMapper.TryAsync(
            () => directory.AdjustWalletForAdminAsync(
                request.CustomerActorUserId,
                request.AdminActorUserId,
                new AdminWalletAdjustmentCommand(request.Amount, request.Direction, request.Reason, request.IdempotencyKey),
                cancellationToken),
            WalletErrorCodes.AdjustRejected);
}
