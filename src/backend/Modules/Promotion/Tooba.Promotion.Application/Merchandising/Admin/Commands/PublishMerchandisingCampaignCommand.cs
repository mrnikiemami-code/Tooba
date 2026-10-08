using MediatR;
using Tooba.BuildingBlocks.Results;

using Tooba.Promotion.Application.Merchandising.Ports;

namespace Tooba.Promotion.Application.Merchandising.Admin.Commands;

/// <summary>انتشار کمپین.</summary>
public sealed record PublishMerchandisingCampaignCommand(Guid CampaignId) : IRequest<Result>;

/// <summary>Handler انتشار کمپین.</summary>
public sealed class PublishMerchandisingCampaignCommandHandler(IMerchandisingCampaignAdminComposer c)
    : IRequestHandler<PublishMerchandisingCampaignCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(PublishMerchandisingCampaignCommand r, CancellationToken ct) =>
        MerchandisingAdminResult.ExecuteFlagAsync(() => c.PublishAsync(r.CampaignId, ct));
}
