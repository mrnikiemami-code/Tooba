using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Promotion.Application.Composition;
using Tooba.Promotion.Application.Promotions.Models;
using Tooba.Promotion.Application.Promotions.Ports;

namespace Tooba.Promotion.Application.Promotions.Commands;

/// <summary>ساخت پروموشن پیش‌نویس فروشنده.</summary>
public sealed record CreateSellerPromotionCommand(
    Guid SellerPartyId,
    PromotionMutationInput Input) : IRequest<Result<PromotionReference>>;

/// <summary>Handler ساخت پروموشن فروشنده.</summary>
public sealed class CreateSellerPromotionCommandHandler(IPromotionDirectory promotions, IClock clock)
    : IRequestHandler<CreateSellerPromotionCommand, Result<PromotionReference>>
{
    /// <inheritdoc />
    public Task<Result<PromotionReference>> Handle(CreateSellerPromotionCommand r, CancellationToken ct) =>
        PromotionOperation.ExecuteAsync(async () =>
        {
            var p = PromotionMutationNormalizer.Normalize(r.Input, clock);
            return await promotions.CreateForSellerAsync(
                null,
                r.SellerPartyId,
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
