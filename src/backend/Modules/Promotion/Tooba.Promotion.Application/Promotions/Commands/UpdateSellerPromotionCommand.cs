using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Promotion.Application.Composition;
using Tooba.Promotion.Application.Promotions.Models;
using Tooba.Promotion.Application.Promotions.Ports;

namespace Tooba.Promotion.Application.Promotions.Commands;

/// <summary>به‌روزرسانی پروموشن فروشنده.</summary>
public sealed record UpdateSellerPromotionCommand(
    Guid SellerPartyId,
    Guid PromotionId,
    PromotionMutationInput Input) : IRequest<Result<PromotionReference>>;

/// <summary>Handler به‌روزرسانی پروموشن فروشنده.</summary>
public sealed class UpdateSellerPromotionCommandHandler(IPromotionDirectory promotions, IClock clock)
    : IRequestHandler<UpdateSellerPromotionCommand, Result<PromotionReference>>
{
    /// <inheritdoc />
    public Task<Result<PromotionReference>> Handle(UpdateSellerPromotionCommand r, CancellationToken ct) =>
        PromotionOperation.ExecuteAsync(async () =>
        {
            var p = PromotionMutationNormalizer.Normalize(r.Input, clock);
            return await promotions.UpdateForSellerAsync(
                null,
                r.SellerPartyId,
                r.PromotionId,
                p.Name,
                p.EffectiveFrom,
                p.EffectiveTo,
                p.DiscountKind,
                p.PercentageRate,
                p.FixedAmount,
                p.FixedAmountCurrency,
                p.CouponCode,
                p.MinimumSubtotal,
                ct);
        });
}
