

namespace Tooba.Fulfillment.Domain.Aggregates;


/// <summary>نوع/گزینهٔ سرویس سطح فرزند (مثلاً پیشتاز، معمولی).</summary>
public sealed class ShippingServiceOption
{
    /// <summary>شناسه گزینه.</summary>
    public Guid ShippingServiceOptionId { get; init; }

    /// <summary>سرویس والد.</summary>
    public Guid ShippingServiceId { get; init; }

    /// <summary>کد ماشین‌خوان مثل express.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>ترتیب نمایش.</summary>
    public int SortOrder { get; private set; }

    /// <summary>فعال بودن.</summary>
    public bool IsActive { get; private set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>گزینه جدید می‌سازد.</summary>
    public static ShippingServiceOption Create(
        Guid id,
        Guid shippingServiceId,
        string code,
        int sortOrder,
        bool isActive,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("shipping_service_option.code.required");
        }

        return new ShippingServiceOption
        {
            ShippingServiceOptionId = id,
            ShippingServiceId = shippingServiceId,
            Code = code.Trim().ToLowerInvariant(),
            SortOrder = sortOrder,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>کد/ترتیب را به‌روز می‌کند.</summary>
    public void Update(string code, int sortOrder, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("shipping_service_option.code.required");
        }

        Code = code.Trim().ToLowerInvariant();
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
