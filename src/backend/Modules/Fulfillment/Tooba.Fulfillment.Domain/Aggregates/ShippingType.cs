using Tooba.BuildingBlocks;

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
/// کد canonical پس از ساخت تغییرناپذیر است؛ تغییر آن یعنی هویت تازه، نه نام‌گذاری دوبارهٔ بی‌صدا. کد با
/// فاصلهٔ ابتدا/انتها یا هر نویسهٔ خارج از شکل مجاز رد می‌شود و هرگز trim/بازنویسی نمی‌شود.
/// </para>
/// <para>
/// مرز خطا: کدها/پیام‌های خطای تجاری مالکیت Application/Contracts است و دامنه به Contracts وابسته نمی‌شود
/// (گارد معماری Fulfillment این وابستگی را ممنوع می‌کند). ورودی این موجودیت همیشه از پیش‌بررسی canonical
/// Application گذشته است؛ بررسی‌های این‌جا فقط نگهبان برنامه‌نویسی (invariant) هستند.
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

    /// <summary>شمارهٔ بازبینی یکنوا؛ همین مقدار predicate واقعی همزمانی است.</summary>
    public long Revision { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ترجمه‌های ذخیره‌شده، مرتب‌شده برای خواندن قطعی.</summary>
    public IReadOnlyList<ShippingTypeTranslation> Translations => _translations;

    /// <summary>
    /// تعریف تازه می‌سازد: همیشه غیرفعال، بازبینی ۱ و با ترجمه‌های ازپیش‌آمادهٔ داده‌شده.
    /// </summary>
    /// <param name="id">شناسهٔ سرورساختهٔ غیرتهی.</param>
    /// <param name="code">کد canonical ازپیش‌اعتبارسنجی‌شده.</param>
    /// <param name="sortOrder">ترتیب صریح در بازهٔ مجاز.</param>
    /// <param name="translations">ترجمه‌های ازپیش‌آماده.</param>
    /// <param name="now">زمان UTC.</param>
    /// <exception cref="ArgumentException">نقض invariant برنامه‌نویسی؛ هرگز از مسیر کاربر عبور نمی‌کند.</exception>
    public static ShippingType Create(
        Guid id,
        string code,
        int sortOrder,
        IReadOnlyList<ShippingTypeTranslation> translations,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(translations);
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sortOrder > SortOrderMaxValue ? -1 : sortOrder);

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

    /// <summary>ترجمهٔ یک زبان موجود را با مقادیر ازپیش‌آماده جایگزین می‌کند.</summary>
    /// <param name="languageId">شناسهٔ زبان همان ردیف.</param>
    /// <param name="name">نام ازپیش‌نرمال‌شدهٔ غیرتهی.</param>
    /// <param name="description">توضیح ازپیش‌نرمال‌شده یا <c>null</c> برای پاک‌کردن همان توضیح.</param>
    public void ReplaceTranslation(Guid languageId, string name, string? description)
    {
        var translation = _translations.FirstOrDefault(x => x.LanguageId == languageId)
            ?? throw new ArgumentException("shipping_type_translation_missing", nameof(languageId));

        translation.Replace(name, description);
    }

    /// <summary>ترجمهٔ یک زبان ازپیش‌آماده را برای این تعریف اضافه می‌کند.</summary>
    /// <param name="shippingTypeTranslationId">شناسهٔ سرورساختهٔ ردیف ترجمه.</param>
    /// <param name="languageId">شناسهٔ زبان.</param>
    /// <param name="name">نام ازپیش‌نرمال‌شده.</param>
    /// <param name="description">توضیح ازپیش‌نرمال‌شدهٔ اختیاری.</param>
    public void AddTranslation(
        Guid shippingTypeTranslationId,
        Guid languageId,
        string name,
        string? description)
    {
        if (_translations.Any(x => x.LanguageId == languageId))
        {
            throw new ArgumentException("shipping_type_translation_duplicate", nameof(languageId));
        }

        _translations.Add(ShippingTypeTranslation.Create(
            shippingTypeTranslationId, languageId, name, description));
    }

    /// <summary>آیا برای این زبان متنی ذخیره شده است.</summary>
    /// <param name="languageId">شناسهٔ زبان.</param>
    public bool HasTranslation(Guid languageId) => _translations.Any(x => x.LanguageId == languageId);

    /// <summary>
    /// کد canonical را اعتبارسنجی می‌کند: حرف کوچک ASCII در ابتدا، سپس حروف کوچک/رقم/زیرخط، ۱..۶۴.
    /// </summary>
    /// <remarks>
    /// هیچ trim یا اصلاح خودکاری انجام نمی‌شود؛ ورودی دارای فاصلهٔ ابتدا/انتها نامعتبر است تا هویت
    /// canonical بی‌صدا بازنویسی نشود.
    /// </remarks>
    /// <returns>همان کد یا <c>null</c> وقتی نامعتبر است.</returns>
    public static string? TryNormalizeCode(string? code)
    {
        if (string.IsNullOrEmpty(code) || code.Length > CodeMaxLength)
        {
            return null;
        }

        var first = code[0];
        if (first < 'a' || first > 'z')
        {
            return null;
        }

        foreach (var ch in code)
        {
            var ok = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_';
            if (!ok)
            {
                return null;
            }
        }

        return code;
    }

    private static string NormalizeCode(string code) =>
        TryNormalizeCode(code) ?? throw new ArgumentException("shipping_type_code_invalid", nameof(code));

    private static void EnsureSortOrder(int sortOrder)
    {
        if (sortOrder is < SortOrderMinValue or > SortOrderMaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }
    }
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

    /// <summary>یک ترجمهٔ تازه با مقادیر ازپیش‌آماده می‌سازد.</summary>
    /// <param name="shippingTypeTranslationId">شناسهٔ سرورساختهٔ ردیف ترجمه (تولید شناسه در Application/Infrastructure).</param>
    /// <param name="languageId">شناسهٔ زبان غیرتهی.</param>
    /// <param name="name">نام ازپیش‌نرمال‌شدهٔ غیرتهی.</param>
    /// <param name="description">توضیح ازپیش‌نرمال‌شدهٔ اختیاری.</param>
    public static ShippingTypeTranslation Create(
        Guid shippingTypeTranslationId,
        Guid languageId,
        string name,
        string? description)
    {
        ArgumentNullException.ThrowIfNull(name);

        return new ShippingTypeTranslation
        {
            ShippingTypeTranslationId = shippingTypeTranslationId,
            LanguageId = languageId,
            Name = name,
            Description = description,
        };
    }

    /// <summary>
    /// مقادیر همین ترجمه را جایگزین می‌کند.
    /// </summary>
    /// <remarks>
    /// هر دو مقدار ازپیش canonical هستند و هم‌زمان انتساب می‌یابند؛ بنابراین هیچ انتساب نیمه‌کاره‌ای ممکن نیست.
    /// اعتبارسنجی متن (خالی/طول/زبان) در قاعدهٔ واحد Application انجام می‌شود، نه در دامنه.
    /// </remarks>
    /// <param name="name">نام ازپیش‌نرمال‌شده.</param>
    /// <param name="description">توضیح ازپیش‌نرمال‌شده یا <c>null</c> برای پاک‌کردن.</param>
    public void Replace(string name, string? description)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
        Description = description;
    }
}
