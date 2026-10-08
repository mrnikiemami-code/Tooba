using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Promotion.Application.Composition;
using Tooba.Promotion.Application.Ports;
using Tooba.Promotion.Contracts.Errors;

namespace Tooba.Promotion.Application.Commands.ActivateSellerPromotion;

/// <summary>فعال‌سازی پروموشن فروشنده.</summary>
public sealed record ActivateSellerPromotionCommand(
    Guid SellerPartyId,
    Guid PromotionId) : IRequest<Result<PromotionReference>>;

/// <summary>Handler فعال‌سازی پروموشن فروشنده.</summary>
public sealed class ActivateSellerPromotionCommandHandler(IPromotionDirectory promotions)
    : IRequestHandler<ActivateSellerPromotionCommand, Result<PromotionReference>>
{
    /// <inheritdoc />
    public Task<Result<PromotionReference>> Handle(ActivateSellerPromotionCommand r, CancellationToken ct) =>
        PromotionOperation.ExecuteAsync(async () =>
        {
            await promotions.ActivateForSellerAsync(null, r.SellerPartyId, r.PromotionId, ct);
            return await promotions.GetForSellerAsync(null, r.SellerPartyId, r.PromotionId, ct)
                ?? throw new ContractOperationException(PromotionErrorCodes.Missing);
        });
}
