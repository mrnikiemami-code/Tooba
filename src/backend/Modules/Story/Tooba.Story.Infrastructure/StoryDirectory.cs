using Microsoft.EntityFrameworkCore;
using Tooba.Story.Application;
using Tooba.Story.Domain;
using Tooba.Story.Infrastructure.Persistence;
using StoryEntity = Tooba.Story.Domain.Story;

namespace Tooba.Story.Infrastructure;

/// <summary>دایرکتوری Story با schema مستقل.</summary>
public sealed class StoryDirectory : IStoryDirectory
{
    private readonly StoryDbContext _db;

    /// <summary>DbContext مالک را تزریق می‌کند.</summary>
    public StoryDirectory(StoryDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublicStoryCard>> GetPublicStoriesAsync(
        Guid tenantId,
        string? locale,
        string? market,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Stories.AsNoTracking()
            .Where(story => story.TenantId == tenantId && story.Status == StoryStatus.Active)
            .OrderBy(story => story.DisplayOrder)
            .ThenBy(story => story.StoryId)
            .ToListAsync(cancellationToken);

        var visible = rows
            .Where(story => story.IsPubliclyVisible(now)
                && StoryRules.MatchesLocale(story.Locale, locale)
                && StoryRules.MatchesMarket(story.Market, market))
            .ToList();

        if (visible.Count == 0)
            return [];

        var ids = visible.Select(story => story.StoryId).ToList();
        var items = await _db.StoryItems.AsNoTracking()
            .Where(item => ids.Contains(item.StoryId))
            .OrderBy(item => item.DisplayOrder)
            .ToListAsync(cancellationToken);
        var itemsByStory = items.GroupBy(item => item.StoryId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<StoryItem>)group.ToList());

        return visible.Select(story =>
        {
            story.AttachItems(itemsByStory.GetValueOrDefault(story.StoryId, []));
            return MapPublic(story);
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminStorySnapshot>> AdminListAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var stories = await LoadStoriesAsync(tenantId, track: false, cancellationToken);
        return stories
            .OrderBy(story => story.DisplayOrder)
            .ThenBy(story => story.StoryId)
            .Select(MapAdmin)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot?> AdminGetAsync(
        Guid tenantId,
        Guid storyId,
        CancellationToken cancellationToken)
    {
        var story = await LoadStoryAsync(tenantId, storyId, track: false, cancellationToken);
        return story is null ? null : MapAdmin(story);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminCreateAsync(
        Guid tenantId,
        CreateStoryCommand command,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var order = command.DisplayOrder
            ?? await NextDisplayOrderAsync(tenantId, cancellationToken);
        var story = StoryEntity.CreateDraft(
            tenantId,
            command.Title,
            order,
            now,
            command.Locale,
            command.Market,
            command.CoverMediaAssetId,
            command.CoverMediaUrl,
            command.CtaType,
            command.CtaTarget);
        _db.Stories.Add(story);
        await _db.SaveChangesAsync(cancellationToken);
        return MapAdmin(story);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminUpdateAsync(
        Guid tenantId,
        Guid storyId,
        UpdateStoryCommand command,
        CancellationToken cancellationToken)
    {
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        story.Update(
            command.Title,
            command.Locale,
            command.Market,
            command.CoverMediaAssetId,
            command.CoverMediaUrl,
            command.CtaType,
            command.CtaTarget,
            DateTimeOffset.UtcNow);
        await SaveStoryAsync(story, cancellationToken);
        return MapAdmin(story);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminSetStatusAsync(
        Guid tenantId,
        Guid storyId,
        StoryStatus status,
        CancellationToken cancellationToken)
    {
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        switch (status)
        {
            case StoryStatus.Active:
                story.Activate(now);
                break;
            case StoryStatus.Disabled:
                story.Disable(now);
                break;
            case StoryStatus.Expired:
                story.MarkExpired(now);
                break;
            case StoryStatus.Draft:
                throw new InvalidOperationException("بازگشت مستقیم به Draft از این مسیر مجاز نیست.");
            case StoryStatus.Scheduled:
                throw new InvalidOperationException("وضعیت Scheduled فقط از طریق زمان‌بندی تنظیم می‌شود.");
            default:
                throw new InvalidOperationException("وضعیت استوری مجاز نیست.");
        }

        await SaveStoryAsync(story, cancellationToken);
        return MapAdmin(story);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminSetScheduleAsync(
        Guid tenantId,
        Guid storyId,
        SetStoryScheduleCommand command,
        CancellationToken cancellationToken)
    {
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        story.SetSchedule(command.StartAt, command.EndAt, DateTimeOffset.UtcNow);
        await SaveStoryAsync(story, cancellationToken);
        return MapAdmin(story);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminStorySnapshot>> AdminReorderStoriesAsync(
        Guid tenantId,
        IReadOnlyList<Guid> storyIdsInOrder,
        CancellationToken cancellationToken)
    {
        var stories = await LoadStoriesAsync(tenantId, track: true, cancellationToken);
        if (storyIdsInOrder.Count != stories.Count)
            throw new InvalidOperationException("ترتیب استوری با تعداد فعلی هم‌خوان نیست.");
        if (storyIdsInOrder.Distinct().Count() != storyIdsInOrder.Count)
            throw new InvalidOperationException("شناسهٔ استوری تکراری در ترتیب وجود دارد.");

        var lookup = stories.ToDictionary(story => story.StoryId);
        var now = DateTimeOffset.UtcNow;
        for (var index = 0; index < storyIdsInOrder.Count; index++)
        {
            if (!lookup.TryGetValue(storyIdsInOrder[index], out var story))
                throw new InvalidOperationException("استوری برای مرتب‌سازی یافت نشد.");
            story.SetDisplayOrder(index, now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return stories
            .OrderBy(story => story.DisplayOrder)
            .Select(MapAdmin)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminAddItemAsync(
        Guid tenantId,
        Guid storyId,
        AddStoryItemCommand command,
        CancellationToken cancellationToken)
    {
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var order = command.DisplayOrder
            ?? (story.Items.Count == 0 ? 0 : story.Items.Max(item => item.DisplayOrder) + 1);
        story.AddItem(
            command.MediaType,
            order,
            now,
            command.MediaAssetId,
            command.MediaUrl,
            command.Caption,
            command.DurationMs,
            command.CtaType,
            command.CtaTarget);
        await SaveStoryAsync(story, cancellationToken);
        return MapAdmin(story);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminUpdateItemAsync(
        Guid tenantId,
        Guid storyId,
        Guid itemId,
        UpdateStoryItemCommand command,
        CancellationToken cancellationToken)
    {
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        story.UpdateItem(
            itemId,
            command.MediaType,
            command.MediaAssetId,
            command.MediaUrl,
            command.Caption,
            command.DurationMs,
            command.CtaType,
            command.CtaTarget,
            DateTimeOffset.UtcNow);
        await SaveStoryAsync(story, cancellationToken);
        return MapAdmin(story);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminRemoveItemAsync(
        Guid tenantId,
        Guid storyId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        story.RemoveItem(itemId, DateTimeOffset.UtcNow);
        await SaveStoryAsync(story, cancellationToken);
        return MapAdmin(story);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminReorderItemsAsync(
        Guid tenantId,
        Guid storyId,
        IReadOnlyList<Guid> itemIdsInOrder,
        CancellationToken cancellationToken)
    {
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        story.ReorderItems(itemIdsInOrder, DateTimeOffset.UtcNow);
        await SaveStoryAsync(story, cancellationToken);
        return MapAdmin(story);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminSoftDisableAsync(
        Guid tenantId,
        Guid storyId,
        CancellationToken cancellationToken)
    {
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        story.Disable(DateTimeOffset.UtcNow);
        await SaveStoryAsync(story, cancellationToken);
        return MapAdmin(story);
    }

    internal static AdminStorySnapshot MapAdmin(StoryEntity story) => new(
        story.StoryId,
        story.TenantId,
        story.Locale,
        story.Market,
        story.Title,
        story.CoverMediaAssetId,
        story.CoverMediaUrl,
        story.DisplayOrder,
        story.StartAt,
        story.EndAt,
        story.Status,
        story.CtaType,
        story.CtaTarget,
        story.VersionToken,
        story.CreatedAt,
        story.UpdatedAt,
        story.Items
            .OrderBy(item => item.DisplayOrder)
            .Select(MapAdminItem)
            .ToList());

    private static PublicStoryCard MapPublic(StoryEntity story)
    {
        var items = story.Items
            .OrderBy(item => item.DisplayOrder)
            .Select(MapPublicItem)
            .ToList();
        return new PublicStoryCard(
            story.StoryId,
            story.Title,
            story.CoverMediaUrl,
            items.Any(item => string.Equals(item.MediaType, StoryRules.MediaVideo, StringComparison.Ordinal)),
            story.DisplayOrder,
            story.CtaType,
            story.CtaTarget,
            items);
    }

    private static PublicStoryItem MapPublicItem(StoryItem item) => new(
        item.StoryItemId,
        item.MediaType,
        item.MediaUrl,
        item.Caption,
        item.DurationMs,
        item.CtaType,
        item.CtaTarget);

    private static AdminStoryItemSnapshot MapAdminItem(StoryItem item) => new(
        item.StoryItemId,
        item.DisplayOrder,
        item.MediaType,
        item.MediaAssetId,
        item.MediaUrl,
        item.Caption,
        item.DurationMs,
        item.CtaType,
        item.CtaTarget,
        item.CreatedAt,
        item.UpdatedAt);

    private async Task<int> NextDisplayOrderAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var max = await _db.Stories.AsNoTracking()
            .Where(story => story.TenantId == tenantId)
            .Select(story => (int?)story.DisplayOrder)
            .MaxAsync(cancellationToken);
        return (max ?? -1) + 1;
    }

    private async Task<StoryEntity> RequireStoryAsync(
        Guid tenantId,
        Guid storyId,
        CancellationToken cancellationToken)
    {
        var story = await LoadStoryAsync(tenantId, storyId, track: true, cancellationToken);
        return story ?? throw new InvalidOperationException("استوری یافت نشد.");
    }

    private async Task<StoryEntity?> LoadStoryAsync(
        Guid tenantId,
        Guid storyId,
        bool track,
        CancellationToken cancellationToken)
    {
        var query = track ? _db.Stories : _db.Stories.AsNoTracking();
        var story = await query.FirstOrDefaultAsync(
            row => row.TenantId == tenantId && row.StoryId == storyId,
            cancellationToken);
        if (story is null)
            return null;

        var itemQuery = track ? _db.StoryItems : _db.StoryItems.AsNoTracking();
        var items = await itemQuery
            .Where(item => item.StoryId == story.StoryId)
            .OrderBy(item => item.DisplayOrder)
            .ToListAsync(cancellationToken);
        story.AttachItems(items);
        return story;
    }

    private async Task<List<StoryEntity>> LoadStoriesAsync(
        Guid tenantId,
        bool track,
        CancellationToken cancellationToken)
    {
        var query = track ? _db.Stories : _db.Stories.AsNoTracking();
        var stories = await query
            .Where(story => story.TenantId == tenantId)
            .OrderBy(story => story.DisplayOrder)
            .ThenBy(story => story.StoryId)
            .ToListAsync(cancellationToken);
        if (stories.Count == 0)
            return stories;

        var ids = stories.Select(story => story.StoryId).ToList();
        var itemQuery = track ? _db.StoryItems : _db.StoryItems.AsNoTracking();
        var items = await itemQuery
            .Where(item => ids.Contains(item.StoryId))
            .OrderBy(item => item.DisplayOrder)
            .ToListAsync(cancellationToken);
        var grouped = items.GroupBy(item => item.StoryId)
            .ToDictionary(group => group.Key, group => group.ToList());
        foreach (var story in stories)
            story.AttachItems(grouped.GetValueOrDefault(story.StoryId, []));
        return stories;
    }

    private async Task SaveStoryAsync(StoryEntity story, CancellationToken cancellationToken)
    {
        _db.Stories.Update(story);
        var existingItems = await _db.StoryItems
            .Where(item => item.StoryId == story.StoryId)
            .ToListAsync(cancellationToken);
        var currentIds = story.Items.Select(item => item.StoryItemId).ToHashSet();
        foreach (var removed in existingItems.Where(item => !currentIds.Contains(item.StoryItemId)))
            _db.StoryItems.Remove(removed);
        foreach (var item in story.Items)
        {
            if (existingItems.Any(existing => existing.StoryItemId == item.StoryItemId))
                _db.StoryItems.Update(item);
            else
                _db.StoryItems.Add(item);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
