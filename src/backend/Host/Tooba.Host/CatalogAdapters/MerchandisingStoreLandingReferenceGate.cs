using Tooba.Catalog.Application;
using Tooba.Promotion.Application.Merchandising;

namespace Tooba.Host.CatalogAdapters;

/// <summary>Thin Host adapter: Promotion campaign membership → Catalog Landing reference gate.</summary>
internal sealed class MerchandisingStoreLandingReferenceGate : IStoreLandingExternalReferenceGate
{
    private readonly IMerchandisingCampaignQuery _campaigns;

    public MerchandisingStoreLandingReferenceGate(IMerchandisingCampaignQuery campaigns) => _campaigns = campaigns;

    public Task<bool> CampaignBelongsToStoreAsync(Guid campaignId, Guid storeId, CancellationToken cancellationToken)
        => _campaigns.CampaignBelongsToStoreAsync(campaignId, storeId, cancellationToken);
}
