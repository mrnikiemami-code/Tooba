using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Errors;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;

namespace Tooba.Wallet.Application.Commands.RedeemCustomerGiftCard;

/// <summary>MediatR RedeemCustomerGiftCard use case.</summary>
public sealed record RedeemCustomerGiftCardCommand(Guid CustomerActorUserId, string Code, string IdempotencyKey)
    : IRequest<Result<GiftCardRedeemResultDto>>;

/// <summary>Handles RedeemCustomerGiftCard.</summary>
public sealed class RedeemCustomerGiftCardHandler(IWalletDirectory directory)
    : IRequestHandler<RedeemCustomerGiftCardCommand, Result<GiftCardRedeemResultDto>>
{
    public Task<Result<GiftCardRedeemResultDto>> Handle(RedeemCustomerGiftCardCommand request, CancellationToken cancellationToken) =>
        WalletExceptionMapper.TryAsync(
            () => directory.RedeemGiftCardForCustomerAsync(
                request.CustomerActorUserId,
                new RedeemGiftCardCommand(request.Code, request.IdempotencyKey),
                cancellationToken),
            WalletErrorCodes.RedeemRejected);
}
