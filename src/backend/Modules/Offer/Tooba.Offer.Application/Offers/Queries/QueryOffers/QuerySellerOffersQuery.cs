using MediatR;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Dtos;

#pragma warning disable CS1591
namespace Tooba.Offer.Application.Offers.Queries.QueryOffers;

public sealed record QuerySellerOffersQuery(Guid SellerPartyId, GridQueryRequest Request)
    : IRequest<Result<GridPageResponse<SellerOfferListItem>>>;

public sealed record QueryAdminOffersQuery(GridQueryRequest Request)
    : IRequest<Result<GridPageResponse<SellerOfferListItem>>>;

public sealed class QuerySellerOffersHandler(IOfferListQueryPort query, OfferReadModelComposer readModels)
    : IRequestHandler<QuerySellerOffersQuery, Result<GridPageResponse<SellerOfferListItem>>>
{
    public async Task<Result<GridPageResponse<SellerOfferListItem>>> Handle(
        QuerySellerOffersQuery request,
        CancellationToken cancellationToken)
    {
        var result = await query.QueryAsync(Normalize(request.Request, request.SellerPartyId), cancellationToken);
        var items = await readModels.ListAsync(result.Items, cancellationToken);
        return Result.Success(new GridPageResponse<SellerOfferListItem>(items, result.Page, result.PageSize, result.TotalCount));
    }

    internal static OfferListCriteria Normalize(GridQueryRequest request, Guid? sellerPartyId)
    {
        var (page, pageSize) = GridQueryPolicyBase.NormalizePaging(request.Page, request.PageSize, 100, 20);
        var statusFilter = (request.Filters ?? []).FirstOrDefault(filter =>
            string.Equals(filter.Field, "status", StringComparison.OrdinalIgnoreCase));
        var status = statusFilter?.Value ?? statusFilter?.Values?.FirstOrDefault();
        var sort = (request.Sort ?? []).FirstOrDefault();
        return new OfferListCriteria(
            sellerPartyId,
            GridQueryPolicyBase.NormalizeSearch(request.Search),
            string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            page,
            pageSize,
            sort?.Field,
            string.Equals(sort?.Direction, "desc", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class QueryAdminOffersHandler(IOfferListQueryPort query, OfferReadModelComposer readModels)
    : IRequestHandler<QueryAdminOffersQuery, Result<GridPageResponse<SellerOfferListItem>>>
{
    public async Task<Result<GridPageResponse<SellerOfferListItem>>> Handle(
        QueryAdminOffersQuery request,
        CancellationToken cancellationToken)
    {
        var result = await query.QueryAsync(QuerySellerOffersHandler.Normalize(request.Request, null), cancellationToken);
        var items = await readModels.ListAsync(result.Items, cancellationToken);
        return Result.Success(new GridPageResponse<SellerOfferListItem>(items, result.Page, result.PageSize, result.TotalCount));
    }
}


