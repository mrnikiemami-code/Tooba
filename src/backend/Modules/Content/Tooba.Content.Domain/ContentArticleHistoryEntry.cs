using Tooba.BuildingBlocks;

namespace Tooba.Content.Domain;

/// <summary>ردیف append-only تاریخچهٔ چرخهٔ عمر مقاله.</summary>
public sealed class ContentArticleHistoryEntry
{
    /// <summary>شناسهٔ ردیف.</summary>
    public Guid HistoryId { get; init; }

    /// <summary>مقالهٔ مالک.</summary>
    public Guid ArticleId { get; init; }

    /// <summary>کد پایدار رویداد.</summary>
    public string EventType { get; init; } = "";

    /// <summary>خلاصهٔ انسانی فارسی.</summary>
    public string SummaryFa { get; init; } = "";

    /// <summary>خلاصهٔ انسانی انگلیسی.</summary>
    public string SummaryEn { get; init; } = "";

    /// <summary>وضعیت قبلی (اختیاری).</summary>
    public string? PreviousState { get; init; }

    /// <summary>وضعیت جدید (اختیاری).</summary>
    public string? NewState { get; init; }

    /// <summary>شناسهٔ بازیگر.</summary>
    public Guid? ActorUserId { get; init; }

    /// <summary>نام نمایشی بازیگر.</summary>
    public string? ActorDisplayName { get; init; }

    /// <summary>زمان رخداد UTC.</summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>ردیف تاریخچه می‌سازد.</summary>
    public static ContentArticleHistoryEntry Create(
        Guid articleId,
        string eventType,
        string summaryFa,
        string summaryEn,
        DateTimeOffset occurredAt,
        string? previousState = null,
        string? newState = null,
        Guid? actorUserId = null,
        string? actorDisplayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(summaryFa);
        ArgumentException.ThrowIfNullOrWhiteSpace(summaryEn);
        return new ContentArticleHistoryEntry
        {
            HistoryId = UuidV7.New(),
            ArticleId = articleId,
            EventType = eventType.Trim(),
            SummaryFa = Truncate(summaryFa, 512)!,
            SummaryEn = Truncate(summaryEn, 512)!,
            PreviousState = Truncate(previousState, 64),
            NewState = Truncate(newState, 64),
            ActorUserId = actorUserId,
            ActorDisplayName = Truncate(actorDisplayName, 120),
            OccurredAt = occurredAt,
        };
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

/// <summary>کدها و برچسب‌های تاریخچهٔ مقاله.</summary>
public static class ArticleHistoryRules
{
    /// <summary>ایجاد پیش‌نویس.</summary>
    public const string EventDraftCreated = "article.draft_created";
    /// <summary>به‌روزرسانی کلیدی.</summary>
    public const string EventUpdated = "article.updated";
    /// <summary>انتشار.</summary>
    public const string EventPublished = "article.published";
    /// <summary>زمان‌بندی.</summary>
    public const string EventScheduled = "article.scheduled";
    /// <summary>لغو انتشار.</summary>
    public const string EventUnpublished = "article.unpublished";
    /// <summary>انتشار مجدد.</summary>
    public const string EventRepublished = "article.republished";
    /// <summary>بایگانی.</summary>
    public const string EventArchived = "article.archived";

    /// <summary>خلاصهٔ ایجاد پیش‌نویس (fa).</summary>
    public const string SummaryDraftCreatedFa = "پیش‌نویس مقاله ایجاد شد";
    /// <summary>خلاصهٔ ایجاد پیش‌نویس (en).</summary>
    public const string SummaryDraftCreatedEn = "Article draft created";
    /// <summary>خلاصهٔ به‌روزرسانی (fa).</summary>
    public const string SummaryUpdatedFa = "مقاله به‌روزرسانی شد";
    /// <summary>خلاصهٔ به‌روزرسانی (en).</summary>
    public const string SummaryUpdatedEn = "Article updated";
    /// <summary>خلاصهٔ انتشار (fa).</summary>
    public const string SummaryPublishedFa = "مقاله منتشر شد";
    /// <summary>خلاصهٔ انتشار (en).</summary>
    public const string SummaryPublishedEn = "Article published";
    /// <summary>خلاصهٔ زمان‌بندی (fa).</summary>
    public const string SummaryScheduledFa = "انتشار مقاله برای آینده زمان‌بندی شد";
    /// <summary>خلاصهٔ زمان‌بندی (en).</summary>
    public const string SummaryScheduledEn = "Article scheduled for future publication";
    /// <summary>خلاصهٔ لغو انتشار (fa).</summary>
    public const string SummaryUnpublishedFa = "انتشار مقاله لغو شد";
    /// <summary>خلاصهٔ لغو انتشار (en).</summary>
    public const string SummaryUnpublishedEn = "Article unpublished";
    /// <summary>خلاصهٔ انتشار مجدد (fa).</summary>
    public const string SummaryRepublishedFa = "مقاله دوباره منتشر شد";
    /// <summary>خلاصهٔ انتشار مجدد (en).</summary>
    public const string SummaryRepublishedEn = "Article republished";
    /// <summary>خلاصهٔ بایگانی (fa).</summary>
    public const string SummaryArchivedFa = "مقاله بایگانی شد";
    /// <summary>خلاصهٔ بایگانی (en).</summary>
    public const string SummaryArchivedEn = "Article archived";

    /// <summary>نام بازیگر سیستم (fa).</summary>
    public const string ActorSystemFa = "سیستم";
    /// <summary>نام بازیگر سیستم (en).</summary>
    public const string ActorSystemEn = "System";

    /// <summary>برچسب انسانی رویداد برای UI — بدون کلید خام.</summary>
    public static string EventLabelFa(string eventType) =>
        eventType switch
        {
            EventDraftCreated => "ایجاد پیش‌نویس",
            EventUpdated => "به‌روزرسانی",
            EventPublished => "انتشار",
            EventScheduled => "زمان‌بندی انتشار",
            EventUnpublished => "لغو انتشار",
            EventRepublished => "انتشار مجدد",
            EventArchived => "بایگانی",
            _ => "رویداد",
        };

    /// <summary>برچسب انسانی انگلیسی رویداد.</summary>
    public static string EventLabelEn(string eventType) =>
        eventType switch
        {
            EventDraftCreated => "Draft created",
            EventUpdated => "Updated",
            EventPublished => "Published",
            EventScheduled => "Scheduled",
            EventUnpublished => "Unpublished",
            EventRepublished => "Republished",
            EventArchived => "Archived",
            _ => "Event",
        };

    /// <summary>برچسب وضعیت چرخهٔ عمر.</summary>
    public static string StatusLabelFa(ContentPublicationStatus status) =>
        status switch
        {
            ContentPublicationStatus.Draft => "پیش‌نویس",
            ContentPublicationStatus.Published => "منتشرشده",
            ContentPublicationStatus.Archived => "بایگانی",
            _ => status.ToString(),
        };

    /// <summary>برچسب انگلیسی وضعیت.</summary>
    public static string StatusLabelEn(ContentPublicationStatus status) =>
        status switch
        {
            ContentPublicationStatus.Draft => "Draft",
            ContentPublicationStatus.Published => "Published",
            ContentPublicationStatus.Archived => "Archived",
            _ => status.ToString(),
        };
}
