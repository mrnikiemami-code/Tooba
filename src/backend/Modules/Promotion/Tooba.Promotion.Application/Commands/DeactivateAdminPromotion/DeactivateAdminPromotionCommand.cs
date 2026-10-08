using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Promotion.Application.Composition;
using Tooba.Promotion.Application.Ports;
using Tooba.Promotion.Contracts.Errors;

namespace Tooba.Promotion.Application.Commands.DeactivateAdminPromotion;

/// <summary>غیرفعال‌سازی نظارتی پروموشن توسط ادمین.</summary>
public sealed record DeactivateAdminPromotionCommand(Guid PromotionId) : IRequest<Result<PromotionReference>>;

/// <summary>Handler غیرفعال‌سازی ادمین.</summary>
public sealed class DeactivateAdminPromotionCommandHandler(IPromotionDirectory promotions)
    : IRequestHandler<DeactivateAdminPromotionCommand, Result<PromotionReference>>
{
    /// <inheritdoc />
    public Task<Result<PromotionReference>> Handle(DeactivateAdminPromotionCommand r, CancellationToken ct) =>
        PromotionOperation.ExecuteAsync(async () =>
        {
            await promotions.DeactivateForAdminAsync(null, r.PromotionId, ct);
            return await promotions.GetForAdminAsync(null, r.PromotionId, ct)
                ?? throw new ContractOperationException(PromotionErrorCodes.Missing);
        });
}
