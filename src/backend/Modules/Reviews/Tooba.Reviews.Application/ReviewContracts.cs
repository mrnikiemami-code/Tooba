using Tooba.Reviews.Domain;

namespace Tooba.Reviews.Application;

/// <summary>ورودی ثبت بررسی؛ هویت Actor و نام عمومی از سرور تأمین می‌شود.</summary>
public sealed record SubmitProductReview(Guid ProductId, int Rating, string? Title, string Body);
/// <summary>DTO عمومی و امن بررسی؛ هیچ شناسهٔ داخلی نویسنده یا یادداشت مدیریتی ندارد.</summary>
public sealed record PublishedReview(Guid ReviewId, string AuthorDisplayName, int Rating, string? Title, string Body,
    bool IsVerifiedPurchase, DateTimeOffset CreatedAt);
/// <summary>خلاصهٔ عمومی امتیازهای Published.</summary>
public sealed record ReviewSummary(long Count, decimal Average, IReadOnlyDictionary<int, long> Distribution);
/// <summary>خلاصهٔ Published یک محصول برای ترکیب کارت و PDP.</summary>
public sealed record ProductReviewSummary(Guid ProductId, long ReviewCount, decimal? AverageRating);
/// <summary>صفحهٔ عمومی بررسی‌های Published.</summary>
public sealed record PublishedReviewPage(ReviewSummary Summary, IReadOnlyList<PublishedReview> Items, int Page, int PageSize);
/// <summary>ردیف صف مدیریت که دادهٔ ممیزی را فقط در مرز مدیر ارائه می‌کند.</summary>
public sealed record ModerationReview(Guid ReviewId, Guid ProductId, string AuthorDisplayName, int Rating, string? Title,
    string Body, bool IsVerifiedPurchase, DateTimeOffset CreatedAt, ReviewStatus Status);
/// <summary>صف شمارش‌دار صف مدیریت.</summary>
public sealed record ModerationReviewPage(IReadOnlyList<ModerationReview> Items, int Page, int PageSize, long TotalCount);

/// <summary>محاسبهٔ قطعی خلاصه از امتیازهای Published که لایهٔ زیرساخت فیلتر کرده است.</summary>
public static class ReviewSummaryCalculator
{
    /// <summary>تعداد، میانگین دو رقمی و توزیع کامل یک تا پنج را می‌سازد.</summary>
    public static ReviewSummary Calculate(IEnumerable<(int Rating, long Count)> groups)
    {
        var values = groups.ToArray();
        var count = values.Sum(x => x.Count);
        var distribution = Enumerable.Range(1, 5).ToDictionary(r => r, r => values.SingleOrDefault(x => x.Rating == r).Count);
        var average = count == 0 ? 0 : decimal.Round(values.Sum(x => x.Rating * x.Count) / (decimal)count, 2, MidpointRounding.AwayFromZero);
        return new ReviewSummary(count, average, distribution);
    }
}

/// <summary>قابلیت کاربردی ثبت، خواندن عمومی و تعدیل بررسی‌های محصول.</summary>
public interface IReviewDirectory
{
    /// <summary>بررسی را برای Actor نشست ثبت می‌کند و تکرار محصول/Actor را قطعی رد می‌کند.</summary>
    Task<Guid> SubmitAsync(Guid actorUserId, SubmitProductReview request, CancellationToken cancellationToken);
    /// <summary>خلاصهٔ Published چند محصول را در یک خواندن گروهی برمی‌گرداند.</summary>
    Task<IReadOnlyDictionary<Guid, ProductReviewSummary>> GetPublishedSummariesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
    /// <summary>خلاصه و صفحهٔ Published محصول را با slug برمی‌گرداند.</summary>
    Task<PublishedReviewPage?> GetPublishedAsync(string productSlug, int page, int pageSize, CancellationToken cancellationToken);
    /// <summary>صف Pending را برای مرز مدیر برمی‌گرداند.</summary>
    Task<ModerationReviewPage> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken);
    /// <summary>بررسی Pending را منتشر می‌کند.</summary>
    Task PublishAsync(Guid reviewId, Guid moderatorUserId, CancellationToken cancellationToken);
    /// <summary>بررسی Pending را با دلیل رد می‌کند.</summary>
    Task RejectAsync(Guid reviewId, Guid moderatorUserId, string reason, CancellationToken cancellationToken);
}
