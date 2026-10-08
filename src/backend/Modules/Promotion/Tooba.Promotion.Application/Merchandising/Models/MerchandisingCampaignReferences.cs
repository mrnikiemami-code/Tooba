using Tooba.Promotion.Domain.Merchandising;

namespace Tooba.Promotion.Application.Merchandising.Models;

/// <summary>
/// مرجع گونهٔ مرچندایزینگ.
/// </summary>
public sealed record MerchandisingPromotionTypeReference(
    Guid Id,
    string Code,
    bool IsSystem,
    bool IsActive,
    int SortOrder);

/// <summary>
/// مرجع کمپین مرچندایزینگ.
/// </summary>
public sealed record MerchandisingCampaignReference(
    Guid Id,
    Guid PromotionTypeId,
    Guid StoreId,
    MerchandisingCampaignLifecycleStatus LifecycleStatus,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int Priority,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// ترجمهٔ کمپین.
/// </summary>
public sealed record MerchandisingCampaignTranslationReference(
    Guid CampaignId,
    string Locale,
    string Title,
    string? Subtitle,
    string? BadgeText);

/// <summary>
/// عضو مرتب‌شدهٔ کمپین.
/// </summary>
public sealed record MerchandisingCampaignMemberReference(
    Guid MembershipId,
    Guid CampaignId,
    Guid SellerOfferId,
    int SortOrder,
    DateTimeOffset CreatedAt);

/// <summary>
/// ردیف فهرست Admin کمپین.
/// </summary>
public sealed record MerchandisingCampaignListRow(
    Guid Id,
    Guid PromotionTypeId,
    string PromotionTypeDisplayName,
    MerchandisingCampaignLifecycleStatus LifecycleStatus,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int Priority,
    int MemberCount,
    string? Title,
    DateTimeOffset UpdatedAt,
    string RuntimeLabel);

/// <summary>
/// گزینهٔ گونه برای انتخاب Admin (DisplayName محور).
/// </summary>
public sealed record MerchandisingPromotionTypeOption(
    Guid Id,
    string DisplayName,
    string Code,
    bool IsSystem);
