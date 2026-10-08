using MediatR;
using Tooba.BuildingBlocks.Results;

using Tooba.Promotion.Application.Merchandising.Ports;

using Tooba.Promotion.Application.Merchandising.Models;

namespace Tooba.Promotion.Application.Merchandising.Admin.Queries;

/// <summary>Offerهای نامزد عضویت کمپین.</summary>
public sealed record ListMerchandisingOfferCandidatesQuery(
    string? Search,
    int Skip,
    int Take) : IRequest<Result<AdminMerchOfferCandidateResponse>>;

/// <summary>Handler نامزدهای Offer.</summary>
public sealed class ListMerchandisingOfferCandidatesQueryHandler(IMerchandisingCampaignAdminComposer c)
    : IRequestHandler<ListMerchandisingOfferCandidatesQuery, Result<AdminMerchOfferCandidateResponse>>
{
    /// <inheritdoc />
    public async Task<Result<AdminMerchOfferCandidateResponse>> Handle(
        ListMerchandisingOfferCandidatesQuery r,
        CancellationToken ct) =>
        Result.Success(await c.ListOfferCandidatesAsync(r.Search, r.Skip, r.Take, ct));
}
