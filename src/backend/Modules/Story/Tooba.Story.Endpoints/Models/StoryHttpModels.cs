using Tooba.Story.Application.Stories.Models;

namespace Tooba.Story.Endpoints.Models;

/// <summary>بدنهٔ ایجاد استوری.</summary>
public sealed record CreateStoryBody(
    string Title,
    string? Locale,
    string? Market,
    Guid? CoverMediaAssetId,
    string? CoverMediaUrl,
    int? DisplayOrder,
    string? CtaType,
    string? CtaTarget);

/// <summary>بدنهٔ به‌روزرسانی استوری.</summary>
public sealed record UpdateStoryBody(
    string Title,
    string? Locale,
    string? Market,
    Guid? CoverMediaAssetId,
    string? CoverMediaUrl,
    string? CtaType,
    string? CtaTarget);

/// <summary>بدنهٔ زمان‌بندی استوری.</summary>
public sealed record SetStoryScheduleBody(DateTimeOffset? StartAt, DateTimeOffset? EndAt);

/// <summary>بدنهٔ رد استوری.</summary>
public sealed record RejectStoryBody(string? Reason);

/// <summary>بدنهٔ مرتب‌سازی استوری‌ها.</summary>
public sealed record ReorderStoriesBody(IReadOnlyList<Guid> StoryIds);

/// <summary>بدنهٔ افزودن آیتم.</summary>
public sealed record AddStoryItemBody(
    string MediaType,
    Guid? MediaAssetId,
    string? MediaUrl,
    string? Caption,
    int? DurationMs,
    string? CtaType,
    string? CtaTarget,
    int? DisplayOrder);

/// <summary>بدنهٔ به‌روزرسانی آیتم.</summary>
public sealed record UpdateStoryItemBody(
    string MediaType,
    Guid? MediaAssetId,
    string? MediaUrl,
    string? Caption,
    int? DurationMs,
    string? CtaType,
    string? CtaTarget);

/// <summary>بدنهٔ مرتب‌سازی آیتم‌ها.</summary>
public sealed record ReorderStoryItemsBody(IReadOnlyList<Guid> ItemIds);

/// <summary>نگاشت بدنهٔ HTTP به فرمان‌های Application.</summary>
public static class StoryBodyMapping
{
    public static CreateStoryCommand ToCreate(CreateStoryBody body) => new(
        body.Title, body.Locale, body.Market, body.CoverMediaAssetId, body.CoverMediaUrl,
        body.DisplayOrder, body.CtaType, body.CtaTarget);

    public static UpdateStoryCommand ToUpdate(UpdateStoryBody body) => new(
        body.Title, body.Locale, body.Market, body.CoverMediaAssetId, body.CoverMediaUrl,
        body.CtaType, body.CtaTarget);

    public static SetStoryScheduleCommand ToSchedule(SetStoryScheduleBody body) =>
        new(body.StartAt, body.EndAt);

    public static AddStoryItemCommand ToAddItem(AddStoryItemBody body) => new(
        body.MediaType, body.MediaAssetId, body.MediaUrl, body.Caption, body.DurationMs,
        body.CtaType, body.CtaTarget, body.DisplayOrder);

    public static UpdateStoryItemCommand ToUpdateItem(UpdateStoryItemBody body) => new(
        body.MediaType, body.MediaAssetId, body.MediaUrl, body.Caption, body.DurationMs,
        body.CtaType, body.CtaTarget);
}
