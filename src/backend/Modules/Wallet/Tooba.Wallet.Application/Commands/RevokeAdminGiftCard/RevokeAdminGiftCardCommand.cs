using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Composition;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;
using Tooba.Wallet.Contracts.Errors;

namespace Tooba.Wallet.Application.Commands.RevokeAdminGiftCard;

/// <summary>MediatR RevokeAdminGiftCard use case.</summary>
public sealed record RevokeAdminGiftCardCommand(Guid CardId)
    : IRequest<Result<GiftCardDetailDto>>;

/// <summary>Handles RevokeAdminGiftCard.</summary>
public sealed class RevokeAdminGiftCardHandler(IWalletDirectory directory)
    : IRequestHandler<RevokeAdminGiftCardCommand, Result<GiftCardDetailDto>>
{
    public Task<Result<GiftCardDetailDto>> Handle(RevokeAdminGiftCardCommand request, CancellationToken cancellationToken) =>
        WalletOperation.ExecuteAsync(
            () => directory.RevokeGiftCardForAdminAsync(request.CardId, cancellationToken),
            WalletErrorCodes.GiftCardRevokeRejected);
}
