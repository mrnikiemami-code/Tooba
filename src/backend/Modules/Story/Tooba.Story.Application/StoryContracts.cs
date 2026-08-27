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

/// <summary>نمای مدیریتی کامل استوری.</summary>
public sealed record AdminStorySnapshot(
    Guid StoryId,
    Guid TenantId,
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

    /// <summary>فهرست مدیریتی استوری‌های Tenant.</summary>
    Task<IReadOnlyList<AdminStorySnapshot>> AdminListAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>جزئیات مدیریتی یک استوری.</summary>
    Task<AdminStorySnapshot?> AdminGetAsync(Guid tenantId, Guid storyId, CancellationToken cancellationToken);

    /// <summary>استوری Draft جدید می‌سازد.</summary>
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
}
