namespace Tooba.Promotion.Application.Merchandising;

#pragma warning disable CS1591

public sealed record AdminMerchCampaignListItem(
    Guid CampaignId,
    string Title,
    string PromotionTypeDisplayName,
    string LifecycleStatus,
    string RuntimeLabel,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int Priority,
    int MemberCount,
    DateTimeOffset UpdatedAt);

public sealed record AdminMerchCampaignListResponse(IReadOnlyList<AdminMerchCampaignListItem> Items, int Total);

public sealed record AdminMerchCampaignTypeOption(Guid PromotionTypeId, string DisplayName);

public sealed record AdminMerchCampaignTypesResponse(IReadOnlyList<AdminMerchCampaignTypeOption> Items);

public sealed record AdminMerchCampaignTranslationDto(
    string Locale,
    string Title,
    string? Subtitle,
    string? BadgeText);

public sealed record AdminMerchCampaignMemberDto(
    Guid SellerOfferId,
    int SortOrder,
    string ProductTitle,
    string SellerDisplayName,
    decimal BaseAmount,
    decimal? CampaignAmount,
    string Currency,
    decimal AvailableUnits,
    bool InStock);

public sealed record AdminMerchCampaignDetail(
    Guid CampaignId,
    Guid PromotionTypeId,
    string PromotionTypeDisplayName,
    string LifecycleStatus,
    string RuntimeLabel,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int Priority,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<AdminMerchCampaignTranslationDto> Translations,
    IReadOnlyList<AdminMerchCampaignMemberDto> Members);

public sealed record AdminMerchCampaignWriteRequest(
    Guid PromotionTypeId,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int Priority,
    IReadOnlyList<AdminMerchCampaignTranslationDto> Translations);

public sealed record AdminMerchCampaignUpdateRequest(
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int Priority,
    IReadOnlyList<AdminMerchCampaignTranslationDto> Translations);

public sealed record AdminMerchAddMemberRequest(Guid SellerOfferId);

public sealed record AdminMerchReorderMembersRequest(IReadOnlyList<Guid> OrderedSellerOfferIds);

public sealed record AdminMerchMemberPriceRequest(
    decimal Amount,
    string? Currency,
    string? Market,
    string? Channel);

public sealed record AdminMerchOfferCandidate(
    Guid SellerOfferId,
    string ProductTitle,
    string SellerDisplayName,
    decimal BaseAmount,
    string Currency,
    decimal AvailableUnits,
    bool InStock);

public sealed record AdminMerchOfferCandidateResponse(IReadOnlyList<AdminMerchOfferCandidate> Items, int Total);
