using MediatR;
using Tooba.BuildingBlocks.Results;

using Tooba.Promotion.Application.Merchandising.Ports;

namespace Tooba.Promotion.Application.Merchandising.Admin.Commands;

/// <summary>بایگانی کمپین.</summary>
public sealed record ArchiveMerchandisingCampaignCommand(Guid CampaignId) : IRequest<Result>;

/// <summary>Handler بایگانی کمپین.</summary>
public sealed class ArchiveMerchandisingCampaignCommandHandler(IMerchandisingCampaignAdminComposer c)
    : IRequestHandler<ArchiveMerchandisingCampaignCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ArchiveMerchandisingCampaignCommand r, CancellationToken ct) =>
        MerchandisingAdminResult.ExecuteFlagAsync(() => c.ArchiveAsync(r.CampaignId, ct));
}
