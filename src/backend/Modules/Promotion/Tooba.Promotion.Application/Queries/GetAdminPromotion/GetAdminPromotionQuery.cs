using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Promotion.Application.Ports;
using Tooba.Promotion.Contracts.Errors;

namespace Tooba.Promotion.Application.Queries.GetAdminPromotion;

/// <summary>جزئیات نظارتی پروموشن.</summary>
public sealed record GetAdminPromotionQuery(Guid PromotionId) : IRequest<Result<PromotionReference>>;

/// <summary>Handler جزئیات نظارتی پروموشن.</summary>
public sealed class GetAdminPromotionQueryHandler(IPromotionDirectory promotions)
    : IRequestHandler<GetAdminPromotionQuery, Result<PromotionReference>>
{
    /// <inheritdoc />
    public async Task<Result<PromotionReference>> Handle(GetAdminPromotionQuery r, CancellationToken ct)
    {
        var row = await promotions.GetForAdminAsync(null, r.PromotionId, ct);
        return row is null
            ? Result.Failure<PromotionReference>(new SemanticError(PromotionErrorCodes.Missing))
            : Result.Success(row);
    }
}
