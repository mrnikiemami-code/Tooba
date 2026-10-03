using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// تعریف ویژگی تایپ‌شده. ستون قیمت یا موجودی محصول نیست.
/// </summary>
public sealed class CatalogAttributeDefinition
{
    /// <summary>
    /// شناسهٔ تعریف.
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// کد پایدار ماشینی (مثلاً color). برچسب نمایش در جدول ترجمه است.
    /// </summary>
    public string Code { get; init; } = "";

    /// <summary>
    /// نوع مقدار؛ از Dictionary آزاد جلوگیری می‌کند.
    /// </summary>
    public CatalogAttributeValueKind ValueKind { get; init; }

    /// <summary>
    /// اگر true باشد تعریف مجاز است به‌عنوان محور ترکیب Variant انتخاب شود (نه مشخصات سادهٔ محصول).
    /// ستون DB همان <c>IsVariantAxis</c> است؛ معنای معنایی IsVariantAxisAllowed.
    /// </summary>
    public bool IsVariantAxis { get; set; }

    /// <summary>
    /// نام مستعار معنایی برای <see cref="IsVariantAxis"/>؛ در EF نادیده گرفته می‌شود.
    /// </summary>
    public bool IsVariantAxisAllowed => IsVariantAxis;

    /// <summary>
    /// واحد نمایشی اختیاری (مثلاً GB، inch)؛ قیمت نیست.
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// پیش‌فرض الزام در سطح تعریف؛ override رده می‌تواند سخت‌تر کند.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// قابل استفاده در فیلتر ویترین آینده.
    /// </summary>
    public bool IsFilterable { get; set; }

    /// <summary>
    /// قابل مقایسه در جدول مقایسهٔ آینده.
    /// </summary>
    public bool IsComparable { get; set; }

    /// <summary>
    /// چندمقداری بودن؛ در foundation فعلی مقدار تکی ذخیره می‌شود.
    /// </summary>
    public bool IsMultivalue { get; set; }

    /// <summary>
    /// ترتیب نمایش پیش‌فرض تعریف.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// حداقل عددی اختیاری برای Number.
    /// </summary>
    public decimal? ValidationMin { get; set; }

    /// <summary>
    /// حداکثر عددی اختیاری برای Number.
    /// </summary>
    public decimal? ValidationMax { get; set; }

    /// <summary>
    /// حداکثر طول متن اختیاری برای Text.
    /// </summary>
    public int? ValidationMaxLength { get; set; }

    /// <summary>
    /// فعال بودن تعریف برای schema authoring؛ پیش‌فرض true برای BC.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// تعریف ویژگی می‌سازد.
    /// </summary>
    public static CatalogAttributeDefinition Create(string code, CatalogAttributeValueKind valueKind, bool isVariantAxis, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new CatalogAttributeDefinition
        {
            DefinitionId = UuidV7.New(),
            Code = code.Trim().ToLowerInvariant(),
            ValueKind = valueKind,
            IsVariantAxis = isVariantAxis,
            Unit = null,
            IsRequired = false,
            IsFilterable = false,
            IsComparable = false,
            IsMultivalue = false,
            DisplayOrder = 0,
            ValidationMin = null,
            ValidationMax = null,
            ValidationMaxLength = null,
            IsActive = true,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// فرادادهٔ schema را بدون تغییر Code/ValueKind/IsVariantAxis به‌روز می‌کند.
    /// </summary>
    public void UpdateMetadata(
        string? unit,
        bool isRequired,
        bool isFilterable,
        bool isComparable,
        bool isMultivalue,
        int displayOrder,
        decimal? validationMin,
        decimal? validationMax,
        int? validationMaxLength,
        bool isActive)
    {
        if (validationMin is not null && validationMax is not null && validationMin > validationMax)
        {
            throw new InvalidOperationException("حداقل اعتبارسنجی نمی‌تواند از حداکثر بزرگ‌تر باشد.");
        }

        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        IsRequired = isRequired;
        IsFilterable = isFilterable;
        IsComparable = isComparable;
        IsMultivalue = isMultivalue;
        DisplayOrder = displayOrder;
        ValidationMin = validationMin;
        ValidationMax = validationMax;
        ValidationMaxLength = validationMaxLength is < 0
            ? throw new InvalidOperationException("حداکثر طول نمی‌تواند منفی باشد.")
            : validationMaxLength;
        IsActive = isActive;
    }

    /// <summary>
    /// قابلیت محور تنوع را به‌روز می‌کند؛ bindingهای رده را خودکار تغییر نمی‌دهد.
    /// </summary>
    public void SetVariantAxisAllowed(bool enabled)
    {
        if (enabled)
        {
            CatalogCategoryAttributeAssignmentRules.ValidateVariantAxisCapabilityEnable(ValueKind);
        }

        IsVariantAxis = enabled;
    }
}
