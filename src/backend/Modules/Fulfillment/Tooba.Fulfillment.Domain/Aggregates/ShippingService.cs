

namespace Tooba.Fulfillment.Domain.Aggregates;


/// <summary>سرویس ارسال سطح والد (مثلاً پست، تیپاکس).</summary>
public sealed class ShippingService
{
    /// <summary>شناسه پایدار سرویس.</summary>
    public Guid ShippingServiceId { get; init; }

    /// <summary>کد ماشین‌خوان مثل post.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>نوع ارائه‌دهنده برای metadata مرسوله (post/tipax/...).</summary>
    public string ProviderKind { get; private set; } = string.Empty;

    /// <summary>کلید آیکن UI مثل post / tipax / courier.</summary>
    public string IconKey { get; private set; } = string.Empty;

    /// <summary>کلید رنگ آیکن مثل blue / amber / emerald.</summary>
    public string ColorKey { get; private set; } = string.Empty;

    /// <summary>ترتیب نمایش.</summary>
    public int SortOrder { get; private set; }

    /// <summary>فعال بودن برای انتخاب.</summary>
    public bool IsActive { get; private set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>سرویس جدید می‌سازد.</summary>
    public static ShippingService Create(
        Guid id,
        string code,
        string providerKind,
        string iconKey,
        string colorKey,
        int sortOrder,
        bool isActive,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("shipping_service.code.required");
        }

        return new ShippingService
        {
            ShippingServiceId = id,
            Code = code.Trim().ToLowerInvariant(),
            ProviderKind = string.IsNullOrWhiteSpace(providerKind) ? code.Trim().ToLowerInvariant() : providerKind.Trim().ToLowerInvariant(),
            IconKey = string.IsNullOrWhiteSpace(iconKey) ? "truck" : iconKey.Trim().ToLowerInvariant(),
            ColorKey = string.IsNullOrWhiteSpace(colorKey) ? "blue" : colorKey.Trim().ToLowerInvariant(),
            SortOrder = sortOrder,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>فیلدهای پایه را به‌روز می‌کند.</summary>
    public void Update(
        string code,
        string providerKind,
        string iconKey,
        string colorKey,
        int sortOrder,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("shipping_service.code.required");
        }

        Code = code.Trim().ToLowerInvariant();
        ProviderKind = string.IsNullOrWhiteSpace(providerKind) ? Code : providerKind.Trim().ToLowerInvariant();
        IconKey = string.IsNullOrWhiteSpace(iconKey) ? IconKey : iconKey.Trim().ToLowerInvariant();
        ColorKey = string.IsNullOrWhiteSpace(colorKey) ? ColorKey : colorKey.Trim().ToLowerInvariant();
        SortOrder = sortOrder;
        UpdatedAt = now;
    }

    /// <summary>فعال/غیرفعال می‌کند.</summary>
    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        UpdatedAt = now;
    }
}
