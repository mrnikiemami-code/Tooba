using Tooba.Story.Domain;

namespace Tooba.Story.Application;

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

/// <summary>قابلیت خواندن و مدیریت استوری.</summary>
public interface IStoryDirectory
{
    /// <summary>استوری‌های قابل نمایش عمومی را برمی‌گرداند.</summary>
    Task<IReadOnlyList<PublicStoryCard>> GetPublicStoriesAsync(
        Guid tenantId,
        string? locale,
        string? market,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>فهرست مدیریتی استوری‌های Tenant با فیلتر اختیاری بازبینی.</summary>
    Task<IReadOnlyList<AdminStorySnapshot>> AdminListAsync(
        Guid tenantId,
        StoryReviewStatus? reviewStatus,
        CancellationToken cancellationToken);

    /// <summary>فهرست استوری‌های در انتظار بازبینی.</summary>
    Task<IReadOnlyList<AdminStorySnapshot>> AdminListPendingReviewAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    /// <summary>جزئیات مدیریتی یک استوری.</summary>
    Task<AdminStorySnapshot?> AdminGetAsync(Guid tenantId, Guid storyId, CancellationToken cancellationToken);

    /// <summary>استوری Draft ادمین می‌سازد.</summary>
    Task<AdminStorySnapshot> AdminCreateAsync(
        Guid tenantId,
        CreateStoryCommand command,
        CancellationToken cancellationToken);

    /// <summary>فیلدهای استوری را به‌روزرسانی می‌کند.</summary>
    Task<AdminStorySnapshot> AdminUpdateAsync(
        Guid tenantId,
        Guid storyId,
        UpdateStoryCommand command,
        CancellationToken cancellationToken);

    /// <summary>وضعیت استوری را فعال یا غیرفعال می‌کند.</summary>
    Task<AdminStorySnapshot> AdminSetStatusAsync(
        Guid tenantId,
        Guid storyId,
        StoryStatus status,
        CancellationToken cancellationToken);

    /// <summary>زمان‌بندی استوری را تنظیم می‌کند.</summary>
    Task<AdminStorySnapshot> AdminSetScheduleAsync(
        Guid tenantId,
        Guid storyId,
        SetStoryScheduleCommand command,
        CancellationToken cancellationToken);

    /// <summary>ترتیب استوری‌ها را تنظیم می‌کند.</summary>
    Task<IReadOnlyList<AdminStorySnapshot>> AdminReorderStoriesAsync(
        Guid tenantId,
        IReadOnlyList<Guid> storyIdsInOrder,
        CancellationToken cancellationToken);

    /// <summary>آیتم به استوری اضافه می‌کند.</summary>
    Task<AdminStorySnapshot> AdminAddItemAsync(
        Guid tenantId,
        Guid storyId,
        AddStoryItemCommand command,
        CancellationToken cancellationToken);

    /// <summary>آیتم استوری را به‌روزرسانی می‌کند.</summary>
    Task<AdminStorySnapshot> AdminUpdateItemAsync(
        Guid tenantId,
        Guid storyId,
        Guid itemId,
        UpdateStoryItemCommand command,
        CancellationToken cancellationToken);

    /// <summary>آیتم استوری را حذف می‌کند.</summary>
    Task<AdminStorySnapshot> AdminRemoveItemAsync(
        Guid tenantId,
        Guid storyId,
        Guid itemId,
        CancellationToken cancellationToken);

    /// <summary>آیتم‌های استوری را مرتب می‌کند.</summary>
    Task<AdminStorySnapshot> AdminReorderItemsAsync(
        Guid tenantId,
        Guid storyId,
        IReadOnlyList<Guid> itemIdsInOrder,
        CancellationToken cancellationToken);

    /// <summary>استوری را با SoftDisable غیرفعال می‌کند.</summary>
    Task<AdminStorySnapshot> AdminSoftDisableAsync(
        Guid tenantId,
        Guid storyId,
        CancellationToken cancellationToken);

    /// <summary>استوری فروشنده را تأیید می‌کند.</summary>
    Task<AdminStorySnapshot> AdminApproveAsync(
        Guid tenantId,
        Guid storyId,
        Guid adminActorUserId,
        CancellationToken cancellationToken);

    /// <summary>استوری فروشنده را با دلیل رد می‌کند.</summary>
    Task<AdminStorySnapshot> AdminRejectAsync(
        Guid tenantId,
        Guid storyId,
        Guid adminActorUserId,
        string reason,
        CancellationToken cancellationToken);

    /// <summary>فهرست استوری‌های یک فروشنده.</summary>
    Task<IReadOnlyList<AdminStorySnapshot>> SellerListAsync(
        Guid tenantId,
        Guid sellerPartyId,
        CancellationToken cancellationToken);

    /// <summary>جزئیات استوری متعلق به فروشنده.</summary>
    Task<AdminStorySnapshot?> SellerGetAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        CancellationToken cancellationToken);

    /// <summary>پیش‌نویس فروشنده می‌سازد.</summary>
    Task<AdminStorySnapshot> SellerCreateDraftAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid actorUserId,
        CreateStoryCommand command,
        CancellationToken cancellationToken);

    /// <summary>پیش‌نویس/ردشدهٔ فروشنده را به‌روزرسانی می‌کند.</summary>
    Task<AdminStorySnapshot> SellerUpdateAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        UpdateStoryCommand command,
        CancellationToken cancellationToken);

    /// <summary>استوری فروشنده را برای بازبینی ارسال می‌کند.</summary>
    Task<AdminStorySnapshot> SellerSubmitAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>آیتم به استوری قابل‌ویرایش فروشنده اضافه می‌کند.</summary>
    Task<AdminStorySnapshot> SellerAddItemAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        AddStoryItemCommand command,
        CancellationToken cancellationToken);

    /// <summary>آیتم استوری قابل‌ویرایش فروشنده را به‌روزرسانی می‌کند.</summary>
    Task<AdminStorySnapshot> SellerUpdateItemAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        Guid itemId,
        UpdateStoryItemCommand command,
        CancellationToken cancellationToken);

    /// <summary>آیتم استوری قابل‌ویرایش فروشنده را حذف می‌کند.</summary>
    Task<AdminStorySnapshot> SellerRemoveItemAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        Guid itemId,
        CancellationToken cancellationToken);

    /// <summary>آیتم‌های استوری قابل‌ویرایش فروشنده را مرتب می‌کند.</summary>
    Task<AdminStorySnapshot> SellerReorderItemsAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        IReadOnlyList<Guid> itemIdsInOrder,
        CancellationToken cancellationToken);
}
