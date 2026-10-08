using MediatR;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Promotion.Application.Merchandising.Admin.Commands;

/// <summary>ساخت کمپین پیش‌نویس.</summary>
public sealed record CreateMerchandisingCampaignCommand(AdminMerchCampaignWriteRequest Body)
    : IRequest<Result<AdminMerchCampaignDetail>>;

/// <summary>Handler ساخت کمپین.</summary>
public sealed class CreateMerchandisingCampaignCommandHandler(IMerchandisingCampaignAdminComposer c)
    : IRequestHandler<CreateMerchandisingCampaignCommand, Result<AdminMerchCampaignDetail>>
{
    /// <inheritdoc />
    public Task<Result<AdminMerchCampaignDetail>> Handle(CreateMerchandisingCampaignCommand r, CancellationToken ct) =>
        Composition.PromotionOperation.ExecuteAsync(() => c.CreateAsync(r.Body, ct));
}
