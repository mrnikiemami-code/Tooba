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
    string? CtaTarget);

/// <summary>کارت عمومی استوری در ریل.</summary>
public sealed record PublicStoryCard(
    Guid StoryId,
    string Title,
    string? CoverMediaUrl,
    bool IsVideo,
    int DisplayOrder,
    string CtaType,
    string? CtaTarget,
    IReadOnlyList<PublicStoryItem> Items);

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
    DateTimeOffset UpdatedAt);

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
    IReadOnlyList<AdminStoryItemSnapshot> Items);

/// <summary>فرمان ایجاد استوری.</summary>
public sealed record CreateStoryCommand(
    string Title,
    string? Locale,
    string? Market,
    Guid? CoverMediaAssetId,
    string? CoverMediaUrl,
    int? DisplayOrder,
    string? CtaType,
    string? CtaTarget);

/// <summary>فرمان به‌روزرسانی استوری.</summary>
public sealed record UpdateStoryCommand(
    string Title,
    string? Locale,
    string? Market,
    Guid? CoverMediaAssetId,
    string? CoverMediaUrl,
    string? CtaType,
    string? CtaTarget);

/// <summary>فرمان زمان‌بندی استوری.</summary>
public sealed record SetStoryScheduleCommand(
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt);

/// <summary>فرمان افزودن آیتم.</summary>
public sealed record AddStoryItemCommand(
    string MediaType,
    Guid? MediaAssetId,
    string? MediaUrl,
    string? Caption,
    int? DurationMs,
    string? CtaType,
    string? CtaTarget,
    int? DisplayOrder);

/// <summary>فرمان به‌روزرسانی آیتم.</summary>
public sealed record UpdateStoryItemCommand(
    string MediaType,
    Guid? MediaAssetId,
    string? MediaUrl,
    string? Caption,
    int? DurationMs,
    string? CtaType,
    string? CtaTarget);
