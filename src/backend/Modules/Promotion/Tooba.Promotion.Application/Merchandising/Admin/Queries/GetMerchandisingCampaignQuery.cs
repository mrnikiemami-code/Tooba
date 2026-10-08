using MediatR;
using Tooba.BuildingBlocks.Results;

using Tooba.Promotion.Application.Merchandising.Ports;

using Tooba.Promotion.Application.Merchandising.Models;

namespace Tooba.Promotion.Application.Merchandising.Admin.Queries;

/// <summary>جزئیات یک کمپین Store-scoped.</summary>
public sealed record GetMerchandisingCampaignQuery(
    Guid CampaignId,
    string? Locale) : IRequest<Result<AdminMerchCampaignDetail>>;

/// <summary>Handler جزئیات کمپین.</summary>
public sealed class GetMerchandisingCampaignQueryHandler(IMerchandisingCampaignAdminComposer c)
    : IRequestHandler<GetMerchandisingCampaignQuery, Result<AdminMerchCampaignDetail>>
{
    /// <inheritdoc />
    public Task<Result<AdminMerchCampaignDetail>> Handle(GetMerchandisingCampaignQuery r, CancellationToken ct) =>
        MerchandisingAdminResult.ExecuteOrMissingAsync(() => c.GetAsync(r.CampaignId, r.Locale, ct));
}
