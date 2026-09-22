using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Errors;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;

namespace Tooba.Wallet.Application.Commands.IssueAdminGiftCard;

/// <summary>MediatR IssueAdminGiftCard use case.</summary>
public sealed record IssueAdminGiftCardCommand(Guid AdminActorUserId, decimal InitialAmount, string? Currency, DateTimeOffset? ExpiresAt, Guid? RecipientActorUserId, string IdempotencyKey)
    : IRequest<Result<GiftCardIssueResultDto>>;

/// <summary>Handles IssueAdminGiftCard.</summary>
public sealed class IssueAdminGiftCardHandler(IWalletDirectory directory)
    : IRequestHandler<IssueAdminGiftCardCommand, Result<GiftCardIssueResultDto>>
{
    public Task<Result<GiftCardIssueResultDto>> Handle(IssueAdminGiftCardCommand request, CancellationToken cancellationToken) =>
        WalletExceptionMapper.TryAsync(
            () => directory.IssueGiftCardForAdminAsync(
                request.AdminActorUserId,
                new IssueGiftCardCommand(request.InitialAmount, request.Currency, request.ExpiresAt, request.RecipientActorUserId, request.IdempotencyKey),
                cancellationToken),
            WalletErrorCodes.GiftCardIssueRejected);
}
