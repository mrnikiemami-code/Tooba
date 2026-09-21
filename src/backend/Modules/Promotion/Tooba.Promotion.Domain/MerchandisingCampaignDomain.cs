using Tooba.BuildingBlocks;

namespace Tooba.Promotion.Domain;

/// <summary>
/// وضعیت چرخهٔ عمر کمپین مرچندایزینگ فروشگاهی. با <see cref="PromotionStatus"/> تسویه یکی نیست.
/// </summary>
public enum MerchandisingCampaignLifecycleStatus
{
    /// <summary>
    /// پیش‌نویس؛ هرگز در زمان اجرا فعال نیست.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// منتشرشده؛ فقط داخل پنجرهٔ StartAt/EndAt فعال محسوب می‌شود.
    /// </summary>
    Published = 1,

    /// <summary>
    /// بایگانی؛ هرگز فعال نیست.
    /// </summary>
    Archived = 2,
}

/// <summary>
/// نقطهٔ توسعه‌ٔ رزروشده برای قیمت پروموشن کمپین.
/// ذخیره‌سازی قیمت کمپین عمداً به تعویق افتاده است: <c>AuthoredPrice.QualifierKind</c>
/// فعلاً فقط Base است و بدون بازطراحی Pricing نمی‌توان qualifier کمپین افزود.
/// اسکالر <c>PromoAmount</c> بدون ارز/ابعاد ممنوع است.
/// </summary>
public interface IMerchandisingCampaignPromoPrice
{
}

/// <summary>
/// گونهٔ مرجع مرچندایزینگ. هویت پایدار ماشین با Code است نه enum ذخیره‌شده.
/// </summary>
public sealed class MerchandisingPromotionType
{
    /// <summary>کد سیستمی پیشنهاد شگفت‌انگیز.</summary>
    public const string AmazingCode = "AMAZING";

    /// <summary>سازندهٔ EF.</summary>
    private MerchandisingPromotionType()
    {
    }

    /// <summary>شناسهٔ UuidV7.</summary>
    public Guid Id { get; init; }

    /// <summary>کد پایدار ماشین (مثلاً AMAZING).</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>آیا گونهٔ سیستمی است و قابل تغییر/حذف عادی نیست.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>فعال بودن برای استفادهٔ جدید.</summary>
    public bool IsActive { get; private set; }

    /// <summary>ترتیب نمایش مرجع.</summary>
    public int SortOrder { get; private set; }

    /// <summary>ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>به‌روزرسانی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// گونهٔ سیستمی می‌سازد. Code پس از ایجاد برای سیستم قابل تغییر نیست.
    /// </summary>
    public static MerchandisingPromotionType CreateSystem(
        Guid id,
        string code,
        int sortOrder,
        DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.type.id_required");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("promotion.type.code_required");
        }

        return new MerchandisingPromotionType
        {
            Id = id,
            Code = code.Trim().ToUpperInvariant(),
            IsSystem = true,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// گونهٔ غیراجباری می‌سازد.
    /// </summary>
    public static MerchandisingPromotionType Create(
        Guid id,
        string code,
        int sortOrder,
        bool isActive,
        DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.type.id_required");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("promotion.type.code_required");
        }

        return new MerchandisingPromotionType
        {
            Id = id,
            Code = code.Trim().ToUpperInvariant(),
            IsSystem = false,
            IsActive = isActive,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// کد را عوض می‌کند. گونه‌های سیستمی قابل تغییر کد نیستند.
    /// </summary>
    public void RenameCode(string newCode, DateTimeOffset now)
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("promotion.type.system_code_immutable");
        }

        if (string.IsNullOrWhiteSpace(newCode))
        {
            throw new InvalidOperationException("promotion.type.code_required");
        }

        Code = newCode.Trim().ToUpperInvariant();
        UpdatedAt = now;
    }

    /// <summary>
    /// حذف منطقی را برای گونهٔ سیستمی رد می‌کند.
    /// </summary>
    public void EnsureCanDelete()
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("promotion.type.system_delete_forbidden");
        }
    }

    /// <summary>
    /// ترتیب یا فعال بودن را به‌روز می‌کند بدون دست زدن به Code سیستمی.
    /// </summary>
    public void UpdateMeta(int sortOrder, bool isActive, DateTimeOffset now)
    {
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = now;
    }
}

/// <summary>
/// ترجمهٔ نمایشی گونهٔ مرچندایزینگ. Locale از Market جداست.
/// </summary>
public sealed class MerchandisingPromotionTypeTranslation
{
    /// <summary>سازندهٔ EF.</summary>
    private MerchandisingPromotionTypeTranslation()
    {
    }

    /// <summary>گونه.</summary>
    public Guid TypeId { get; init; }

    /// <summary>locale نرمال‌شده (مثلاً fa-IR یا en-US).</summary>
    public string Locale { get; init; } = string.Empty;

    /// <summary>نام نمایشی.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// ترجمه می‌سازد.
    /// </summary>
    public static MerchandisingPromotionTypeTranslation Create(
        Guid typeId,
        string locale,
        string displayName)
    {
        if (typeId == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.type.id_required");
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            throw new InvalidOperationException("promotion.translation.locale_required");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("promotion.translation.name_required");
        }

        return new MerchandisingPromotionTypeTranslation
        {
            TypeId = typeId,
            Locale = locale.Trim(),
            DisplayName = displayName.Trim(),
        };
    }

    /// <summary>
    /// نام نمایشی را عوض می‌کند.
    /// </summary>
    public void SetDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("promotion.translation.name_required");
        }

        DisplayName = displayName.Trim();
    }
}

/// <summary>
/// کمپین مرچندایزینگ فروشگاهی؛ جدا از تعریف تخفیف تسویه (<see cref="PromotionDefinition"/>).
/// </summary>
public sealed class MerchandisingCampaign
{
    /// <summary>سازندهٔ EF.</summary>
    private MerchandisingCampaign()
    {
    }

    /// <summary>شناسهٔ UuidV7.</summary>
    public Guid Id { get; init; }

    /// <summary>گونهٔ مرچندایزینگ.</summary>
    public Guid PromotionTypeId { get; private set; }

    /// <summary>محدودهٔ فروشگاه.</summary>
    public Guid StoreId { get; init; }

    /// <summary>وضعیت چرخهٔ عمر.</summary>
    public MerchandisingCampaignLifecycleStatus LifecycleStatus { get; private set; }

    /// <summary>شروع پنجرهٔ UTC.</summary>
    public DateTimeOffset StartAt { get; private set; }

    /// <summary>پایان اختیاری UTC؛ تهی یعنی باز.</summary>
    public DateTimeOffset? EndAt { get; private set; }

    /// <summary>اولویت بین کمپین‌های هم‌نوع؛ عدد بزرگ‌تر زودتر.</summary>
    public int Priority { get; private set; }

    /// <summary>ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>به‌روزرسانی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// کمپین پیش‌نویس می‌سازد.
    /// </summary>
    public static MerchandisingCampaign Create(
        Guid id,
        Guid promotionTypeId,
        Guid storeId,
        DateTimeOffset startAt,
        DateTimeOffset? endAt,
        int priority,
        DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.campaign.id_required");
        }

        if (promotionTypeId == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.campaign.type_required");
        }

        if (storeId == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.campaign.store_required");
        }

        ValidateWindow(startAt, endAt);

        return new MerchandisingCampaign
        {
            Id = id,
            PromotionTypeId = promotionTypeId,
            StoreId = storeId,
            LifecycleStatus = MerchandisingCampaignLifecycleStatus.Draft,
            StartAt = startAt,
            EndAt = endAt,
            Priority = priority,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// وضعیت چرخهٔ عمر را برای upsert دانهٔ Development تنظیم می‌کند (فقط seed).
    /// </summary>
    public void ForceLifecycleForSeed(MerchandisingCampaignLifecycleStatus status, DateTimeOffset now)
    {
        LifecycleStatus = status;
        UpdatedAt = now;
    }

    /// <summary>
    /// پنجره و اولویت را به‌روز می‌کند.
    /// </summary>
    public void UpdateWindow(DateTimeOffset startAt, DateTimeOffset? endAt, int priority, DateTimeOffset now)
    {
        ValidateWindow(startAt, endAt);
        StartAt = startAt;
        EndAt = endAt;
        Priority = priority;
        UpdatedAt = now;
    }

    /// <summary>
    /// کمپین را منتشر می‌کند.
    /// </summary>
    public void Publish(DateTimeOffset now)
    {
        if (LifecycleStatus == MerchandisingCampaignLifecycleStatus.Archived)
        {
            throw new InvalidOperationException("promotion.campaign.archived_cannot_publish");
        }

        LifecycleStatus = MerchandisingCampaignLifecycleStatus.Published;
        UpdatedAt = now;
    }

    /// <summary>
    /// کمپین را بایگانی می‌کند. عضویت Offer را حذف نمی‌کند مگر فراخوان صریح.
    /// </summary>
    public void Archive(DateTimeOffset now)
    {
        LifecycleStatus = MerchandisingCampaignLifecycleStatus.Archived;
        UpdatedAt = now;
    }

    /// <summary>
    /// آیا در لحظهٔ داده‌شده برای ریل فروشگاهی فعال است (مشتق از وضعیت + پنجره).
    /// </summary>
    public bool IsRuntimeActive(DateTimeOffset now) =>
        LifecycleStatus == MerchandisingCampaignLifecycleStatus.Published
        && now >= StartAt
        && (EndAt is null || now < EndAt);

    private static void ValidateWindow(DateTimeOffset startAt, DateTimeOffset? endAt)
    {
        if (endAt is not null && endAt <= startAt)
        {
            throw new InvalidOperationException("promotion.campaign.window_invalid");
        }
    }
}

/// <summary>
/// ترجمهٔ کمپین. Locale متعلق به بخش صفحه نیست.
/// </summary>
public sealed class MerchandisingCampaignTranslation
{
    /// <summary>سازندهٔ EF.</summary>
    private MerchandisingCampaignTranslation()
    {
    }

    /// <summary>کمپین.</summary>
    public Guid CampaignId { get; init; }

    /// <summary>locale نرمال‌شده.</summary>
    public string Locale { get; init; } = string.Empty;

    /// <summary>عنوان.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>زیرعنوان اختیاری.</summary>
    public string? Subtitle { get; private set; }

    /// <summary>متن بج اختیاری.</summary>
    public string? BadgeText { get; private set; }

    /// <summary>
    /// ترجمه می‌سازد.
    /// </summary>
    public static MerchandisingCampaignTranslation Create(
        Guid campaignId,
        string locale,
        string title,
        string? subtitle,
        string? badgeText)
    {
        if (campaignId == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.campaign.id_required");
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            throw new InvalidOperationException("promotion.translation.locale_required");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("promotion.translation.title_required");
        }

        return new MerchandisingCampaignTranslation
        {
            CampaignId = campaignId,
            Locale = locale.Trim(),
            Title = title.Trim(),
            Subtitle = string.IsNullOrWhiteSpace(subtitle) ? null : subtitle.Trim(),
            BadgeText = string.IsNullOrWhiteSpace(badgeText) ? null : badgeText.Trim(),
        };
    }

    /// <summary>
    /// فیلدهای نمایشی را عوض می‌کند.
    /// </summary>
    public void Upsert(string title, string? subtitle, string? badgeText)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("promotion.translation.title_required");
        }

        Title = title.Trim();
        Subtitle = string.IsNullOrWhiteSpace(subtitle) ? null : subtitle.Trim();
        BadgeText = string.IsNullOrWhiteSpace(badgeText) ? null : badgeText.Trim();
    }
}

/// <summary>
/// عضویت SellerOffer در کمپین. پرچم روی Offer نیست و موجودی/قیمت پایه را مالک نیست.
/// </summary>
public sealed class MerchandisingCampaignOffer
{
    /// <summary>سازندهٔ EF.</summary>
    private MerchandisingCampaignOffer()
    {
    }

    /// <summary>شناسهٔ UuidV7.</summary>
    public Guid Id { get; init; }

    /// <summary>کمپین.</summary>
    public Guid CampaignId { get; init; }

    /// <summary>شناسهٔ SellerOffer؛ بدون FK به schema offer.</summary>
    public Guid SellerOfferId { get; init; }

    /// <summary>ترتیب نمایش داخل کمپین.</summary>
    public int SortOrder { get; private set; }

    /// <summary>ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// عضویت می‌سازد. فیلد تخصیص/سقف کمپین عمداً نیست — سقف سفارش روی Offer است.
    /// </summary>
    public static MerchandisingCampaignOffer Create(
        Guid id,
        Guid campaignId,
        Guid sellerOfferId,
        int sortOrder,
        DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.campaign_offer.id_required");
        }

        if (campaignId == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.campaign.id_required");
        }

        if (sellerOfferId == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.campaign_offer.offer_required");
        }

        return new MerchandisingCampaignOffer
        {
            Id = id,
            CampaignId = campaignId,
            SellerOfferId = sellerOfferId,
            SortOrder = sortOrder,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// ترتیب را عوض می‌کند.
    /// </summary>
    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
