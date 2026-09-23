using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Sellers.Models;
using Tooba.Order.Application.Admin.Sellers.Ports;

namespace Tooba.Order.Application.Admin.Sellers.Queries.GetSellerOrderCounts;

/// <summary>شمارش سفارش به ازای فهرست SellerPartyId.</summary>
public sealed record GetSellerOrderCountsQuery(IReadOnlyList<Guid> SellerPartyIds)
    : IRequest<Result<SellerOrderCountMap>>;

/// <summary>شمارش را از مرز Order می‌خواند.</summary>
public sealed class GetSellerOrderCountsHandler(ISellerOrderCountReader reader)
    : IRequestHandler<GetSellerOrderCountsQuery, Result<SellerOrderCountMap>>
{
    public async Task<Result<SellerOrderCountMap>> Handle(
        GetSellerOrderCountsQuery request,
        CancellationToken cancellationToken)
    {
        var ids = request.SellerPartyIds ?? [];
        var counts = await reader.GetCountsBySellerAsync(ids, cancellationToken);
        return Result.Success(new SellerOrderCountMap(counts));
    }
}
