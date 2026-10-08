using MediatR;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Promotion.Application.Merchandising.Admin.Commands;

/// <summary>افزودن Offer به کمپین.</summary>
public sealed record AddMerchandisingCampaignMemberCommand(
    Guid CampaignId,
    Guid SellerOfferId) : IRequest<Result<AdminMerchCampaignDetail>>;

/// <summary>Handler افزودن عضو.</summary>
public sealed class AddMerchandisingCampaignMemberCommandHandler(IMerchandisingCampaignAdminComposer c)
    : IRequestHandler<AddMerchandisingCampaignMemberCommand, Result<AdminMerchCampaignDetail>>
{
    /// <inheritdoc />
    public Task<Result<AdminMerchCampaignDetail>> Handle(AddMerchandisingCampaignMemberCommand r, CancellationToken ct) =>
        MerchandisingAdminResult.ExecuteOrMissingAsync(() => c.AddMemberAsync(r.CampaignId, r.SellerOfferId, ct));
}
