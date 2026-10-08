using MediatR;
using Tooba.BuildingBlocks.Results;

using Tooba.Promotion.Application.Merchandising.Ports;

using Tooba.Promotion.Application.Merchandising.Models;

namespace Tooba.Promotion.Application.Merchandising.Admin.Queries;

/// <summary>گونه‌های فعال کمپین برای انتخاب Admin.</summary>
public sealed record ListMerchandisingCampaignTypesQuery(string? Locale) : IRequest<Result<AdminMerchCampaignTypesResponse>>;

/// <summary>Handler گونه‌های کمپین.</summary>
public sealed class ListMerchandisingCampaignTypesQueryHandler(IMerchandisingCampaignAdminComposer c)
    : IRequestHandler<ListMerchandisingCampaignTypesQuery, Result<AdminMerchCampaignTypesResponse>>
{
    /// <inheritdoc />
    public async Task<Result<AdminMerchCampaignTypesResponse>> Handle(
        ListMerchandisingCampaignTypesQuery r,
        CancellationToken ct) =>
        Result.Success(await c.ListTypesAsync(r.Locale, ct));
}
