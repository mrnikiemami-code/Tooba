using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Fulfillment.Domain.Aggregates;

/// <summary>
/// تعریف مشترک «نوع ارسال عملیاتی» (مثلاً عادی/سنگین) — فقط هویت، ترتیب، فعال‌بودن و متن چندزبانه.
/// </summary>
/// <remarks>
/// <para>
/// این موجودیت هیچ نرخ، جغرافیا، بسته‌بندی، برنامه، ارز، حالت پرداخت/محاسبه، ظرفیت یا وعدهٔ تحویل ندارد؛
/// فعال‌بودن آن هیچ روش ارسالی را برای مشتری قابل انتخاب نمی‌کند. فهرست نمونه‌های محصول (عادی/سنگین/…) یک
/// مثال است و در دامنه/داده به‌عنوان seed یا enum بسته هاردکد نمی‌شود.
/// </para>
/// <para>
/// کد canonical پس از ساخت تغییرناپذیر است؛ تغییر آن یعنی هویت تازه، نه نام‌گذاری دوبارهٔ بی‌صدا.
/// </para>
/// </remarks>
public sealed class ShippingType
{
    /// <summary>حداکثر طول کد canonical.</summary>
    public const int CodeMaxLength = 64;

    /// <summary>حداکثر طول نام هر ترجمه.</summary>
    public const int NameMaxLength = 128;

    /// <summary>حداکثر طول توضیح هر ترجمه.</summary>
    public const int DescriptionMaxLength = 512;

    /// <summary>کمینهٔ ترتیب نمایش.</summary>
    public const int SortOrderMinValue = 0;

    /// <summary>بیشینهٔ ترتیب نمایش.</summary>
    public const int SortOrderMaxValue = 1_000_000;

    private readonly List<ShippingTypeTranslation> _translations = [];

    private ShippingType()
    {
    }

    /// <summary>شناسهٔ پایدار سرورساخته.</summary>
    public Guid ShippingTypeId { get; private set; }

    /// <summary>کد canonical تغییرناپذیر.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>ترتیب نمایش صریح.</summary>
    public int SortOrder { get; private set; }

    /// <summary>فعال‌بودن صریح؛ تعریف تازه همیشه غیرفعال ساخته می‌شود.</summary>
    public bool IsActive { get; private set; }

    /// <summary>شمارهٔ بازبینی یکنوا؛ همین ستون predicate واقعی همزمانی است.</summary>
    public long Revision { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ترجمه‌های ذخیره‌شده، مرتب‌شده برای خواندن قطعی.</summary>
    public IReadOnlyList<ShippingTypeTranslation> Translations => _translations;

    /// <summary>
    /// تعریف تازه می‌سازد: همیشه غیرفعال، بازبینی ۱ و با ترجمه‌های داده‌شده.
    /// </summary>
    /// <param name="id">شناسهٔ سرورساختهٔ غیرتهی.</param>
    /// <param name="code">کد canonical.</param>
    /// <param name="sortOrder">ترتیب صریح در بازهٔ مجاز.</param>
    /// <param name="translations">ترجمه‌های اولیه (می‌تواند فقط زبان پیش‌فرض فعال باشد).</param>
    /// <param name="now">زمان UTC.</param>
    public static ShippingType Create(
        Guid id,
        string code,
        int sortOrder,
        IReadOnlyList<ShippingTypeTranslation> translations,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(translations);

        if (id == Guid.Empty)
        {
            throw Fault("shipping_type.translation.invalid");
        }

        EnsureSortOrder(sortOrder);

        var entity = new ShippingType
        {
            ShippingTypeId = id,
            Code = NormalizeCode(code),
            SortOrder = sortOrder,
            IsActive = false,
            Revision = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        entity._translations.AddRange(translations);
        return entity;
    }

    /// <summary>
    /// ترتیب و فعال‌بودن را جایگزین می‌کند و بازبینی را یک واحد بالا می‌برد.
    /// </summary>
    /// <param name="sortOrder">ترتیب صریح در بازهٔ مجاز.</param>
    /// <param name="isActive">وضعیت فعال‌بودن درخواستی.</param>
    /// <param name="now">زمان UTC.</param>
    public void Update(int sortOrder, bool isActive, DateTimeOffset now)
    {
        EnsureSortOrder(sortOrder);
        SortOrder = sortOrder;
        IsActive = isActive;
        Revision += 1;
        UpdatedAt = now;
    }

    /// <summary>ترجمهٔ یک زبان موجود را با مقادیر داده‌شده جایگزین می‌کند.</summary>
    /// <param name="languageId">شناسهٔ زبان همان ردیف.</param>
    /// <param name="name">نام غیرتهی نرمال‌شده.</param>
    /// <param name="description">توضیح یا <c>null</c> برای پاک‌کردن همان توضیح.</param>
    public void ReplaceTranslation(Guid languageId, string name, string? description)
    {
        var translation = _translations.FirstOrDefault(x => x.LanguageId == languageId)
            ?? throw Fault("shipping_type.translation.invalid");

        translation.Replace(name, description);
    }

    /// <summary>ترجمهٔ یک زبان را برای یک تعریف تازه اضافه می‌کند.</summary>
    /// <param name="languageId">شناسهٔ زبان.</param>
    /// <param name="name">نام.</param>
    /// <param name="description">توضیح اختیاری.</param>
    public void AddTranslation(Guid languageId, string name, string? description)
    {
        if (_translations.Any(x => x.LanguageId == languageId))
        {
            throw Fault("shipping_type.translation.invalid");
        }

        _translations.Add(ShippingTypeTranslation.Create(languageId, name, description));
    }

    /// <summary>آیا برای این زبان متنی ذخیره شده است.</summary>
    /// <param name="languageId">شناسهٔ زبان.</param>
    public bool HasTranslation(Guid languageId) => _translations.Any(x => x.LanguageId == languageId);

    /// <summary>
    /// کد canonical را نرمال/اعتبارسنجی می‌کند: حرف کوچک ASCII در ابتدا، سپس حروف کوچک/رقم/زیرخط، ۱..۶۴.
    /// </summary>
    /// <returns>کد نرمال‌شده یا <c>null</c> وقتی نامعتبر است.</returns>
    public static string? TryNormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var candidate = code.Trim();
        if (candidate.Length is 0 or > CodeMaxLength)
        {
            return null;
        }

        var first = candidate[0];
        if (first < 'a' || first > 'z')
        {
            return null;
        }

        foreach (var ch in candidate)
        {
            var ok = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_';
            if (!ok)
            {
                return null;
            }
        }

        return candidate;
    }

    private static string NormalizeCode(string code)
        => TryNormalizeCode(code)
           ?? throw Fault("shipping_type.code.invalid");

    private static void EnsureSortOrder(int sortOrder)
    {
        if (sortOrder is < SortOrderMinValue or > SortOrderMaxValue)
        {
            throw Fault("shipping_type.sort_order.invalid");
        }
    }

    private static SemanticException Fault(string code) => new(new SemanticError(code));
}

/// <summary>
/// متن نمایشی یک نوع ارسال برای یک زبان محتوا. شناسهٔ زبان پایدار رجیستری Localization است.
/// </summary>
public sealed class ShippingTypeTranslation
{
    private ShippingTypeTranslation()
    {
    }

    /// <summary>شناسهٔ پایدار ردیف.</summary>
    public Guid ShippingTypeTranslationId { get; private set; }

    /// <summary>نوع ارسال مالک.</summary>
    public Guid ShippingTypeId { get; private set; }

    /// <summary>شناسهٔ زبان محتوا.</summary>
    public Guid LanguageId { get; private set; }

    /// <summary>نام غیرتهی.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>توضیح اختیاری.</summary>
    public string? Description { get; private set; }

    /// <summary>یک ترجمهٔ تازه می‌سازد.</summary>
    /// <param name="languageId">شناسهٔ زبان غیرتهی.</param>
    /// <param name="name">نام غیرتهی (حداکثر ۱۲۸ نویسه).</param>
    /// <param name="description">توضیح اختیاری (حداکثر ۵۱۲ نویسه).</param>
    public static ShippingTypeTranslation Create(Guid languageId, string name, string? description)
    {
        if (languageId == Guid.Empty)
        {
            throw new SemanticException(new SemanticError("shipping_type.translation.invalid"));
        }

        return new ShippingTypeTranslation
        {
            ShippingTypeTranslationId = UuidV7.New(),
            LanguageId = languageId,
            Name = NormalizeName(name),
            Description = NormalizeDescription(description),
        };
    }

    /// <summary>مقادیر همین ترجمه را جایگزین می‌کند.</summary>
    /// <param name="name">نام غیرتهی جدید.</param>
    /// <param name="description">توضیح جدید یا <c>null</c> برای پاک‌کردن.</param>
    public void Replace(string name, string? description)
    {
        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new SemanticException(new SemanticError("shipping_type.translation.invalid"));
        }

        var trimmed = name.Trim();
        if (trimmed.Length > ShippingType.NameMaxLength)
        {
            throw new SemanticException(new SemanticError("shipping_type.translation.invalid"));
        }

        return trimmed;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (description is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        if (trimmed.Length > ShippingType.DescriptionMaxLength)
        {
            throw new SemanticException(new SemanticError("shipping_type.translation.invalid"));
        }

        return trimmed;
    }
}
