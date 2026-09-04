using System.Text.RegularExpressions;

namespace Tooba.Content.Domain;

/// <summary>یک بررسی آمادگی انتشار مقاله — قرارداد پایدار برای Admin و Publish.</summary>
public sealed record ArticlePublicationCheck(
    string Key,
    string LabelKey,
    bool Required,
    bool Satisfied,
    string? Detail = null,
    string? ActionTarget = null);

/// <summary>
/// نتیجهٔ آمادگی انتشار — تنها منبع قوانین مشترک readiness query و Publish.
/// score فقط برای UX؛ جایگزین چک‌لیست نیست.
/// </summary>
public sealed record ArticlePublicationReadiness(
    bool CanPublish,
    IReadOnlyList<ArticlePublicationCheck> Checks,
    IReadOnlyList<ArticlePublicationCheck> RequiredMissing,
    IReadOnlyList<ArticlePublicationCheck> RecommendedMissing,
    int? Score);

/// <summary>ورودی ارزیابی آمادگی بدون وابستگی به Infrastructure.</summary>
public sealed record ArticlePublicationReadinessInput(
    string Title,
    string Excerpt,
    string Body,
    string Slug,
    string Locale,
    Guid? AuthorId,
    Guid? CategoryId,
    Guid? CoverMediaAssetId,
    Guid? SeoImageMediaAssetId,
    string? SeoTitle,
    string? SeoDescription,
    ContentPublicationStatus Status,
    DateTimeOffset PublishDate,
    bool LanguageIsActive,
    DateTimeOffset UtcNow);

/// <summary>کدهای پایدار آمادگی/انتشار مقاله.</summary>
public static class ArticlePublicationCodes
{
    /// <summary>عنوان.</summary>
    public const string Title = "content.publish.title";
    /// <summary>چکیده.</summary>
    public const string Excerpt = "content.publish.excerpt";
    /// <summary>بدنه.</summary>
    public const string Body = "content.publish.body";
    /// <summary>نویسنده.</summary>
    public const string Author = "content.publish.author";
    /// <summary>دسته.</summary>
    public const string Category = "content.publish.category";
    /// <summary>تصویر شاخص.</summary>
    public const string FeaturedImage = "content.publish.featured_image";
    /// <summary>عنوان SEO.</summary>
    public const string SeoTitle = "content.publish.seo_title";
    /// <summary>توضیح SEO.</summary>
    public const string SeoDescription = "content.publish.seo_description";
    /// <summary>تصویر SEO/اجتماعی.</summary>
    public const string SeoImage = "content.publish.seo_image";
    /// <summary>زبان فعال.</summary>
    public const string LanguageActive = "content.publish.language_active";
    /// <summary>slug.</summary>
    public const string Slug = "content.publish.slug";
    /// <summary>زمان‌بندی.</summary>
    public const string Schedule = "content.publish.schedule";
    /// <summary>غیربایگانی.</summary>
    public const string NotArchived = "content.publish.not_archived";
    /// <summary>انتشار به‌خاطر الزامات اجباری ناقص رد شد.</summary>
    public const string NotReady = "content.publish.not_ready";
    /// <summary>زمان‌بندی انتشار نامعتبر است.</summary>
    public const string InvalidSchedule = "content.publish.invalid_schedule";
    /// <summary>پیش‌نمایش در دسترس نیست.</summary>
    public const string PreviewUnavailable = "content.preview.unavailable";
    /// <summary>انتشار برای وضعیت فعلی مجاز نیست.</summary>
    public const string PublishForbidden = "content.publish.forbidden";
    /// <summary>لغو انتشار برای وضعیت فعلی مجاز نیست.</summary>
    public const string UnpublishInvalid = "content.unpublish.invalid_state";
}

/// <summary>
/// منبع واحد قوانین آمادگی انتشار مقاله.
/// الزامی‌ها مطابق نیاز واقعی عمومی/سیاست Content؛ تزئینی‌ها فقط توصیه.
/// </summary>
public static class ArticlePublicationReadinessRules
{
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SlugRegex = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>آمادگی را از ورودی پایدار ارزیابی می‌کند.</summary>
    public static ArticlePublicationReadiness Evaluate(ArticlePublicationReadinessInput input)
    {
        var checks = new List<ArticlePublicationCheck>
        {
            Check(
                ArticlePublicationCodes.NotArchived,
                "content.publish.check.not_archived",
                required: true,
                satisfied: input.Status != ContentPublicationStatus.Archived,
                detail: input.Status == ContentPublicationStatus.Archived
                    ? ContentArticleErrorCodes.AlreadyArchived
                    : null,
                actionTarget: "publication"),
            Check(
                ArticlePublicationCodes.Title,
                "content.publish.check.title",
                required: true,
                satisfied: !string.IsNullOrWhiteSpace(input.Title),
                actionTarget: "general"),
            Check(
                ArticlePublicationCodes.Excerpt,
                "content.publish.check.excerpt",
                required: true,
                satisfied: !string.IsNullOrWhiteSpace(input.Excerpt),
                actionTarget: "content"),
            Check(
                ArticlePublicationCodes.Body,
                "content.publish.check.body",
                required: true,
                satisfied: HasMeaningfulBody(input.Body),
                actionTarget: "content"),
            Check(
                ArticlePublicationCodes.Author,
                "content.publish.check.author",
                required: true,
                satisfied: input.AuthorId is not null,
                detail: input.AuthorId is null ? ContentAuthorErrorCodes.RequiredForPublish : null,
                actionTarget: "author"),
            Check(
                ArticlePublicationCodes.LanguageActive,
                "content.publish.check.language_active",
                required: true,
                satisfied: input.LanguageIsActive && !string.IsNullOrWhiteSpace(input.Locale),
                detail: input.LanguageIsActive ? null : "localization.language.inactive",
                actionTarget: "general"),
            Check(
                ArticlePublicationCodes.Slug,
                "content.publish.check.slug",
                required: true,
                satisfied: IsValidSlug(input.Slug),
                actionTarget: "general"),
            Check(
                ArticlePublicationCodes.Schedule,
                "content.publish.check.schedule",
                required: true,
                satisfied: IsValidSchedule(input.PublishDate),
                detail: IsValidSchedule(input.PublishDate) ? null : ArticlePublicationCodes.InvalidSchedule,
                actionTarget: "publication"),
            Check(
                ArticlePublicationCodes.Category,
                "content.publish.check.category",
                required: false,
                satisfied: input.CategoryId is not null,
                actionTarget: "categories"),
            Check(
                ArticlePublicationCodes.FeaturedImage,
                "content.publish.check.featured_image",
                required: false,
                satisfied: input.CoverMediaAssetId is not null,
                actionTarget: "media"),
            Check(
                ArticlePublicationCodes.SeoTitle,
                "content.publish.check.seo_title",
                required: false,
                satisfied: !string.IsNullOrWhiteSpace(input.SeoTitle),
                actionTarget: "seo"),
            Check(
                ArticlePublicationCodes.SeoDescription,
                "content.publish.check.seo_description",
                required: false,
                satisfied: !string.IsNullOrWhiteSpace(input.SeoDescription),
                actionTarget: "seo"),
            Check(
                ArticlePublicationCodes.SeoImage,
                "content.publish.check.seo_image",
                required: false,
                satisfied: input.SeoImageMediaAssetId is not null || input.CoverMediaAssetId is not null,
                actionTarget: "seo"),
        };

        var requiredMissing = checks.Where(c => c.Required && !c.Satisfied).ToList();
        var recommendedMissing = checks.Where(c => !c.Required && !c.Satisfied).ToList();
        var canPublish = requiredMissing.Count == 0;
        var satisfiedCount = checks.Count(c => c.Satisfied);
        var score = checks.Count == 0 ? 100 : (int)Math.Round(100.0 * satisfiedCount / checks.Count);

        return new ArticlePublicationReadiness(
            canPublish,
            checks,
            requiredMissing,
            recommendedMissing,
            score);
    }

    /// <summary>آیا بدنه پس از حذف تگ‌های HTML محتوای معنادار دارد.</summary>
    public static bool HasMeaningfulBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        var text = HtmlTagRegex.Replace(body, " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        return !string.IsNullOrWhiteSpace(text);
    }

    private static bool IsValidSlug(string? slug) =>
        !string.IsNullOrWhiteSpace(slug)
        && slug.Length <= ContentArticle.SlugMaxLength
        && SlugRegex.IsMatch(slug.Trim());

    /// <summary>
    /// زمان‌بندی باید در بازهٔ معقول UTC باشد.
    /// تاریخ گذشته برای انتشار فوری مجاز است؛ فقط مقادیر خراب/حداقل رد می‌شوند.
    /// </summary>
    private static bool IsValidSchedule(DateTimeOffset publishDate) =>
        publishDate > DateTimeOffset.UnixEpoch
        && publishDate.Year is >= 2000 and <= 2100;

    private static ArticlePublicationCheck Check(
        string key,
        string labelKey,
        bool required,
        bool satisfied,
        string? detail = null,
        string? actionTarget = null) =>
        new(key, labelKey, required, satisfied, detail, actionTarget);
}
