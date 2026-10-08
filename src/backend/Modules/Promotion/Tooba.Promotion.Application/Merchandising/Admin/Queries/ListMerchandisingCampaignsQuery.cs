using MediatR;
using Tooba.BuildingBlocks.Results;

using Tooba.Promotion.Application.Merchandising.Ports;

using Tooba.Promotion.Application.Merchandising.Models;

namespace Tooba.Promotion.Application.Merchandising.Admin.Queries;

/// <summary>فهرست Store-scoped کمپین‌ها برای Admin.</summary>
public sealed record ListMerchandisingCampaignsQuery(
    string? Search,
    string? Lifecycle,
    Guid? PromotionTypeId,
    string? RuntimeWindow,
    int Skip,
    int Take,
    string? Locale) : IRequest<Result<AdminMerchCampaignListResponse>>;

/// <summary>Handler فهرست کمپین‌ها.</summary>
public sealed class ListMerchandisingCampaignsQueryHandler(IMerchandisingCampaignAdminComposer c)
    : IRequestHandler<ListMerchandisingCampaignsQuery, Result<AdminMerchCampaignListResponse>>
{
    /// <inheritdoc />
    public async Task<Result<AdminMerchCampaignListResponse>> Handle(
        ListMerchandisingCampaignsQuery r,
        CancellationToken ct) =>
        Result.Success(await c.ListAsync(
            r.Search,
            r.Lifecycle,
            r.PromotionTypeId,
            r.RuntimeWindow,
            r.Skip,
            r.Take,
            r.Locale,
            ct));
}
