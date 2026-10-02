using System.Security.Cryptography;
using System.Text;
using Tooba.BuildingBlocks;
using Tooba.Story.Contracts.Errors;

using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Rules;

namespace Tooba.Story.Domain.Aggregates;

/// <summary>aggregate استوری فروشگاهی با آیتم‌های رسانه.</summary>
public sealed class Story
{
    private readonly List<StoryItem> _items = [];

    private Story() { }

    /// <summary>شناسهٔ پایدار استوری.</summary>
    public Guid StoryId { get; init; }
    /// <summary>Tenant مالک.</summary>
    public Guid TenantId { get; init; }
    /// <summary>منبع ایجاد استوری.</summary>
    public StoryOrigin Origin { get; private set; }
    /// <summary>Party فروشندهٔ مالک برای استوری‌های Seller.</summary>
    public Guid? SellerPartyId { get; private set; }
    /// <summary>وضعیت بازبینی.</summary>
    public StoryReviewStatus ReviewStatus { get; private set; }
    /// <summary>Actor ارسال‌کننده برای بازبینی.</summary>
    public Guid? SubmittedByActorUserId { get; private set; }
    /// <summary>Actor ادمین بازبین.</summary>
    public Guid? ReviewedByActorUserId { get; private set; }
    /// <summary>زمان ارسال برای بازبینی.</summary>
    public DateTimeOffset? SubmittedAt { get; private set; }
    /// <summary>زمان آخرین بازبینی.</summary>
    public DateTimeOffset? ReviewedAt { get; private set; }
    /// <summary>دلیل رد بازبینی.</summary>
    public string? RejectionReason { get; private set; }
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
    /// <summary>وضعیت انتشار.</summary>
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

    /// <summary>استوری Draft ادمین می‌سازد.</summary>
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
            Origin = StoryOrigin.Admin,
            SellerPartyId = null,
            ReviewStatus = StoryReviewStatus.None,
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

    /// <summary>پیش‌نویس فروشنده می‌سازد.</summary>
    public static Story CreateSellerDraft(
        Guid tenantId,
        Guid sellerPartyId,
        Guid actorUserId,
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
        if (sellerPartyId == Guid.Empty)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        if (actorUserId == Guid.Empty)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));

        ValidateTitle(title);
        ValidateLocale(locale);
        ValidateMarket(market);
        ValidateMediaUrl(coverMediaUrl);
        var (normalizedCtaType, normalizedCtaTarget) = StoryRules.ValidateCta(ctaType, ctaTarget);
        return new Story
        {
            StoryId = UuidV7.New(),
            TenantId = tenantId,
            Origin = StoryOrigin.Seller,
            SellerPartyId = sellerPartyId,
            ReviewStatus = StoryReviewStatus.None,
            SubmittedByActorUserId = actorUserId,
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

    /// <summary>آیا فروشنده مجاز به ویرایش محتوا است.</summary>
    public bool IsSellerContentEditable() =>
        Origin == StoryOrigin.Seller
        && Status == StoryStatus.Draft
        && (ReviewStatus is StoryReviewStatus.None or StoryReviewStatus.Rejected);

    /// <summary>آیا مسیر انتشار (زمان‌بندی/فعال) باز است.</summary>
    public bool IsPublicationEligible() =>
        Origin == StoryOrigin.Admin || ReviewStatus == StoryReviewStatus.Approved;

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
        EnsurePublicationEligible();
        if (startAt.HasValue && endAt.HasValue && endAt.Value <= startAt.Value)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));

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

    /// <summary>استوری را فعال می‌کند؛ فقط پس از واجد شرایط بودن انتشار.</summary>
    public void Activate(DateTimeOffset now)
    {
        EnsurePublicationEligible();
        Status = StoryStatus.Active;
        Touch(now);
    }

    /// <summary>استوری را برای بازبینی ارسال می‌کند.</summary>
    public void SubmitForReview(Guid actorUserId, DateTimeOffset now)
    {
        if (Origin != StoryOrigin.Seller)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        if (actorUserId == Guid.Empty)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        if (Status != StoryStatus.Draft)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        if (ReviewStatus is not (StoryReviewStatus.None or StoryReviewStatus.Rejected))
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));

        ReviewStatus = StoryReviewStatus.Submitted;
        SubmittedByActorUserId = actorUserId;
        SubmittedAt = now;
        RejectionReason = null;
        ReviewedByActorUserId = null;
        ReviewedAt = null;
        Touch(now);
    }

    /// <summary>استوری را تأیید می‌کند؛ فراخوانی تکراری امن است.</summary>
    public void Approve(Guid adminActorUserId, DateTimeOffset now)
    {
        if (adminActorUserId == Guid.Empty)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        if (Origin != StoryOrigin.Seller)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));

        if (ReviewStatus == StoryReviewStatus.Approved)
        {
            ReviewedByActorUserId ??= adminActorUserId;
            ReviewedAt ??= now;
            Touch(now);
            return;
        }

        if (ReviewStatus != StoryReviewStatus.Submitted)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));

        ReviewStatus = StoryReviewStatus.Approved;
        ReviewedByActorUserId = adminActorUserId;
        ReviewedAt = now;
        RejectionReason = null;
        Touch(now);
    }

    /// <summary>استوری را با دلیل رد می‌کند.</summary>
    public void Reject(Guid adminActorUserId, string reason, DateTimeOffset now)
    {
        if (adminActorUserId == Guid.Empty)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        if (Origin != StoryOrigin.Seller)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        if (ReviewStatus != StoryReviewStatus.Submitted)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));

        var normalized = ValidateRejectionReason(reason);
        ReviewStatus = StoryReviewStatus.Rejected;
        ReviewedByActorUserId = adminActorUserId;
        ReviewedAt = now;
        RejectionReason = normalized;
        Status = StoryStatus.Draft;
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
        IsPublicationEligible()
        && Status == StoryStatus.Active
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
            throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));
        _items.RemoveAt(index);
        ReindexItems(now);
        Touch(now);
    }

    /// <summary>آیتم‌ها را با شناسه‌های داده‌شده مرتب می‌کند.</summary>
    public void ReorderItems(IReadOnlyList<Guid> itemIdsInOrder, DateTimeOffset now)
    {
        if (itemIdsInOrder.Count != _items.Count)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        if (itemIdsInOrder.Distinct().Count() != itemIdsInOrder.Count)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));

        var lookup = _items.ToDictionary(item => item.StoryItemId);
        for (var index = 0; index < itemIdsInOrder.Count; index++)
        {
            if (!lookup.TryGetValue(itemIdsInOrder[index], out var item))
                throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));
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

    private void EnsurePublicationEligible()
    {
        if (!IsPublicationEligible())
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
    }

    private static string ValidateRejectionReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        var trimmed = reason.Trim();
        if (trimmed.Length > StoryRules.RejectionReasonMaxLength)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        return trimmed;
    }

    private StoryItem RequireItem(Guid storyItemId) =>
        _items.FirstOrDefault(item => item.StoryItemId == storyItemId)
        ?? throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));

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
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
    }

    private static void ValidateLocale(string? locale)
    {
        if (locale is not null && (locale.Trim().Length == 0 || locale.Trim().Length > StoryRules.LocaleMaxLength))
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
    }

    private static void ValidateMarket(string? market)
    {
        if (market is not null && (market.Trim().Length == 0 || market.Trim().Length > StoryRules.MarketMaxLength))
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
    }

    private static void ValidateMediaUrl(string? mediaUrl)
    {
        if (mediaUrl is not null && (mediaUrl.Trim().Length == 0 || mediaUrl.Trim().Length > StoryRules.MediaUrlMaxLength))
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        return trimmed;
    }
}
