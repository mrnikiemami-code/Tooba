using Tooba.Catalog.Application;
using Tooba.Promotion.Application;

namespace Tooba.Host.Admin;

/// <summary>آداپتر Host برای عضویت کمپین در اعتبارسنجی بخش Landing.</summary>
internal sealed class MerchandisingStoreLandingReferenceGate : IStoreLandingExternalReferenceGate
{
    private readonly IMerchandisingCampaignQuery _campaigns;

    public MerchandisingStoreLandingReferenceGate(IMerchandisingCampaignQuery campaigns) => _campaigns = campaigns;

    /// <inheritdoc />
    public Task<bool> CampaignBelongsToStoreAsync(Guid campaignId, Guid storeId, CancellationToken cancellationToken)
        => _campaigns.CampaignBelongsToStoreAsync(campaignId, storeId, cancellationToken);
}
