using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain;

/// <summary>آیتم درختی منو با عمق کران‌دار.</summary>
public sealed class StoreMenuItem
{
    /// <summary>حداکثر عمق L1..L3.</summary>
    public const int MaxDepth = 3;

    /// <summary>حداکثر طول برچسب.</summary>
    public const int LabelMaxLength = 120;

    /// <summary>حداکثر طول نشانی خارجی.</summary>
    public const int ExternalUrlMaxLength = 500;

    private StoreMenuItem()
    {
    }

    /// <summary>شناسهٔ پایدار آیتم.</summary>
    public Guid MenuItemId { get; init; }

    /// <summary>منوی مالک.</summary>
    public Guid MenuId { get; init; }

    /// <summary>والد هم‌منو؛ ریشه null است.</summary>
    public Guid? ParentMenuItemId { get; private set; }

    /// <summary>برچسب انسانی.</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>نوع مقصد کنترل‌شده.</summary>
    public StoreMenuLinkType LinkType { get; private set; } = StoreMenuLinkType.Group;

    /// <summary>ارجاع داخلی فروشگاه.</summary>
    public Guid? TargetId { get; private set; }

    /// <summary>نشانی خارجی فقط برای External.</summary>
    public string? ExternalUrl { get; private set; }

    /// <summary>ترتیب میان هم‌سطح‌ها.</summary>
    public int SortOrder { get; private set; }

    /// <summary>فعال در ویرایشگر؛ عمومی فقط اگر زنجیره فعال باشد.</summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>آیتم جدید می‌سازد.</summary>
    public static StoreMenuItem Create(
        Guid menuId,
        Guid? parentMenuItemId,
        string? label,
        StoreMenuLinkType linkType,
        Guid? targetId,
        string? externalUrl,
        int sortOrder,
        DateTimeOffset now)
    {
        var item = new StoreMenuItem
        {
            MenuItemId = UuidV7.New(),
            MenuId = menuId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        item.Apply(parentMenuItemId, label, linkType, targetId, externalUrl, sortOrder, true, now);
        return item;
    }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>فیلدهای قابل ویرایش را می‌نویسد.</summary>
    public void Apply(
        Guid? parentMenuItemId,
        string? label,
        StoreMenuLinkType linkType,
        Guid? targetId,
        string? externalUrl,
        int sortOrder,
        bool isEnabled,
        DateTimeOffset now)
    {
        if (parentMenuItemId == MenuItemId)
        {
            throw new PlatformHttpException(400, "آیتم نمی‌تواند والد خودش باشد.", "menu.item.cycle");
        }

        Label = NormalizeLabel(label);
        LinkType = linkType;
        ParentMenuItemId = parentMenuItemId;
        SortOrder = sortOrder;
        IsEnabled = isEnabled;
        (TargetId, ExternalUrl) = NormalizeDestination(linkType, targetId, externalUrl);
        UpdatedAt = now;
    }

    /// <summary>ترتیب هم‌سطح را بدون تغییر هویت می‌نویسد.</summary>
    public void SetSortOrder(int sortOrder, DateTimeOffset now)
    {
        SortOrder = sortOrder;
        UpdatedAt = now;
    }

    /// <summary>فعال‌سازی را بدون حذف وضعیت ویرایشگر می‌نویسد.</summary>
    public void SetEnabled(bool enabled, DateTimeOffset now)
    {
        IsEnabled = enabled;
        UpdatedAt = now;
    }

    /// <summary>رشتهٔ API را به نوع مقصد تبدیل می‌کند.</summary>
    public static StoreMenuLinkType ParseLinkType(string? raw)
    {
        return raw?.Trim() switch
        {
            "Home" => StoreMenuLinkType.Home,
            "LandingPage" => StoreMenuLinkType.LandingPage,
            "Product" => StoreMenuLinkType.Product,
            "Category" => StoreMenuLinkType.Category,
            "Brand" => StoreMenuLinkType.Brand,
            "Article" => StoreMenuLinkType.Article,
            "External" => StoreMenuLinkType.External,
            "Group" => StoreMenuLinkType.Group,
            _ => throw new PlatformHttpException(400, "نوع مقصد منو معتبر نیست.", "menu.link.invalid"),
        };
    }

    /// <summary>نشانی خارجی را فقط با http/https می‌پذیرد.</summary>
    public static string NormalizeExternalUrl(string? raw)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            throw new PlatformHttpException(400, "نشانی بیرونی لازم است.", "menu.url.required");
        }

        if (value.Length > ExternalUrlMaxLength)
        {
            value = value[..ExternalUrlMaxLength];
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new PlatformHttpException(400, "فقط نشانی وب امن مجاز است.", "menu.url.unsafe");
        }

        return uri.AbsoluteUri;
    }

    private static (Guid? TargetId, string? ExternalUrl) NormalizeDestination(
        StoreMenuLinkType linkType,
        Guid? targetId,
        string? externalUrl)
    {
        return linkType switch
        {
            StoreMenuLinkType.Home or StoreMenuLinkType.Group => (null, null),
            StoreMenuLinkType.External => (null, NormalizeExternalUrl(externalUrl)),
            _ => (
                targetId ?? throw new PlatformHttpException(400, "مقصد داخلی را از فهرست انتخاب کنید.", "menu.target.required"),
                null),
        };
    }

    private static string NormalizeLabel(string? label)
    {
        var value = label?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            throw new PlatformHttpException(400, "عنوان آیتم لازم است.", "menu.item.label.required");
        }

        return value.Length > LabelMaxLength ? value[..LabelMaxLength] : value;
    }
}
