using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Errors;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;

namespace Tooba.Wallet.Application.Queries.GetAdminGiftCard;

/// <summary>MediatR GetAdminGiftCard use case.</summary>
public sealed record GetAdminGiftCardQuery(Guid CardId)
    : IRequest<Result<GiftCardDetailDto>>;

/// <summary>Handles GetAdminGiftCard.</summary>
public sealed class GetAdminGiftCardHandler(IWalletDirectory directory)
    : IRequestHandler<GetAdminGiftCardQuery, Result<GiftCardDetailDto>>
{
    public async Task<Result<GiftCardDetailDto>> Handle(GetAdminGiftCardQuery request, CancellationToken cancellationToken)
    {
        var detail = await directory.GetGiftCardForAdminAsync(request.CardId, cancellationToken);
        return detail is null
            ? Result.Failure<GiftCardDetailDto>(new SemanticError(WalletErrorCodes.GiftCardMissing))
            : Result.Success(detail);
    }
}
