using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain;

/// <summary>بعد اندازه‌گیری واحد کالا.</summary>
public enum UnitOfMeasureDimension
{
    /// <summary>شمارشی (عدد).</summary>
    Count = 0,

    /// <summary>جرم.</summary>
    Mass = 1,

    /// <summary>حجم.</summary>
    Volume = 2,

    /// <summary>طول.</summary>
    Length = 3,
}

/// <summary>واحد اندازه‌گیری پایگاه‌محور.</summary>
public sealed class UnitOfMeasure
{
    /// <summary>شناسه پایدار واحد.</summary>
    public Guid UnitOfMeasureId { get; init; }

    /// <summary>کد ماشین‌خوان مثل pcs یا kg.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>بعد فیزیکی واحد.</summary>
    public UnitOfMeasureDimension Dimension { get; private set; }

    /// <summary>آیا واحد برای انتخاب فعال است.</summary>
    public bool IsActive { get; private set; }

    /// <summary>ترتیب نمایش.</summary>
    public int SortOrder { get; private set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>واحد جدید می‌سازد.</summary>
    public static UnitOfMeasure Create(
        Guid unitOfMeasureId,
        string code,
        UnitOfMeasureDimension dimension,
        bool isActive,
        int sortOrder,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("unit.code.required");
        }

        return new UnitOfMeasure
        {
            UnitOfMeasureId = unitOfMeasureId,
            Code = code.Trim().ToLowerInvariant(),
            Dimension = dimension,
            IsActive = isActive,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}

/// <summary>ترجمهٔ واحد با LanguageId رجیستری زبان موجود.</summary>
public sealed class UnitOfMeasureTranslation
{
    /// <summary>شناسه ترجمه.</summary>
    public Guid TranslationId { get; init; }

    /// <summary>واحد مالک.</summary>
    public Guid UnitOfMeasureId { get; init; }

    /// <summary>زبان رجیستری موجود.</summary>
    public Guid LanguageId { get; init; }

    /// <summary>نام کامل محلی.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>نام کوتاه محلی.</summary>
    public string ShortName { get; private set; } = string.Empty;

    /// <summary>ترجمهٔ واحد می‌سازد.</summary>
    public static UnitOfMeasureTranslation Create(
        Guid unitOfMeasureId,
        Guid languageId,
        string name,
        string shortName)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(shortName))
        {
            throw new InvalidOperationException("unit.translation.required");
        }

        return new UnitOfMeasureTranslation
        {
            TranslationId = UuidV7.New(),
            UnitOfMeasureId = unitOfMeasureId,
            LanguageId = languageId,
            Name = name.Trim(),
            ShortName = shortName.Trim(),
        };
    }
}

/// <summary>یک ردیف تنظیم گرد کردن سراسری فروشگاه.</summary>
public sealed class StoreQuantitySettings
{
    /// <summary>شناسه تک‌ردیفی تنظیم فروشگاه.</summary>
    public static readonly Guid SingletonId = Guid.Parse("01900000-0000-7000-8000-00000000aa01");

    /// <summary>کلید ردیف.</summary>
    public Guid SettingsId { get; init; }

    /// <summary>حالت گرد کردن سراسری.</summary>
    public QuantityRoundingMode RoundingMode { get; private set; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>تنظیم پیش‌فرض نزدیک‌ترین مقدار.</summary>
    public static StoreQuantitySettings CreateDefault(DateTimeOffset now) => new()
    {
        SettingsId = SingletonId,
        RoundingMode = QuantityRoundingMode.Nearest,
        UpdatedAt = now,
    };

    /// <summary>حالت گرد کردن سراسری را عوض می‌کند.</summary>
    public void SetRoundingMode(QuantityRoundingMode mode, DateTimeOffset now)
    {
        RoundingMode = mode;
        UpdatedAt = now;
    }
}

/// <summary>شناسه‌های پایدار واحدهای اولیه.</summary>
public static class CanonicalUnits
{
    /// <summary>عدد / قطعه.</summary>
    public static readonly Guid Pcs = Guid.Parse("01900000-0000-7000-8000-000000000001");

    /// <summary>کیلوگرم.</summary>
    public static readonly Guid Kg = Guid.Parse("01900000-0000-7000-8000-000000000002");

    /// <summary>گرم.</summary>
    public static readonly Guid Gram = Guid.Parse("01900000-0000-7000-8000-000000000003");
}
