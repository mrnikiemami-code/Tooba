using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Promotion.Application.Ports;

namespace Tooba.Promotion.Application.Queries.ListAdminPromotions;

/// <summary>فهرست نظارتی پروموشن‌ها؛ فیلتر اختیاری فروشنده.</summary>
public sealed record ListAdminPromotionsQuery(Guid? SellerPartyId) : IRequest<Result<IReadOnlyList<PromotionReference>>>;

/// <summary>Handler فهرست نظارتی پروموشن‌ها.</summary>
public sealed class ListAdminPromotionsQueryHandler(IPromotionDirectory promotions)
    : IRequestHandler<ListAdminPromotionsQuery, Result<IReadOnlyList<PromotionReference>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PromotionReference>>> Handle(
        ListAdminPromotionsQuery r,
        CancellationToken ct) =>
        Result.Success(await promotions.ListForAdminAsync(null, r.SellerPartyId, ct));
}
