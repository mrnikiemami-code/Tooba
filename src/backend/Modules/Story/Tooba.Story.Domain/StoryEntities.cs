using System.Security.Cryptography;
using System.Text;
using Tooba.BuildingBlocks;

namespace Tooba.Story.Domain;

/// <summary>وضعیت چرخهٔ عمر استوری.</summary>
public enum StoryStatus
{
    /// <summary>پیش‌نویس و غیرقابل نمایش عمومی.</summary>
    Draft = 0,
    /// <summary>زمان‌بندی‌شده برای آینده.</summary>
    Scheduled = 1,
    /// <summary>فعال برای نمایش عمومی در بازهٔ زمانی.</summary>
    Active = 2,
    /// <summary>منقضی‌شده.</summary>
    Expired = 3,
    /// <summary>غیرفعال دستی.</summary>
    Disabled = 4,
}

/// <summary>شناسهٔ پایدار Tenant برای ماژول Story.</summary>
public static class StoryTenantIds
{
    /// <summary>Tenant توسعهٔ store-alpha.</summary>
    public static readonly Guid StoreAlpha = Guid.Parse("a0000000-0001-4000-8000-000000000001");

    /// <summary>Tenant توسعهٔ store-beta برای تست جداسازی.</summary>
    public static readonly Guid StoreBeta = Guid.Parse("a0000000-0002-4000-8000-000000000002");

    /// <summary>کلید Tenant پیکربندی‌شده را به Guid پایدار نگاشت می‌کند.</summary>
    public static Guid FromTenantKey(string tenantKey)
    {
        if (string.IsNullOrWhiteSpace(tenantKey))
            throw new InvalidOperationException("TenantId معتبر نیست.");

        if (string.Equals(tenantKey, "store-alpha", StringComparison.Ordinal))
            return StoreAlpha;
        if (string.Equals(tenantKey, "store-beta", StringComparison.Ordinal))
            return StoreBeta;

        var payload = Encoding.UTF8.GetBytes($"tooba:story:{tenantKey.Trim()}");
        var hash = SHA256.HashData(payload);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x40);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }
}

/// <summary>ثابت‌ها و اعتبارسنجی CTA و رسانهٔ استوری.</summary>
public static class StoryRules
{
    /// <summary>حداکثر طول عنوان.</summary>
    public const int TitleMaxLength = 120;
    /// <summary>حداکثر طول locale.</summary>
    public const int LocaleMaxLength = 16;
    /// <summary>حداکثر طول market.</summary>
    public const int MarketMaxLength = 32;
    /// <summary>حداکثر طول URL رسانه.</summary>
    public const int MediaUrlMaxLength = 512;
    /// <summary>حداکثر طول نوع CTA.</summary>
    public const int CtaTypeMaxLength = 32;
    /// <summary>حداکثر طول هدف CTA.</summary>
    public const int CtaTargetMaxLength = 512;
    /// <summary>حداکثر طول caption آیتم.</summary>
    public const int CaptionMaxLength = 200;
    /// <summary>حداکثر طول نوع رسانه.</summary>
    public const int MediaTypeMaxLength = 16;

    /// <summary>نوع CTA بدون لینک.</summary>
    public const string CtaNone = "none";
    /// <summary>نوع رسانه تصویر.</summary>
    public const string MediaImage = "image";
    /// <summary>نوع رسانه ویدیو.</summary>
    public const string MediaVideo = "video";

    private static readonly HashSet<string> AllowedCtaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        CtaNone,
        "product",
        "category",
        "article",
        "internal",
        "external",
    };

    private static readonly HashSet<string> AllowedMediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaImage,
        MediaVideo,
    };

    private static readonly string[] ForbiddenCtaSchemes =
    [
        "javascript:",
        "data:",
        "vbscript:",
    ];

    /// <summary>نوع و هدف CTA را اعتبارسنجی و نرمال می‌کند.</summary>
    public static (string CtaType, string? CtaTarget) ValidateCta(string? ctaType, string? ctaTarget)
    {
        var normalizedType = string.IsNullOrWhiteSpace(ctaType) ? CtaNone : ctaType.Trim().ToLowerInvariant();
        if (normalizedType.Length > CtaTypeMaxLength || !AllowedCtaTypes.Contains(normalizedType))
            throw new InvalidOperationException("نوع CTA استوری مجاز نیست.");

        if (string.Equals(normalizedType, CtaNone, StringComparison.Ordinal))
            return (CtaNone, null);

        if (string.IsNullOrWhiteSpace(ctaTarget))
            throw new InvalidOperationException("هدف CTA برای این نوع الزامی است.");

        var normalizedTarget = ctaTarget.Trim();
        if (normalizedTarget.Length > CtaTargetMaxLength)
            throw new InvalidOperationException("هدف CTA از سقف مجاز بلندتر است.");

        foreach (var scheme in ForbiddenCtaSchemes)
        {
            if (normalizedTarget.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("هدف CTA ناامن است.");
        }

        return (normalizedType, normalizedTarget);
    }

    /// <summary>نوع رسانه را اعتبارسنجی می‌کند.</summary>
    public static string ValidateMediaType(string mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType) || !AllowedMediaTypes.Contains(mediaType.Trim()))
            throw new InvalidOperationException("نوع رسانهٔ استوری مجاز نیست.");
        return mediaType.Trim().ToLowerInvariant();
    }

    /// <summary>آیا locale استوری با locale درخواست عمومی سازگار است.</summary>
    public static bool MatchesLocale(string? storyLocale, string? requestLocale)
    {
        if (string.IsNullOrWhiteSpace(storyLocale))
            return true;
        if (string.IsNullOrWhiteSpace(requestLocale))
            return true;

        var story = storyLocale.Trim();
        var request = requestLocale.Trim();
        if (string.Equals(story, request, StringComparison.OrdinalIgnoreCase))
            return true;

        static string Language(string value)
        {
            var dash = value.IndexOf('-');
            return dash < 0 ? value : value[..dash];
        }

        return string.Equals(Language(story), Language(request), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>آیا market استوری با فیلتر درخواست سازگار است.</summary>
    public static bool MatchesMarket(string? storyMarket, string? requestMarket)
    {
        if (string.IsNullOrWhiteSpace(requestMarket))
            return true;
        if (string.IsNullOrWhiteSpace(storyMarket))
            return true;
        return string.Equals(storyMarket.Trim(), requestMarket.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>aggregate استوری فروشگاهی با آیتم‌های رسانه.</summary>
public sealed class Story
{
    private readonly List<StoryItem> _items = [];

    private Story() { }

    /// <summary>شناسهٔ پایدار استوری.</summary>
    public Guid StoryId { get; init; }
    /// <summary>Tenant مالک.</summary>
    public Guid TenantId { get; init; }
    /// <summary>locale اختیاری؛ null یعنی همهٔ localeها.</summary>
    public string? Locale { get; private set; }
    /// <summary>بازار اختیاری؛ از locale استنباط نمی‌شود.</summary>
    public string? Market { get; private set; }
    /// <summary>برچسب ریل استوری.</summary>
    public string Title { get; private set; } = string.Empty;
    /// <summary>مرجع مات رسانهٔ جلد.</summary>
    public Guid? CoverMediaAssetId { get; private set; }
    /// <summary>URL ایستا یا سرو شدهٔ جلد.</summary>
    public string? CoverMediaUrl { get; private set; }
    /// <summary>ترتیب نمایش در ریل.</summary>
    public int DisplayOrder { get; private set; }
    /// <summary>شروع نمایش اختیاری.</summary>
    public DateTimeOffset? StartAt { get; private set; }
    /// <summary>پایان نمایش اختیاری.</summary>
    public DateTimeOffset? EndAt { get; private set; }
    /// <summary>وضعیت چرخهٔ عمر.</summary>
    public StoryStatus Status { get; private set; }
    /// <summary>نوع CTA سطح استوری.</summary>
    public string CtaType { get; private set; } = StoryRules.CtaNone;
    /// <summary>هدف CTA سطح استوری.</summary>
    public string? CtaTarget { get; private set; }
    /// <summary>توکن همزمانی.</summary>
    public int VersionToken { get; private set; }
    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>زمان آخرین به‌روزرسانی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }
    /// <summary>آیتم‌های استوری.</summary>
    public IReadOnlyCollection<StoryItem> Items => _items;

    /// <summary>استوری Draft جدید می‌سازد.</summary>
    public static Story CreateDraft(
        Guid tenantId,
        string title,
        int displayOrder,
        DateTimeOffset now,
        string? locale = null,
        string? market = null,
        Guid? coverMediaAssetId = null,
        string? coverMediaUrl = null,
        string? ctaType = null,
        string? ctaTarget = null)
    {
        ValidateTitle(title);
        ValidateLocale(locale);
        ValidateMarket(market);
        ValidateMediaUrl(coverMediaUrl);
        var (normalizedCtaType, normalizedCtaTarget) = StoryRules.ValidateCta(ctaType, ctaTarget);
        return new Story
        {
            StoryId = UuidV7.New(),
            TenantId = tenantId,
            Locale = NormalizeOptional(locale, StoryRules.LocaleMaxLength),
            Market = NormalizeOptional(market, StoryRules.MarketMaxLength),
            Title = title.Trim(),
            CoverMediaAssetId = coverMediaAssetId,
            CoverMediaUrl = NormalizeOptional(coverMediaUrl, StoryRules.MediaUrlMaxLength),
            DisplayOrder = displayOrder,
            Status = StoryStatus.Draft,
            CtaType = normalizedCtaType,
            CtaTarget = normalizedCtaTarget,
            VersionToken = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>فیلدهای سطح استوری را به‌روزرسانی می‌کند.</summary>
    public void Update(
        string title,
        string? locale,
        string? market,
        Guid? coverMediaAssetId,
        string? coverMediaUrl,
        string? ctaType,
        string? ctaTarget,
        DateTimeOffset now)
    {
        ValidateTitle(title);
        ValidateLocale(locale);
        ValidateMarket(market);
        ValidateMediaUrl(coverMediaUrl);
        var (normalizedCtaType, normalizedCtaTarget) = StoryRules.ValidateCta(ctaType, ctaTarget);
        Title = title.Trim();
        Locale = NormalizeOptional(locale, StoryRules.LocaleMaxLength);
        Market = NormalizeOptional(market, StoryRules.MarketMaxLength);
        CoverMediaAssetId = coverMediaAssetId;
        CoverMediaUrl = NormalizeOptional(coverMediaUrl, StoryRules.MediaUrlMaxLength);
        CtaType = normalizedCtaType;
        CtaTarget = normalizedCtaTarget;
        Touch(now);
    }

    /// <summary>زمان‌بندی را تنظیم و وضعیت Scheduled یا Active را بر اساس now تعیین می‌کند.</summary>
    public void SetSchedule(DateTimeOffset? startAt, DateTimeOffset? endAt, DateTimeOffset now)
    {
        if (startAt.HasValue && endAt.HasValue && endAt.Value <= startAt.Value)
            throw new InvalidOperationException("بازهٔ زمانی استوری معتبر نیست.");

        StartAt = startAt;
        EndAt = endAt;
        if (endAt.HasValue && endAt.Value <= now)
            Status = StoryStatus.Expired;
        else if (startAt.HasValue && startAt.Value > now)
            Status = StoryStatus.Scheduled;
        else
            Status = StoryStatus.Active;
        Touch(now);
    }

    /// <summary>استوری را فعال می‌کند.</summary>
    public void Activate(DateTimeOffset now)
    {
        Status = StoryStatus.Active;
        Touch(now);
    }

    /// <summary>استوری را غیرفعال می‌کند.</summary>
    public void Disable(DateTimeOffset now)
    {
        Status = StoryStatus.Disabled;
        Touch(now);
    }

    /// <summary>استوری را منقضی علامت می‌زند.</summary>
    public void MarkExpired(DateTimeOffset now)
    {
        Status = StoryStatus.Expired;
        Touch(now);
    }

    /// <summary>ترتیب نمایش سطح استوری را تنظیم می‌کند.</summary>
    public void SetDisplayOrder(int displayOrder, DateTimeOffset now)
    {
        DisplayOrder = displayOrder;
        Touch(now);
    }

    /// <summary>آیا استوری در زمان داده‌شده برای عموم قابل نمایش است.</summary>
    public bool IsPubliclyVisible(DateTimeOffset now) =>
        Status == StoryStatus.Active
        && (StartAt is null || StartAt <= now)
        && (EndAt is null || EndAt > now);

    /// <summary>آیتم رسانهٔ جدید اضافه می‌کند.</summary>
    public StoryItem AddItem(
        string mediaType,
        int displayOrder,
        DateTimeOffset now,
        Guid? mediaAssetId = null,
        string? mediaUrl = null,
        string? caption = null,
        int? durationMs = null,
        string? ctaType = null,
        string? ctaTarget = null)
    {
        var item = StoryItem.Create(
            StoryId,
            mediaType,
            displayOrder,
            now,
            mediaAssetId,
            mediaUrl,
            caption,
            durationMs,
            ctaType,
            ctaTarget);
        _items.Add(item);
        Touch(now);
        return item;
    }

    /// <summary>آیتم موجود را به‌روزرسانی می‌کند.</summary>
    public void UpdateItem(
        Guid storyItemId,
        string mediaType,
        Guid? mediaAssetId,
        string? mediaUrl,
        string? caption,
        int? durationMs,
        string? ctaType,
        string? ctaTarget,
        DateTimeOffset now)
    {
        var item = RequireItem(storyItemId);
        item.Update(mediaType, mediaAssetId, mediaUrl, caption, durationMs, ctaType, ctaTarget, now);
        Touch(now);
    }

    /// <summary>آیتم را حذف می‌کند.</summary>
    public void RemoveItem(Guid storyItemId, DateTimeOffset now)
    {
        var index = _items.FindIndex(item => item.StoryItemId == storyItemId);
        if (index < 0)
            throw new InvalidOperationException("آیتم استوری یافت نشد.");
        _items.RemoveAt(index);
        ReindexItems(now);
        Touch(now);
    }

    /// <summary>آیتم‌ها را با شناسه‌های داده‌شده مرتب می‌کند.</summary>
    public void ReorderItems(IReadOnlyList<Guid> itemIdsInOrder, DateTimeOffset now)
    {
        if (itemIdsInOrder.Count != _items.Count)
            throw new InvalidOperationException("ترتیب آیتم با تعداد فعلی هم‌خوان نیست.");
        if (itemIdsInOrder.Distinct().Count() != itemIdsInOrder.Count)
            throw new InvalidOperationException("شناسهٔ آیتم تکراری در ترتیب وجود دارد.");

        var lookup = _items.ToDictionary(item => item.StoryItemId);
        for (var index = 0; index < itemIdsInOrder.Count; index++)
        {
            if (!lookup.TryGetValue(itemIdsInOrder[index], out var item))
                throw new InvalidOperationException("آیتم استوری برای مرتب‌سازی یافت نشد.");
            item.SetDisplayOrder(index, now);
        }

        _items.Sort((left, right) => left.DisplayOrder.CompareTo(right.DisplayOrder));
        Touch(now);
    }

    /// <summary>آیتم‌های بارگذاری‌شده را به aggregate متصل می‌کند.</summary>
    public void AttachItems(IEnumerable<StoryItem> items)
    {
        _items.Clear();
        _items.AddRange(items.OrderBy(item => item.DisplayOrder));
    }

    private StoryItem RequireItem(Guid storyItemId) =>
        _items.FirstOrDefault(item => item.StoryItemId == storyItemId)
        ?? throw new InvalidOperationException("آیتم استوری یافت نشد.");

    private void ReindexItems(DateTimeOffset now)
    {
        var ordered = _items.OrderBy(item => item.DisplayOrder).ToList();
        for (var index = 0; index < ordered.Count; index++)
            ordered[index].SetDisplayOrder(index, now);
    }

    private void Touch(DateTimeOffset now)
    {
        VersionToken++;
        UpdatedAt = now;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > StoryRules.TitleMaxLength)
            throw new InvalidOperationException("عنوان استوری معتبر نیست.");
    }

    private static void ValidateLocale(string? locale)
    {
        if (locale is not null && (locale.Trim().Length == 0 || locale.Trim().Length > StoryRules.LocaleMaxLength))
            throw new InvalidOperationException("locale استوری معتبر نیست.");
    }

    private static void ValidateMarket(string? market)
    {
        if (market is not null && (market.Trim().Length == 0 || market.Trim().Length > StoryRules.MarketMaxLength))
            throw new InvalidOperationException("market استوری معتبر نیست.");
    }

    private static void ValidateMediaUrl(string? mediaUrl)
    {
        if (mediaUrl is not null && (mediaUrl.Trim().Length == 0 || mediaUrl.Trim().Length > StoryRules.MediaUrlMaxLength))
            throw new InvalidOperationException("URL رسانهٔ استوری معتبر نیست.");
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new InvalidOperationException("مقدار اختیاری استوری از سقف مجاز بلندتر است.");
        return trimmed;
    }
}

/// <summary>یک اسلاید رسانه در استوری.</summary>
public sealed class StoryItem
{
    private StoryItem() { }

    /// <summary>شناسهٔ پایدار آیتم.</summary>
    public Guid StoryItemId { get; init; }
    /// <summary>شناسهٔ استوری والد.</summary>
    public Guid StoryId { get; init; }
    /// <summary>ترتیب نمایش.</summary>
    public int DisplayOrder { get; private set; }
    /// <summary>نوع رسانه image یا video.</summary>
    public string MediaType { get; private set; } = StoryRules.MediaImage;
    /// <summary>مرجع مات رسانه.</summary>
    public Guid? MediaAssetId { get; private set; }
    /// <summary>URL ایستا یا سرو شده.</summary>
    public string? MediaUrl { get; private set; }
    /// <summary>زیرنویس اختیاری.</summary>
    public string? Caption { get; private set; }
    /// <summary>مدت نمایش اختیاری به میلی‌ثانیه.</summary>
    public int? DurationMs { get; private set; }
    /// <summary>نوع CTA آیتم.</summary>
    public string CtaType { get; private set; } = StoryRules.CtaNone;
    /// <summary>هدف CTA آیتم.</summary>
    public string? CtaTarget { get; private set; }
    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>زمان آخرین به‌روزرسانی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>آیتم جدید می‌سازد.</summary>
    internal static StoryItem Create(
        Guid storyId,
        string mediaType,
        int displayOrder,
        DateTimeOffset now,
        Guid? mediaAssetId,
        string? mediaUrl,
        string? caption,
        int? durationMs,
        string? ctaType,
        string? ctaTarget)
    {
        var normalizedMediaType = StoryRules.ValidateMediaType(mediaType);
        ValidateMediaUrl(mediaUrl);
        ValidateCaption(caption);
        ValidateDuration(durationMs);
        var (normalizedCtaType, normalizedCtaTarget) = StoryRules.ValidateCta(ctaType, ctaTarget);
        return new StoryItem
        {
            StoryItemId = UuidV7.New(),
            StoryId = storyId,
            DisplayOrder = displayOrder,
            MediaType = normalizedMediaType,
            MediaAssetId = mediaAssetId,
            MediaUrl = NormalizeOptional(mediaUrl),
            Caption = NormalizeOptional(caption),
            DurationMs = durationMs,
            CtaType = normalizedCtaType,
            CtaTarget = normalizedCtaTarget,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>فیلدهای آیتم را به‌روزرسانی می‌کند.</summary>
    internal void Update(
        string mediaType,
        Guid? mediaAssetId,
        string? mediaUrl,
        string? caption,
        int? durationMs,
        string? ctaType,
        string? ctaTarget,
        DateTimeOffset now)
    {
        MediaType = StoryRules.ValidateMediaType(mediaType);
        ValidateMediaUrl(mediaUrl);
        ValidateCaption(caption);
        ValidateDuration(durationMs);
        var (normalizedCtaType, normalizedCtaTarget) = StoryRules.ValidateCta(ctaType, ctaTarget);
        MediaAssetId = mediaAssetId;
        MediaUrl = NormalizeOptional(mediaUrl);
        Caption = NormalizeOptional(caption);
        DurationMs = durationMs;
        CtaType = normalizedCtaType;
        CtaTarget = normalizedCtaTarget;
        UpdatedAt = now;
    }

    /// <summary>ترتیب نمایش را تنظیم می‌کند.</summary>
    internal void SetDisplayOrder(int displayOrder, DateTimeOffset now)
    {
        DisplayOrder = displayOrder;
        UpdatedAt = now;
    }

    private static void ValidateMediaUrl(string? mediaUrl)
    {
        if (mediaUrl is not null && (mediaUrl.Trim().Length == 0 || mediaUrl.Trim().Length > StoryRules.MediaUrlMaxLength))
            throw new InvalidOperationException("URL رسانهٔ آیتم معتبر نیست.");
    }

    private static void ValidateCaption(string? caption)
    {
        if (caption is not null && caption.Trim().Length > StoryRules.CaptionMaxLength)
            throw new InvalidOperationException("caption آیتم از سقف مجاز بلندتر است.");
    }

    private static void ValidateDuration(int? durationMs)
    {
        if (durationMs is < 0)
            throw new InvalidOperationException("مدت نمایش آیتم معتبر نیست.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
