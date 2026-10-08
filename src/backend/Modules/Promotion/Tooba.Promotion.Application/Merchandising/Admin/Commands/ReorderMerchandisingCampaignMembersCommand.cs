using MediatR;
using Tooba.BuildingBlocks.Results;

using Tooba.Promotion.Application.Merchandising.Ports;

using Tooba.Promotion.Application.Merchandising.Models;

namespace Tooba.Promotion.Application.Merchandising.Admin.Commands;

/// <summary>بازچینش اعضای کمپین با لیست مرتب SellerOfferId.</summary>
public sealed record ReorderMerchandisingCampaignMembersCommand(
    Guid CampaignId,
    IReadOnlyList<Guid> OrderedSellerOfferIds) : IRequest<Result<AdminMerchCampaignDetail>>;

/// <summary>Handler بازچینش اعضا.</summary>
public sealed class ReorderMerchandisingCampaignMembersCommandHandler(IMerchandisingCampaignAdminComposer c)
    : IRequestHandler<ReorderMerchandisingCampaignMembersCommand, Result<AdminMerchCampaignDetail>>
{
    /// <inheritdoc />
    public Task<Result<AdminMerchCampaignDetail>> Handle(
        ReorderMerchandisingCampaignMembersCommand r,
        CancellationToken ct) =>
        MerchandisingAdminResult.ExecuteOrMissingAsync(() => c.ReorderMembersAsync(r.CampaignId, r.OrderedSellerOfferIds, ct));
}
