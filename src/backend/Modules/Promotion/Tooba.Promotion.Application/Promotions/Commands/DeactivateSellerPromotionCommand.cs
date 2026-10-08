using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Promotion.Application.Composition;
using Tooba.Promotion.Application.Promotions.Ports;
using Tooba.Promotion.Contracts.Errors;

namespace Tooba.Promotion.Application.Promotions.Commands;

/// <summary>غیرفعال‌سازی پروموشن فروشنده.</summary>
public sealed record DeactivateSellerPromotionCommand(
    Guid SellerPartyId,
    Guid PromotionId) : IRequest<Result<PromotionReference>>;

/// <summary>Handler غیرفعال‌سازی پروموشن فروشنده.</summary>
public sealed class DeactivateSellerPromotionCommandHandler(IPromotionDirectory promotions)
    : IRequestHandler<DeactivateSellerPromotionCommand, Result<PromotionReference>>
{
    /// <inheritdoc />
    public Task<Result<PromotionReference>> Handle(DeactivateSellerPromotionCommand r, CancellationToken ct) =>
        PromotionOperation.ExecuteAsync(async () =>
        {
            await promotions.DeactivateForSellerAsync(null, r.SellerPartyId, r.PromotionId, ct);
            return await promotions.GetForSellerAsync(null, r.SellerPartyId, r.PromotionId, ct)
                ?? throw new ContractOperationException(PromotionErrorCodes.Missing);
        });
}
