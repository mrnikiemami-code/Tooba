using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Story.Application;
using Tooba.Story.Domain;

namespace Tooba.Story.Application.Presentation;

/// <summary>ترکیب use-case برای مسیرهای عمومی، فروشنده و مدیریتی Story.</summary>
public sealed class StoryPresentationComposer
{
    private readonly IStoryDirectory _stories;
    private readonly IAdminStoryGridPort _grid;

    /// <summary>دایرکتوری Story و پورت گرید ماژول را تزریق می‌کند.</summary>
    public StoryPresentationComposer(IStoryDirectory stories, IAdminStoryGridPort grid)
    {
        _stories = stories;
        _grid = grid;
    }

    /// <summary>استوری‌های عمومی.</summary>
    public Task<IReadOnlyList<PublicStoryCard>> GetPublicStoriesAsync(
        Guid tenantId, string? locale, string? market, CancellationToken cancellationToken) =>
        _stories.GetPublicStoriesAsync(tenantId, locale, market, DateTimeOffset.UtcNow, cancellationToken);

    /// <summary>فهرست مدیریتی با فیلتر اختیاری بازبینی.</summary>
    public Task<IReadOnlyList<AdminStorySnapshot>> AdminListAsync(
        Guid tenantId, StoryReviewStatus? reviewStatus, CancellationToken cancellationToken) =>
        _stories.AdminListAsync(tenantId, reviewStatus, cancellationToken);

    /// <summary>صفحه‌بندی server-side گرید استوری Admin.</summary>
    public Task<GridPageResponse<AdminStorySnapshot>> QueryAdminGridAsync(
        Guid tenantId, StoryReviewStatus? reviewStatus, GridQueryRequest request, CancellationToken cancellationToken) =>
        _grid.QueryAsync(tenantId, reviewStatus, request, cancellationToken);

    /// <summary>فهرست در انتظار بازبینی.</summary>
    public Task<IReadOnlyList<AdminStorySnapshot>> AdminListPendingReviewAsync(
        Guid tenantId, CancellationToken cancellationToken) =>
        _stories.AdminListPendingReviewAsync(tenantId, cancellationToken);

    /// <summary>جزئیات مدیریتی.</summary>
    public Task<AdminStorySnapshot?> AdminGetAsync(Guid tenantId, Guid storyId, CancellationToken cancellationToken) =>
        _stories.AdminGetAsync(tenantId, storyId, cancellationToken);

    /// <summary>ایجاد استوری ادمین.</summary>
    public Task<AdminStorySnapshot> AdminCreateAsync(
        Guid tenantId, CreateStoryCommand command, CancellationToken cancellationToken) =>
        _stories.AdminCreateAsync(tenantId, command, cancellationToken);

    /// <summary>به‌روزرسانی استوری.</summary>
    public Task<AdminStorySnapshot> AdminUpdateAsync(
        Guid tenantId, Guid storyId, UpdateStoryCommand command, CancellationToken cancellationToken) =>
        _stories.AdminUpdateAsync(tenantId, storyId, command, cancellationToken);

    /// <summary>فعال‌سازی.</summary>
    public Task<AdminStorySnapshot> AdminEnableAsync(Guid tenantId, Guid storyId, CancellationToken cancellationToken) =>
        _stories.AdminSetStatusAsync(tenantId, storyId, StoryStatus.Active, cancellationToken);

    /// <summary>غیرفعال‌سازی.</summary>
    public Task<AdminStorySnapshot> AdminDisableAsync(Guid tenantId, Guid storyId, CancellationToken cancellationToken) =>
        _stories.AdminSoftDisableAsync(tenantId, storyId, cancellationToken);

    /// <summary>زمان‌بندی.</summary>
    public Task<AdminStorySnapshot> AdminSetScheduleAsync(
        Guid tenantId, Guid storyId, SetStoryScheduleCommand command, CancellationToken cancellationToken) =>
        _stories.AdminSetScheduleAsync(tenantId, storyId, command, cancellationToken);

    /// <summary>تأیید استوری فروشنده.</summary>
    public Task<AdminStorySnapshot> AdminApproveAsync(
        Guid tenantId, Guid storyId, Guid adminActorUserId, CancellationToken cancellationToken) =>
        _stories.AdminApproveAsync(tenantId, storyId, adminActorUserId, cancellationToken);

    /// <summary>رد استوری فروشنده.</summary>
    public Task<AdminStorySnapshot> AdminRejectAsync(
        Guid tenantId, Guid storyId, Guid adminActorUserId, string reason, CancellationToken cancellationToken) =>
        _stories.AdminRejectAsync(tenantId, storyId, adminActorUserId, reason, cancellationToken);

    /// <summary>مرتب‌سازی استوری‌ها.</summary>
    public Task<IReadOnlyList<AdminStorySnapshot>> AdminReorderStoriesAsync(
        Guid tenantId, IReadOnlyList<Guid> storyIds, CancellationToken cancellationToken) =>
        _stories.AdminReorderStoriesAsync(tenantId, storyIds, cancellationToken);

    /// <summary>افزودن آیتم.</summary>
    public Task<AdminStorySnapshot> AdminAddItemAsync(
        Guid tenantId, Guid storyId, AddStoryItemCommand command, CancellationToken cancellationToken) =>
        _stories.AdminAddItemAsync(tenantId, storyId, command, cancellationToken);

    /// <summary>به‌روزرسانی آیتم.</summary>
    public Task<AdminStorySnapshot> AdminUpdateItemAsync(
        Guid tenantId, Guid storyId, Guid itemId, UpdateStoryItemCommand command, CancellationToken cancellationToken) =>
        _stories.AdminUpdateItemAsync(tenantId, storyId, itemId, command, cancellationToken);

    /// <summary>حذف آیتم.</summary>
    public Task<AdminStorySnapshot> AdminRemoveItemAsync(
        Guid tenantId, Guid storyId, Guid itemId, CancellationToken cancellationToken) =>
        _stories.AdminRemoveItemAsync(tenantId, storyId, itemId, cancellationToken);

    /// <summary>مرتب‌سازی آیتم‌ها.</summary>
    public Task<AdminStorySnapshot> AdminReorderItemsAsync(
        Guid tenantId, Guid storyId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken) =>
        _stories.AdminReorderItemsAsync(tenantId, storyId, itemIds, cancellationToken);

    /// <summary>فهرست استوری‌های فروشنده.</summary>
    public Task<IReadOnlyList<AdminStorySnapshot>> SellerListAsync(
        Guid tenantId, Guid sellerPartyId, CancellationToken cancellationToken) =>
        _stories.SellerListAsync(tenantId, sellerPartyId, cancellationToken);

    /// <summary>جزئیات استوری فروشنده.</summary>
    public Task<AdminStorySnapshot?> SellerGetAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, CancellationToken cancellationToken) =>
        _stories.SellerGetAsync(tenantId, sellerPartyId, storyId, cancellationToken);

    /// <summary>ایجاد پیش‌نویس فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerCreateDraftAsync(
        Guid tenantId, Guid sellerPartyId, Guid actorUserId, CreateStoryCommand command, CancellationToken cancellationToken) =>
        _stories.SellerCreateDraftAsync(tenantId, sellerPartyId, actorUserId, command, cancellationToken);

    /// <summary>به‌روزرسانی پیش‌نویس/ردشده فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerUpdateAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, UpdateStoryCommand command, CancellationToken cancellationToken) =>
        _stories.SellerUpdateAsync(tenantId, sellerPartyId, storyId, command, cancellationToken);

    /// <summary>ارسال برای بازبینی.</summary>
    public Task<AdminStorySnapshot> SellerSubmitAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, Guid actorUserId, CancellationToken cancellationToken) =>
        _stories.SellerSubmitAsync(tenantId, sellerPartyId, storyId, actorUserId, cancellationToken);

    /// <summary>افزودن آیتم فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerAddItemAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, AddStoryItemCommand command, CancellationToken cancellationToken) =>
        _stories.SellerAddItemAsync(tenantId, sellerPartyId, storyId, command, cancellationToken);

    /// <summary>به‌روزرسانی آیتم فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerUpdateItemAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, Guid itemId, UpdateStoryItemCommand command, CancellationToken cancellationToken) =>
        _stories.SellerUpdateItemAsync(tenantId, sellerPartyId, storyId, itemId, command, cancellationToken);

    /// <summary>حذف آیتم فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerRemoveItemAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, Guid itemId, CancellationToken cancellationToken) =>
        _stories.SellerRemoveItemAsync(tenantId, sellerPartyId, storyId, itemId, cancellationToken);

    /// <summary>مرتب‌سازی آیتم‌های فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerReorderItemsAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken) =>
        _stories.SellerReorderItemsAsync(tenantId, sellerPartyId, storyId, itemIds, cancellationToken);

    /// <summary>Tenant جاری را به Guid پایدار نگاشت می‌کند.</summary>
    public static Guid RequireTenantId(ICurrentTenant tenant) =>
        StoryTenantIds.FromTenantKey(
            tenant.Current?.TenantId.Value
            ?? throw new InvalidOperationException("Tenant resolve نشده است."));
}
