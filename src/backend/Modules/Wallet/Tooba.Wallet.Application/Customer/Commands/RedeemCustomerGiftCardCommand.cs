using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Composition;
using Tooba.Wallet.Application.Customer.Models;
using Tooba.Wallet.Application.Ports;
using Tooba.Wallet.Contracts.Errors;

namespace Tooba.Wallet.Application.Customer.Commands;

/// <summary>MediatR RedeemCustomerGiftCard use case.</summary>
public sealed record RedeemCustomerGiftCardCommand(Guid CustomerActorUserId, string Code, string IdempotencyKey)
    : IRequest<Result<GiftCardRedeemResultDto>>;

/// <summary>Handles RedeemCustomerGiftCard.</summary>
public sealed class RedeemCustomerGiftCardHandler(IWalletDirectory directory)
    : IRequestHandler<RedeemCustomerGiftCardCommand, Result<GiftCardRedeemResultDto>>
{
    public Task<Result<GiftCardRedeemResultDto>> Handle(RedeemCustomerGiftCardCommand request, CancellationToken cancellationToken) =>
        WalletOperation.ExecuteAsync(
            () => directory.RedeemGiftCardForCustomerAsync(
                request.CustomerActorUserId,
                new RedeemGiftCardCommand(request.Code, request.IdempotencyKey),
                cancellationToken),
            WalletErrorCodes.RedeemRejected);
}
