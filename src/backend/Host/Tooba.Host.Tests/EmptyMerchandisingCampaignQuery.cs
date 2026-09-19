using Tooba.Promotion.Application;

namespace Tooba.Host.Tests;

/// <summary>Stub for Landing composer unit tests that do not exercise campaigns.</summary>
internal sealed class EmptyMerchandisingCampaignQuery : IMerchandisingCampaignQuery
{
    public Task<MerchandisingCampaignRuntimeModel?> ResolveActiveByTypeAsync(
        Guid storeId,
        string typeCode,
        string locale,
        DateTimeOffset now,
        int take,
        MerchandisingPriceScope? priceScope,
        CancellationToken cancellationToken) =>
        Task.FromResult<MerchandisingCampaignRuntimeModel?>(null);

    public Task<MerchandisingCampaignRuntimeModel?> ResolveFutureByTypeAsync(
        Guid storeId,
        string typeCode,
        string locale,
        DateTimeOffset now,
        int take,
        MerchandisingPriceScope? priceScope,
        CancellationToken cancellationToken) =>
        Task.FromResult<MerchandisingCampaignRuntimeModel?>(null);

    public Task<IReadOnlyList<MerchandisingCampaignMemberRuntimeModel>> ResolveCampaignMembersAsync(
        Guid campaignId,
        Guid storeId,
        string locale,
        DateTimeOffset now,
        int take,
        MerchandisingPriceScope? priceScope,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MerchandisingCampaignMemberRuntimeModel>>(Array.Empty<MerchandisingCampaignMemberRuntimeModel>());

    public Task<bool> CampaignBelongsToStoreAsync(
        Guid campaignId,
        Guid storeId,
        CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
