using MediatR;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Promotion.Application.Merchandising.Admin.Commands;

/// <summary>حذف عضویت Offer از کمپین بدون حذف SellerOffer.</summary>
public sealed record RemoveMerchandisingCampaignMemberCommand(
    Guid CampaignId,
    Guid SellerOfferId) : IRequest<Result<AdminMerchCampaignDetail>>;

/// <summary>Handler حذف عضو.</summary>
public sealed class RemoveMerchandisingCampaignMemberCommandHandler(IMerchandisingCampaignAdminComposer c)
    : IRequestHandler<RemoveMerchandisingCampaignMemberCommand, Result<AdminMerchCampaignDetail>>
{
    /// <inheritdoc />
    public Task<Result<AdminMerchCampaignDetail>> Handle(RemoveMerchandisingCampaignMemberCommand r, CancellationToken ct) =>
        MerchandisingAdminResult.ExecuteOrMissingAsync(() => c.RemoveMemberAsync(r.CampaignId, r.SellerOfferId, ct));
}
