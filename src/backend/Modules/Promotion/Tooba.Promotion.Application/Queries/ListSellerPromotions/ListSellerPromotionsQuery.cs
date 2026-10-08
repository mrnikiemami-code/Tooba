using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Promotion.Application.Ports;

namespace Tooba.Promotion.Application.Queries.ListSellerPromotions;

/// <summary>فهرست پروموشن‌های فروشنده.</summary>
public sealed record ListSellerPromotionsQuery(Guid SellerPartyId) : IRequest<Result<IReadOnlyList<PromotionReference>>>;

/// <summary>Handler فهرست پروموشن‌های فروشنده.</summary>
public sealed class ListSellerPromotionsQueryHandler(IPromotionDirectory promotions)
    : IRequestHandler<ListSellerPromotionsQuery, Result<IReadOnlyList<PromotionReference>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PromotionReference>>> Handle(
        ListSellerPromotionsQuery request,
        CancellationToken ct) =>
        Result.Success(await promotions.ListBySellerAsync(null, request.SellerPartyId, ct));
}
