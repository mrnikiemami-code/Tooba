using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Composition;
using Tooba.Wallet.Application.Admin.Models;
using Tooba.Wallet.Application.Customer.Models;
using Tooba.Wallet.Application.Ports;
using Tooba.Wallet.Contracts.Errors;

namespace Tooba.Wallet.Application.Admin.Queries;

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
        return WalletOperation.NotFoundIfNull(detail, WalletErrorCodes.GiftCardMissing);
    }
}
