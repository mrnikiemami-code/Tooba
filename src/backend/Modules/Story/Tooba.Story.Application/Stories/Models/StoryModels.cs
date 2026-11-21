using Tooba.Story.Domain.Enums;

namespace Tooba.Story.Application.Stories.Models;

/// <summary>آیتم عمومی استوری برای storefront.</summary>
public sealed record PublicStoryItem(
    Guid StoryItemId,
    string MediaType,
    string? MediaUrl,
    string? Caption,
    int? DurationMs,
    string CtaType,
    string? CtaTarget,
    string? CtaHref = null);

/// <summary>کارت عمومی استوری در ریل.</summary>
public sealed record PublicStoryCard(
    Guid StoryId,
    string Title,
    string? CoverMediaUrl,
    bool IsVideo,
    int DisplayOrder,
    string CtaType,
    string? CtaTarget,
    IReadOnlyList<PublicStoryItem> Items,
    Guid? SellerPartyId = null,
    string? ShopDisplayName = null,
    string? ShopPublicId = null,
    string? CtaHref = null,
    /// <summary>پایان پنجرهٔ انتشار؛ برای اعتبارسنجی مجدد محدود در سمت مشتری.</summary>
    DateTimeOffset? EndAt = null);

/// <summary>آیتم مدیریتی استوری.</summary>
public sealed record AdminStoryItemSnapshot(
    Guid StoryItemId,
    int DisplayOrder,
    string MediaType,
    Guid? MediaAssetId,
    string? MediaUrl,
    string? Caption,
    int? DurationMs,
    string CtaType,
    string? CtaTarget,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    /// <summary>عنوان محصول مالک برای CTA محصول؛ null یعنی ناموجود/غیرمجاز در scope فعلی.</summary>
    string? CtaProductTitle = null,
    string? CtaHref = null);

/// <summary>نمای مدیریتی/فروشنده کامل استوری.</summary>
public sealed record AdminStorySnapshot(
    Guid StoryId,
    Guid TenantId,
    StoryOrigin Origin,
    Guid? SellerPartyId,
    StoryReviewStatus ReviewStatus,
    Guid? SubmittedByActorUserId,
    Guid? ReviewedByActorUserId,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason,
    string? Locale,
    string? Market,
    string Title,
    Guid? CoverMediaAssetId,
    string? CoverMediaUrl,
    int DisplayOrder,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    StoryStatus Status,
    string CtaType,
    string? CtaTarget,
    int VersionToken,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<AdminStoryItemSnapshot> Items,
    Guid? CreatedByActorUserId = null,
    string? ShopDisplayName = null,
    bool ContentReady = false,
    bool EffectivelyPublic = false,
    IReadOnlyList<string>? ActionCodes = null);

/// <summary>فرمان ایجاد استوری.</summary>
public sealed record CreateStoryCommand(
    string Title,
    string? Locale,
    string? Market,
    Guid? CoverMediaAssetId,
    string? CoverMediaUrl,
    int? DisplayOrder,
    string? CtaType,
    string? CtaTarget,
    Guid? SellerPartyId = null,
    string? OccurrenceKey = null,
    int? ExpectedVersion = null,
    DateTimeOffset? StartAt = null,
    DateTimeOffset? EndAt = null);

/// <summary>فرمان به‌روزرسانی استوری.</summary>
public sealed record UpdateStoryCommand(
    string Title,
    string? Locale,
    string? Market,
    Guid? CoverMediaAssetId,
    string? CoverMediaUrl,
    string? CtaType,
    string? CtaTarget,
    int? ExpectedVersion = null,
    string? OccurrenceKey = null,
    DateTimeOffset? StartAt = null,
    DateTimeOffset? EndAt = null);

/// <summary>فرمان زمان‌بندی استوری.</summary>
public sealed record SetStoryScheduleCommand(
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    int? ExpectedVersion = null,
    string? OccurrenceKey = null);

/// <summary>فرمان افزودن آیتم.</summary>
public sealed record AddStoryItemCommand(
    string MediaType,
    Guid? MediaAssetId,
    string? MediaUrl,
    string? Caption,
    int? DurationMs,
    string? CtaType,
    string? CtaTarget,
    int? DisplayOrder,
    string? OccurrenceKey = null,
    int? ExpectedVersion = null);

/// <summary>فرمان به‌روزرسانی آیتم.</summary>
public sealed record UpdateStoryItemCommand(
    string MediaType,
    Guid? MediaAssetId,
    string? MediaUrl,
    string? Caption,
    int? DurationMs,
    string? CtaType,
    string? CtaTarget,
    int? ExpectedVersion = null,
    string? OccurrenceKey = null);
