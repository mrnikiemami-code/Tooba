using MediatR;
using Tooba.BuildingBlocks.Results;

using Tooba.Promotion.Application.Merchandising.Ports;

using Tooba.Promotion.Application.Merchandising.Models;

namespace Tooba.Promotion.Application.Merchandising.Admin.Commands;

/// <summary>تنظیم قیمت کمپین برای یک عضو.</summary>
public sealed record SetMerchandisingCampaignMemberPriceCommand(
    Guid CampaignId,
    Guid SellerOfferId,
    AdminMerchMemberPriceRequest Body) : IRequest<Result<AdminMerchCampaignDetail>>;

/// <summary>Handler تنظیم قیمت عضو.</summary>
public sealed class SetMerchandisingCampaignMemberPriceCommandHandler(IMerchandisingCampaignAdminComposer c)
    : IRequestHandler<SetMerchandisingCampaignMemberPriceCommand, Result<AdminMerchCampaignDetail>>
{
    /// <inheritdoc />
    public Task<Result<AdminMerchCampaignDetail>> Handle(
        SetMerchandisingCampaignMemberPriceCommand r,
        CancellationToken ct) =>
        MerchandisingAdminResult.ExecuteOrMissingAsync(() => c.SetMemberPriceAsync(r.CampaignId, r.SellerOfferId, r.Body, ct));
}
