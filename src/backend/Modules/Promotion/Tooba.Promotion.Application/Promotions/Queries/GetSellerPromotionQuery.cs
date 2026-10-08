using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Promotion.Application.Promotions.Ports;
using Tooba.Promotion.Contracts.Errors;

namespace Tooba.Promotion.Application.Promotions.Queries;

/// <summary>جزئیات پروموشن متعلق به فروشنده.</summary>
public sealed record GetSellerPromotionQuery(
    Guid SellerPartyId,
    Guid PromotionId) : IRequest<Result<PromotionReference>>;

/// <summary>Handler جزئیات پروموشن فروشنده.</summary>
public sealed class GetSellerPromotionQueryHandler(IPromotionDirectory promotions)
    : IRequestHandler<GetSellerPromotionQuery, Result<PromotionReference>>
{
    /// <inheritdoc />
    public async Task<Result<PromotionReference>> Handle(GetSellerPromotionQuery r, CancellationToken ct)
    {
        var row = await promotions.GetForSellerAsync(null, r.SellerPartyId, r.PromotionId, ct);
        return row is null
            ? Result.Failure<PromotionReference>(new SemanticError(PromotionErrorCodes.Missing))
            : Result.Success(row);
    }
}
