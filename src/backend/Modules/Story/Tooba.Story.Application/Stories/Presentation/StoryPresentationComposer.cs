using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Tenant;

namespace Tooba.Story.Application.Stories.Presentation;

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
        Guard(() => _stories.GetPublicStoriesAsync(tenantId, locale, market, DateTimeOffset.UtcNow, cancellationToken));

    /// <summary>فهرست مدیریتی با فیلتر اختیاری بازبینی.</summary>
    public Task<IReadOnlyList<AdminStorySnapshot>> AdminListAsync(
        Guid tenantId, StoryReviewStatus? reviewStatus, CancellationToken cancellationToken) =>
        Guard(() => _stories.AdminListAsync(tenantId, reviewStatus, cancellationToken));

    /// <summary>صفحه‌بندی server-side گرید استوری Admin.</summary>
    public Task<GridPageResponse<AdminStorySnapshot>> QueryAdminGridAsync(
        Guid tenantId, StoryReviewStatus? reviewStatus, GridQueryRequest request, CancellationToken cancellationToken) =>
        Guard(() => _grid.QueryAsync(tenantId, reviewStatus, request, cancellationToken));

    /// <summary>فهرست در انتظار بازبینی.</summary>
    public Task<IReadOnlyList<AdminStorySnapshot>> AdminListPendingReviewAsync(
        Guid tenantId, CancellationToken cancellationToken) =>
        Guard(() => _stories.AdminListPendingReviewAsync(tenantId, cancellationToken));

    /// <summary>جزئیات مدیریتی.</summary>
    public Task<AdminStorySnapshot?> AdminGetAsync(Guid tenantId, Guid storyId, CancellationToken cancellationToken) =>
        Guard(() => _stories.AdminGetAsync(tenantId, storyId, cancellationToken));

    /// <summary>ایجاد استوری ادمین یا پیش‌نویس برای فروشگاه انتخاب‌شده.</summary>
    public Task<AdminStorySnapshot> AdminCreateAsync(
        Guid tenantId, Guid actorUserId, CreateStoryCommand command, CancellationToken cancellationToken) =>
        Guard(() => command.SellerPartyId is Guid shop && shop != Guid.Empty
            ? _stories.AdminCreateForShopAsync(tenantId, actorUserId, shop, command, cancellationToken)
            : _stories.AdminCreateAsync(tenantId, command, cancellationToken));

    /// <summary>ارسال پیش‌نویس متعلق به فروشگاه به بازبینی توسط ادمین.</summary>
    public Task<AdminStorySnapshot> AdminSubmitForShopAsync(
        Guid tenantId,
        Guid storyId,
        Guid adminActorUserId,
        int? expectedVersion,
        CancellationToken cancellationToken,
        string? occurrenceKey = null) =>
        Guard(() => _stories.AdminSubmitForShopAsync(
            tenantId, storyId, adminActorUserId, expectedVersion, cancellationToken, occurrenceKey));

    /// <summary>تأیید و انتشار اتمی.</summary>
    public Task<AdminStorySnapshot> AdminApproveAndPublishAsync(
        Guid tenantId, Guid storyId, Guid adminActorUserId, int? expectedVersion, CancellationToken cancellationToken,
        string? occurrenceKey = null) =>
        Guard(() => _stories.AdminApproveAndPublishAsync(
            tenantId, storyId, adminActorUserId, expectedVersion, cancellationToken, occurrenceKey));

    /// <summary>تأیید و زمان‌بندی اتمی.</summary>
    public Task<AdminStorySnapshot> AdminApproveAndScheduleAsync(
        Guid tenantId,
        Guid storyId,
        Guid adminActorUserId,
        int? expectedVersion,
        SetStoryScheduleCommand schedule,
        CancellationToken cancellationToken,
        string? occurrenceKey = null) =>
        Guard(() => _stories.AdminApproveAndScheduleAsync(
            tenantId, storyId, adminActorUserId, expectedVersion, schedule, cancellationToken, occurrenceKey));

    /// <summary>بازگشت فروشنده به پیش‌نویس.</summary>
    public Task<AdminStorySnapshot> SellerReturnToDraftAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, int? expectedVersion, CancellationToken cancellationToken,
        string? occurrenceKey = null) =>
        Guard(() => _stories.SellerReturnToDraftAsync(
            tenantId, sellerPartyId, storyId, expectedVersion, cancellationToken, occurrenceKey));

    /// <summary>توقف انتشار توسط فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerStopAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, int? expectedVersion, CancellationToken cancellationToken,
        string? occurrenceKey = null) =>
        Guard(() => _stories.SellerStopAsync(
            tenantId, sellerPartyId, storyId, expectedVersion, cancellationToken, occurrenceKey));

    /// <summary>پرس‌وجوی صفحه‌بندی‌شدهٔ فروشنده.</summary>
    public Task<GridPageResponse<AdminStorySnapshot>> SellerQueryAsync(
        Guid tenantId, Guid sellerPartyId, GridQueryRequest request, CancellationToken cancellationToken) =>
        Guard(() => _stories.SellerQueryAsync(tenantId, sellerPartyId, request, cancellationToken));

    /// <summary>به‌روزرسانی استوری.</summary>
    public Task<AdminStorySnapshot> AdminUpdateAsync(
        Guid tenantId, Guid storyId, UpdateStoryCommand command, CancellationToken cancellationToken) =>
        Guard(() => _stories.AdminUpdateAsync(tenantId, storyId, command, cancellationToken));

    /// <summary>حذف کل استوری ادمین.</summary>
    public Task<AdminStorySnapshot> AdminDeleteAsync(
        Guid tenantId, Guid storyId, int? expectedVersion, string? occurrenceKey, CancellationToken cancellationToken) =>
        Guard(() => _stories.AdminDeleteAsync(tenantId, storyId, expectedVersion, occurrenceKey, cancellationToken));

    /// <summary>فعال‌سازی.</summary>
    public Task<AdminStorySnapshot> AdminEnableAsync(
        Guid tenantId, Guid storyId, int? expectedVersion, CancellationToken cancellationToken,
        string? occurrenceKey = null) =>
        Guard(() => _stories.AdminSetStatusAsync(
            tenantId, storyId, StoryStatus.Active, expectedVersion, cancellationToken, occurrenceKey));

    /// <summary>غیرفعال‌سازی.</summary>
    public Task<AdminStorySnapshot> AdminDisableAsync(
        Guid tenantId, Guid storyId, int? expectedVersion, CancellationToken cancellationToken,
        string? occurrenceKey = null) =>
        Guard(() => _stories.AdminSoftDisableAsync(
            tenantId, storyId, expectedVersion, cancellationToken, occurrenceKey));

    /// <summary>زمان‌بندی.</summary>
    public Task<AdminStorySnapshot> AdminSetScheduleAsync(
        Guid tenantId, Guid storyId, SetStoryScheduleCommand command, CancellationToken cancellationToken) =>
        Guard(() => _stories.AdminSetScheduleAsync(tenantId, storyId, command, cancellationToken));

    /// <summary>تأیید استوری فروشنده.</summary>
    public Task<AdminStorySnapshot> AdminApproveAsync(
        Guid tenantId, Guid storyId, Guid adminActorUserId, int? expectedVersion, CancellationToken cancellationToken,
        string? occurrenceKey = null) =>
        Guard(() => _stories.AdminApproveAsync(
            tenantId, storyId, adminActorUserId, expectedVersion, cancellationToken, occurrenceKey));

    /// <summary>رد استوری فروشنده.</summary>
    public Task<AdminStorySnapshot> AdminRejectAsync(
        Guid tenantId, Guid storyId, Guid adminActorUserId, string reason, int? expectedVersion, CancellationToken cancellationToken,
        string? occurrenceKey = null) =>
        Guard(() => _stories.AdminRejectAsync(
            tenantId, storyId, adminActorUserId, reason, expectedVersion, cancellationToken, occurrenceKey));

    /// <summary>مرتب‌سازی استوری‌ها.</summary>
    public Task<IReadOnlyList<AdminStorySnapshot>> AdminReorderStoriesAsync(
        Guid tenantId, IReadOnlyList<Guid> storyIds, IReadOnlyList<int> expectedVersions, string? occurrenceKey,
        CancellationToken cancellationToken) =>
        Guard(() => _stories.AdminReorderStoriesAsync(
            tenantId, storyIds, expectedVersions, occurrenceKey, cancellationToken));

    /// <summary>افزودن آیتم.</summary>
    public Task<AdminStorySnapshot> AdminAddItemAsync(
        Guid tenantId, Guid storyId, AddStoryItemCommand command, CancellationToken cancellationToken) =>
        Guard(() => _stories.AdminAddItemAsync(tenantId, storyId, command, cancellationToken));

    /// <summary>به‌روزرسانی آیتم.</summary>
    public Task<AdminStorySnapshot> AdminUpdateItemAsync(
        Guid tenantId, Guid storyId, Guid itemId, UpdateStoryItemCommand command, CancellationToken cancellationToken) =>
        Guard(() => _stories.AdminUpdateItemAsync(tenantId, storyId, itemId, command, cancellationToken));

    /// <summary>حذف آیتم.</summary>
    public Task<AdminStorySnapshot> AdminRemoveItemAsync(
        Guid tenantId, Guid storyId, Guid itemId, int? expectedVersion, string? occurrenceKey,
        CancellationToken cancellationToken) =>
        Guard(() => _stories.AdminRemoveItemAsync(
            tenantId, storyId, itemId, expectedVersion, occurrenceKey, cancellationToken));

    /// <summary>مرتب‌سازی آیتم‌ها.</summary>
    public Task<AdminStorySnapshot> AdminReorderItemsAsync(
        Guid tenantId, Guid storyId, IReadOnlyList<Guid> itemIds, int? expectedVersion, string? occurrenceKey,
        CancellationToken cancellationToken) =>
        Guard(() => _stories.AdminReorderItemsAsync(
            tenantId, storyId, itemIds, expectedVersion, occurrenceKey, cancellationToken));

    /// <summary>فهرست استوری‌های فروشنده.</summary>
    public Task<IReadOnlyList<AdminStorySnapshot>> SellerListAsync(
        Guid tenantId, Guid sellerPartyId, CancellationToken cancellationToken) =>
        Guard(() => _stories.SellerListAsync(tenantId, sellerPartyId, cancellationToken));

    /// <summary>جزئیات استوری فروشنده.</summary>
    public Task<AdminStorySnapshot?> SellerGetAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, CancellationToken cancellationToken) =>
        Guard(() => _stories.SellerGetAsync(tenantId, sellerPartyId, storyId, cancellationToken));

    /// <summary>ایجاد پیش‌نویس فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerCreateDraftAsync(
        Guid tenantId, Guid sellerPartyId, Guid actorUserId, CreateStoryCommand command, CancellationToken cancellationToken) =>
        Guard(() => _stories.SellerCreateDraftAsync(tenantId, sellerPartyId, actorUserId, command, cancellationToken));

    /// <summary>به‌روزرسانی پیش‌نویس/ردشده فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerUpdateAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, UpdateStoryCommand command, CancellationToken cancellationToken) =>
        Guard(() => _stories.SellerUpdateAsync(tenantId, sellerPartyId, storyId, command, cancellationToken));

    /// <summary>حذف کل استوری قابل‌ویرایش فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerDeleteAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, int? expectedVersion, string? occurrenceKey,
        CancellationToken cancellationToken) =>
        Guard(() => _stories.SellerDeleteAsync(
            tenantId, sellerPartyId, storyId, expectedVersion, occurrenceKey, cancellationToken));

    /// <summary>ارسال برای بازبینی.</summary>
    public Task<AdminStorySnapshot> SellerSubmitAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, Guid actorUserId, CancellationToken cancellationToken,
        int? expectedVersion = null, string? occurrenceKey = null) =>
        Guard(() => _stories.SellerSubmitAsync(
            tenantId, sellerPartyId, storyId, actorUserId, cancellationToken, expectedVersion, occurrenceKey));

    /// <summary>افزودن آیتم فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerAddItemAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, AddStoryItemCommand command, CancellationToken cancellationToken) =>
        Guard(() => _stories.SellerAddItemAsync(tenantId, sellerPartyId, storyId, command, cancellationToken));

    /// <summary>به‌روزرسانی آیتم فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerUpdateItemAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, Guid itemId, UpdateStoryItemCommand command, CancellationToken cancellationToken) =>
        Guard(() => _stories.SellerUpdateItemAsync(tenantId, sellerPartyId, storyId, itemId, command, cancellationToken));

    /// <summary>حذف آیتم فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerRemoveItemAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, Guid itemId, int? expectedVersion, string? occurrenceKey,
        CancellationToken cancellationToken) =>
        Guard(() => _stories.SellerRemoveItemAsync(
            tenantId, sellerPartyId, storyId, itemId, expectedVersion, occurrenceKey, cancellationToken));

    /// <summary>مرتب‌سازی آیتم‌های فروشنده.</summary>
    public Task<AdminStorySnapshot> SellerReorderItemsAsync(
        Guid tenantId, Guid sellerPartyId, Guid storyId, IReadOnlyList<Guid> itemIds, int? expectedVersion,
        string? occurrenceKey, CancellationToken cancellationToken) =>
        Guard(() => _stories.SellerReorderItemsAsync(
            tenantId, sellerPartyId, storyId, itemIds, expectedVersion, occurrenceKey, cancellationToken));

    /// <summary>Tenant جاری را به Guid پایدار نگاشت می‌کند.</summary>
    public static Guid RequireTenantId(ICurrentTenant tenant)
    {
        var key = tenant.Current?.TenantId.Value;
        if (string.IsNullOrWhiteSpace(key))
            throw new SemanticException(new SemanticError(StoryErrorCodes.TenantMissing));

        try
        {
            return StoryTenantIds.FromTenantKey(key);
        }
        catch (SemanticException)
        {
            throw;
        }
    }

    private static async Task<T> Guard<T>(Func<Task<T>> action)
    {
        // Domain/Infra now throw SemanticException with stable codes; no message-text remapping.
        return await action();
    }
}
