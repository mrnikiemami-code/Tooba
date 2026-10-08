using Tooba.Promotion.Application.Merchandising.Models;

namespace Tooba.Promotion.Application.Merchandising.Ports;

public interface IMerchandisingCampaignAdminComposer
{
    Guid ResolveStoreId();
    Task<AdminMerchCampaignListResponse> ListAsync(string? search, string? lifecycle, Guid? promotionTypeId, string? runtimeWindow, int skip, int take, string? locale, CancellationToken cancellationToken);
    Task<AdminMerchCampaignTypesResponse> ListTypesAsync(string? locale, CancellationToken cancellationToken);
    Task<AdminMerchCampaignDetail?> GetAsync(Guid campaignId, string? locale, CancellationToken cancellationToken);
    Task<AdminMerchCampaignDetail> CreateAsync(AdminMerchCampaignWriteRequest body, CancellationToken cancellationToken);
    Task<AdminMerchCampaignDetail?> UpdateAsync(Guid campaignId, AdminMerchCampaignUpdateRequest body, CancellationToken cancellationToken);
    Task<bool> PublishAsync(Guid campaignId, CancellationToken cancellationToken);
    Task<bool> ArchiveAsync(Guid campaignId, CancellationToken cancellationToken);
    Task<AdminMerchCampaignDetail?> AddMemberAsync(Guid campaignId, Guid sellerOfferId, CancellationToken cancellationToken);
    Task<AdminMerchCampaignDetail?> RemoveMemberAsync(Guid campaignId, Guid sellerOfferId, CancellationToken cancellationToken);
    Task<AdminMerchCampaignDetail?> ReorderMembersAsync(Guid campaignId, IReadOnlyList<Guid> orderedSellerOfferIds, CancellationToken cancellationToken);
    Task<AdminMerchCampaignDetail?> SetMemberPriceAsync(Guid campaignId, Guid sellerOfferId, AdminMerchMemberPriceRequest body, CancellationToken cancellationToken);
    Task<AdminMerchOfferCandidateResponse> ListOfferCandidatesAsync(string? search, int skip, int take, CancellationToken cancellationToken);
}
