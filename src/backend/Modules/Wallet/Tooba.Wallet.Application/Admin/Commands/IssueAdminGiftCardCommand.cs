using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Composition;
using Tooba.Wallet.Application.Admin.Models;
using Tooba.Wallet.Application.Customer.Models;
using Tooba.Wallet.Application.Ports;
using Tooba.Wallet.Contracts.Errors;

namespace Tooba.Wallet.Application.Admin.Commands;

/// <summary>MediatR IssueAdminGiftCard use case.</summary>
public sealed record IssueAdminGiftCardCommand(Guid AdminActorUserId, decimal InitialAmount, string? Currency, DateTimeOffset? ExpiresAt, Guid? RecipientActorUserId, string IdempotencyKey)
    : IRequest<Result<GiftCardIssueResultDto>>;

/// <summary>Handles IssueAdminGiftCard.</summary>
public sealed class IssueAdminGiftCardHandler(IWalletDirectory directory)
    : IRequestHandler<IssueAdminGiftCardCommand, Result<GiftCardIssueResultDto>>
{
    public Task<Result<GiftCardIssueResultDto>> Handle(IssueAdminGiftCardCommand request, CancellationToken cancellationToken) =>
        WalletOperation.ExecuteAsync(
            () => directory.IssueGiftCardForAdminAsync(
                request.AdminActorUserId,
                new IssueGiftCardCommand(request.InitialAmount, request.Currency, request.ExpiresAt, request.RecipientActorUserId, request.IdempotencyKey),
                cancellationToken),
            WalletErrorCodes.GiftCardIssueRejected);
}
