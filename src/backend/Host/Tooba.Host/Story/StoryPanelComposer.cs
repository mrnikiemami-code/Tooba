using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using global::Tooba.Story.Application;
using global::Tooba.Story.Domain;

namespace Tooba.Host.Story;

/// <summary>ترکیب HTTP برای مسیرهای عمومی و مدیریتی Story.</summary>
public sealed class StoryPanelComposer
{
    private readonly IStoryDirectory _stories;

    /// <summary>دایرکتوری Story را تزریق می‌کند.</summary>
    public StoryPanelComposer(IStoryDirectory stories) => _stories = stories;

    /// <summary>استوری‌های عمومی.</summary>
    public Task<IReadOnlyList<PublicStoryCard>> GetPublicStoriesAsync(
        Guid tenantId,
        string? locale,
        string? market,
        CancellationToken cancellationToken) =>
        _stories.GetPublicStoriesAsync(tenantId, locale, market, DateTimeOffset.UtcNow, cancellationToken);

    /// <summary>فهرست مدیریتی.</summary>
    public Task<IReadOnlyList<AdminStorySnapshot>> AdminListAsync(Guid tenantId, CancellationToken cancellationToken) =>
        _stories.AdminListAsync(tenantId, cancellationToken);

    /// <summary>جزئیات مدیریتی.</summary>
    public Task<AdminStorySnapshot?> AdminGetAsync(Guid tenantId, Guid storyId, CancellationToken cancellationToken) =>
        _stories.AdminGetAsync(tenantId, storyId, cancellationToken);

    /// <summary>ایجاد استوری.</summary>
    public Task<AdminStorySnapshot> AdminCreateAsync(
        Guid tenantId,
        CreateStoryBody body,
        CancellationToken cancellationToken) =>
        _stories.AdminCreateAsync(
            tenantId,
            new CreateStoryCommand(
                body.Title,
                body.Locale,
                body.Market,
                body.CoverMediaAssetId,
                body.CoverMediaUrl,
                body.DisplayOrder,
                body.CtaType,
                body.CtaTarget),
            cancellationToken);

    /// <summary>به‌روزرسانی استوری.</summary>
    public Task<AdminStorySnapshot> AdminUpdateAsync(
        Guid tenantId,
        Guid storyId,
        UpdateStoryBody body,
        CancellationToken cancellationToken) =>
        _stories.AdminUpdateAsync(
            tenantId,
            storyId,
            new UpdateStoryCommand(
                body.Title,
                body.Locale,
                body.Market,
                body.CoverMediaAssetId,
                body.CoverMediaUrl,
                body.CtaType,
                body.CtaTarget),
            cancellationToken);

    /// <summary>فعال‌سازی.</summary>
    public Task<AdminStorySnapshot> AdminEnableAsync(Guid tenantId, Guid storyId, CancellationToken cancellationToken) =>
        _stories.AdminSetStatusAsync(tenantId, storyId, StoryStatus.Active, cancellationToken);

    /// <summary>غیرفعال‌سازی.</summary>
    public Task<AdminStorySnapshot> AdminDisableAsync(Guid tenantId, Guid storyId, CancellationToken cancellationToken) =>
        _stories.AdminSoftDisableAsync(tenantId, storyId, cancellationToken);

    /// <summary>زمان‌بندی.</summary>
    public Task<AdminStorySnapshot> AdminSetScheduleAsync(
        Guid tenantId,
        Guid storyId,
        SetStoryScheduleBody body,
        CancellationToken cancellationToken) =>
        _stories.AdminSetScheduleAsync(
            tenantId,
            storyId,
            new SetStoryScheduleCommand(body.StartAt, body.EndAt),
            cancellationToken);

    /// <summary>مرتب‌سازی استوری‌ها.</summary>
    public Task<IReadOnlyList<AdminStorySnapshot>> AdminReorderStoriesAsync(
        Guid tenantId,
        IReadOnlyList<Guid> storyIds,
        CancellationToken cancellationToken) =>
        _stories.AdminReorderStoriesAsync(tenantId, storyIds, cancellationToken);

    /// <summary>افزودن آیتم.</summary>
    public Task<AdminStorySnapshot> AdminAddItemAsync(
        Guid tenantId,
        Guid storyId,
        AddStoryItemBody body,
        CancellationToken cancellationToken) =>
        _stories.AdminAddItemAsync(
            tenantId,
            storyId,
            new AddStoryItemCommand(
                body.MediaType,
                body.MediaAssetId,
                body.MediaUrl,
                body.Caption,
                body.DurationMs,
                body.CtaType,
                body.CtaTarget,
                body.DisplayOrder),
            cancellationToken);

    /// <summary>به‌روزرسانی آیتم.</summary>
    public Task<AdminStorySnapshot> AdminUpdateItemAsync(
        Guid tenantId,
        Guid storyId,
        Guid itemId,
        UpdateStoryItemBody body,
        CancellationToken cancellationToken) =>
        _stories.AdminUpdateItemAsync(
            tenantId,
            storyId,
            itemId,
            new UpdateStoryItemCommand(
                body.MediaType,
                body.MediaAssetId,
                body.MediaUrl,
                body.Caption,
                body.DurationMs,
                body.CtaType,
                body.CtaTarget),
            cancellationToken);

    /// <summary>حذف آیتم.</summary>
    public Task<AdminStorySnapshot> AdminRemoveItemAsync(
        Guid tenantId,
        Guid storyId,
        Guid itemId,
        CancellationToken cancellationToken) =>
        _stories.AdminRemoveItemAsync(tenantId, storyId, itemId, cancellationToken);

    /// <summary>مرتب‌سازی آیتم‌ها.</summary>
    public Task<AdminStorySnapshot> AdminReorderItemsAsync(
        Guid tenantId,
        Guid storyId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken) =>
        _stories.AdminReorderItemsAsync(tenantId, storyId, itemIds, cancellationToken);

    /// <summary>Tenant جاری را به Guid پایدار نگاشت می‌کند.</summary>
    public static Guid RequireTenantId(ICurrentTenant tenant) =>
        StoryTenantIds.FromTenantKey(
            tenant.Current?.TenantId.Value
            ?? throw new InvalidOperationException("Tenant resolve نشده است."));
}
