using MediatR;
using Tooba.BuildingBlocks.Results;

using Tooba.Promotion.Application.Merchandising.Ports;

using Tooba.Promotion.Application.Merchandising.Models;

namespace Tooba.Promotion.Application.Merchandising.Admin.Commands;

/// <summary>به‌روزرسانی پنجره/اولویت/ترجمه‌های کمپین.</summary>
public sealed record UpdateMerchandisingCampaignCommand(
    Guid CampaignId,
    AdminMerchCampaignUpdateRequest Body) : IRequest<Result<AdminMerchCampaignDetail>>;

/// <summary>Handler به‌روزرسانی کمپین.</summary>
public sealed class UpdateMerchandisingCampaignCommandHandler(IMerchandisingCampaignAdminComposer c)
    : IRequestHandler<UpdateMerchandisingCampaignCommand, Result<AdminMerchCampaignDetail>>
{
    /// <inheritdoc />
    public Task<Result<AdminMerchCampaignDetail>> Handle(UpdateMerchandisingCampaignCommand r, CancellationToken ct) =>
        MerchandisingAdminResult.ExecuteOrMissingAsync(() => c.UpdateAsync(r.CampaignId, r.Body, ct));
}
