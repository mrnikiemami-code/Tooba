namespace Tooba.Content.Domain;

/// <summary>قواعد دید عمومی مقاله — انتشار مستقل و زمان‌بندی.</summary>
public static class ContentArticlePublicRules
{
    /// <summary>آیا مقاله در لحظهٔ utcNow برای عموم قابل نمایش است.</summary>
    public static bool IsPubliclyVisible(
        ContentPublicationStatus status,
        DateTimeOffset publishDate,
        DateTimeOffset utcNow) =>
        status == ContentPublicationStatus.Published && publishDate <= utcNow;
}
