using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Errors;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;

namespace Tooba.Wallet.Application.Commands.RevokeAdminGiftCard;

/// <summary>MediatR RevokeAdminGiftCard use case.</summary>
public sealed record RevokeAdminGiftCardCommand(Guid CardId)
    : IRequest<Result<GiftCardDetailDto>>;

/// <summary>Handles RevokeAdminGiftCard.</summary>
public sealed class RevokeAdminGiftCardHandler(IWalletDirectory directory)
    : IRequestHandler<RevokeAdminGiftCardCommand, Result<GiftCardDetailDto>>
{
    public Task<Result<GiftCardDetailDto>> Handle(RevokeAdminGiftCardCommand request, CancellationToken cancellationToken) =>
        WalletExceptionMapper.TryAsync(
            () => directory.RevokeGiftCardForAdminAsync(request.CardId, cancellationToken),
            WalletErrorCodes.GiftCardRevokeRejected);
}
