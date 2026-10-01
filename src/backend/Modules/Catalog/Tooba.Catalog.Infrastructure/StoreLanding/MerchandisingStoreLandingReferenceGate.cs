using Tooba.Catalog.Application;
using Tooba.Promotion.Contracts.Merchandising;

namespace Tooba.Catalog.Infrastructure.StoreLanding;

/// <summary>Thin Catalog adapter: Promotion campaign membership → Catalog Landing reference gate.</summary>
internal sealed class MerchandisingStoreLandingReferenceGate(IMerchandisingCampaignQuery campaigns)
    : IStoreLandingExternalReferenceGate
{
    public Task<bool> CampaignBelongsToStoreAsync(Guid campaignId, Guid storeId, CancellationToken cancellationToken)
        => campaigns.CampaignBelongsToStoreAsync(campaignId, storeId, cancellationToken);
}
