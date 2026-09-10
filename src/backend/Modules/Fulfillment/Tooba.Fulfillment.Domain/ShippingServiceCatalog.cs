namespace Tooba.Fulfillment.Domain;

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

/// <summary>ترجمهٔ نام سرویس ارسال والد.</summary>
public sealed class ShippingServiceTranslation
{
    /// <summary>شناسه ترجمه.</summary>
    public Guid TranslationId { get; init; }

    /// <summary>سرویس مالک.</summary>
    public Guid ShippingServiceId { get; init; }

    /// <summary>زبان رجیستری.</summary>
    public Guid LanguageId { get; init; }

    /// <summary>نام محلی.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>توضیح اختیاری.</summary>
    public string? Description { get; private set; }

    /// <summary>ترجمه می‌سازد.</summary>
    public static ShippingServiceTranslation Create(
        Guid translationId,
        Guid shippingServiceId,
        Guid languageId,
        string name,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("shipping_service.name.required");
        }

        return new ShippingServiceTranslation
        {
            TranslationId = translationId,
            ShippingServiceId = shippingServiceId,
            LanguageId = languageId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
        };
    }

    /// <summary>نام/توضیح را به‌روز می‌کند.</summary>
    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("shipping_service.name.required");
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}

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

/// <summary>ترجمهٔ نوع سرویس فرزند.</summary>
public sealed class ShippingServiceOptionTranslation
{
    /// <summary>شناسه ترجمه.</summary>
    public Guid TranslationId { get; init; }

    /// <summary>گزینه مالک.</summary>
    public Guid ShippingServiceOptionId { get; init; }

    /// <summary>زبان رجیستری.</summary>
    public Guid LanguageId { get; init; }

    /// <summary>نام محلی.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>ترجمه می‌سازد.</summary>
    public static ShippingServiceOptionTranslation Create(
        Guid translationId,
        Guid optionId,
        Guid languageId,
        string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("shipping_service_option.name.required");
        }

        return new ShippingServiceOptionTranslation
        {
            TranslationId = translationId,
            ShippingServiceOptionId = optionId,
            LanguageId = languageId,
            Name = name.Trim(),
        };
    }

    /// <summary>نام را به‌روز می‌کند.</summary>
    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("shipping_service_option.name.required");
        }

        Name = name.Trim();
    }
}
