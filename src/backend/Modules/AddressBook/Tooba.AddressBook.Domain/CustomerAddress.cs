using Tooba.BuildingBlocks;

namespace Tooba.AddressBook.Domain;

/// <summary>
/// آدرس خصوصی یک مشتری برای تحویل/گیرنده. قیمت، موجودی و کلید سفارش را مالک نیست
/// و هویت سازمانی Party را جایگزین دفترچهٔ ارسال نمی‌کند.
/// </summary>
public sealed class CustomerAddress
{
    /// <summary>حداکثر طول نام گیرنده پس از trim.</summary>
    public const int RecipientNameMaxLength = 128;
    /// <summary>حداقل طول تماس پس از trim؛ قانون ملی ایران در هسته اعمال نمی‌شود.</summary>
    public const int ContactMobileMinLength = 8;
    /// <summary>حداکثر طول تماس پس از trim.</summary>
    public const int ContactMobileMaxLength = 32;
    /// <summary>حداکثر طول کد کشور ISO-مانند.</summary>
    public const int CountryMaxLength = 8;
    /// <summary>کشور پیش‌فرض وقتی درخواست مقدار نداده است.</summary>
    public const string DefaultCountry = "IR";
    /// <summary>حداکثر طول نام استان/ایالت.</summary>
    public const int ProvinceNameMaxLength = 64;
    /// <summary>حداکثر طول نام شهر.</summary>
    public const int CityNameMaxLength = 64;
    /// <summary>حداقل طول کدپستی عمومی.</summary>
    public const int PostalCodeMinLength = 3;
    /// <summary>حداکثر طول کدپستی عمومی؛ رقم‌شمار ملی ایران اجباری نیست.</summary>
    public const int PostalCodeMaxLength = 16;
    /// <summary>حداکثر طول خط نشانی.</summary>
    public const int PostalAddressMaxLength = 512;
    /// <summary>حداکثر طول واحد/ساختمان اختیاری.</summary>
    public const int BuildingUnitMaxLength = 64;
    /// <summary>حداکثر طول برچسب نمایشی اختیاری.</summary>
    public const int LabelMaxLength = 64;

    private CustomerAddress()
    {
    }

    /// <summary>شناسهٔ پایدار ردیف دفترچه.</summary>
    public Guid AddressId { get; init; }

    /// <summary>مالک سرورمحور؛ هرگز از بدنهٔ HTTP پذیرفته نمی‌شود.</summary>
    public Guid OwnerUserId { get; init; }

    /// <summary>نام گیرندهٔ تحویل.</summary>
    public string RecipientName { get; private set; } = string.Empty;

    /// <summary>شمارهٔ تماس گیرنده با محدودیت طول عمومی.</summary>
    public string ContactMobile { get; private set; } = string.Empty;

    /// <summary>کد کشور؛ پیش‌فرض <c>IR</c> است و قوانین اختصاصی بازار ایران را در هسته قفل نمی‌کند.</summary>
    public string Country { get; private set; } = DefaultCountry;

    /// <summary>نام استان یا ایالت؛ برای دفترچه اختیاری است.</summary>
    public string? ProvinceName { get; private set; }

    /// <summary>نام شهر الزامی.</summary>
    public string CityName { get; private set; } = string.Empty;

    /// <summary>کدپستی با محدودیت طول عمومی.</summary>
    public string PostalCode { get; private set; } = string.Empty;

    /// <summary>خط نشانی پستی الزامی.</summary>
    public string PostalAddress { get; private set; } = string.Empty;

    /// <summary>واحد یا پلاک ساختمان؛ اختیاری است.</summary>
    public string? BuildingUnit { get; private set; }

    /// <summary>برچسب نمایشی مثل خانه/محل کار؛ اختیاری است.</summary>
    public string? Label { get; private set; }

    /// <summary>آیا این ردیف پیش‌فرض مالک است؛ حداکثر یکی برای هر مالک.</summary>
    public bool IsDefault { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ردیف معتبر دفترچه را برای مالک مشخص می‌سازد.</summary>
    /// <param name="ownerUserId">شناسهٔ Actor تأمین‌شده از Host.</param>
    /// <param name="recipientName">نام گیرنده.</param>
    /// <param name="contactMobile">تماس گیرنده.</param>
    /// <param name="country">کشور؛ تهی یعنی پیش‌فرض <c>IR</c>.</param>
    /// <param name="provinceName">استان اختیاری.</param>
    /// <param name="cityName">شهر الزامی.</param>
    /// <param name="postalCode">کدپستی عمومی.</param>
    /// <param name="postalAddress">خط نشانی.</param>
    /// <param name="buildingUnit">واحد اختیاری.</param>
    /// <param name="label">برچسب اختیاری.</param>
    /// <param name="isDefault">درخواست پیش‌فرض بودن؛ یکتایی در لایهٔ دایرکتوری اعمال می‌شود.</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="addressId">شناسهٔ قطعی دانه؛ در مسیر عادی تولید می‌شود.</param>
    public static CustomerAddress Create(
        Guid ownerUserId,
        string recipientName,
        string contactMobile,
        string? country,
        string? provinceName,
        string cityName,
        string postalCode,
        string postalAddress,
        string? buildingUnit,
        string? label,
        bool isDefault,
        DateTimeOffset now,
        Guid? addressId = null)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Actor معتبر الزامی است.");
        }

        var address = new CustomerAddress
        {
            AddressId = addressId ?? UuidV7.New(),
            OwnerUserId = ownerUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        address.ApplyFields(
            recipientName,
            contactMobile,
            country,
            provinceName,
            cityName,
            postalCode,
            postalAddress,
            buildingUnit,
            label,
            isDefault,
            now);
        return address;
    }

    /// <summary>فیلدهای قابل ویرایش مالک را با همان قواعد اعتبارسنجی به‌روز می‌کند.</summary>
    public void Update(
        string recipientName,
        string contactMobile,
        string? country,
        string? provinceName,
        string cityName,
        string postalCode,
        string postalAddress,
        string? buildingUnit,
        string? label,
        bool isDefault,
        DateTimeOffset now) =>
        ApplyFields(
            recipientName,
            contactMobile,
            country,
            provinceName,
            cityName,
            postalCode,
            postalAddress,
            buildingUnit,
            label,
            isDefault,
            now);

    /// <summary>این ردیف را پیش‌فرض می‌کند؛ پاک‌سازی پیش‌فرض قبلی وظیفهٔ تراکنش دایرکتوری است.</summary>
    public void MarkDefault(DateTimeOffset now)
    {
        IsDefault = true;
        UpdatedAt = now;
    }

    /// <summary>وضعیت پیش‌فرض را برمی‌دارد بدون انتخاب جایگزین.</summary>
    public void ClearDefault(DateTimeOffset now)
    {
        IsDefault = false;
        UpdatedAt = now;
    }

    private void ApplyFields(
        string recipientName,
        string contactMobile,
        string? country,
        string? provinceName,
        string cityName,
        string postalCode,
        string postalAddress,
        string? buildingUnit,
        string? label,
        bool isDefault,
        DateTimeOffset now)
    {
        RecipientName = RequireBounded(recipientName, 1, RecipientNameMaxLength, "نام گیرنده الزامی است.");
        ContactMobile = RequireBounded(contactMobile, ContactMobileMinLength, ContactMobileMaxLength, "شمارهٔ تماس معتبر نیست.");
        var resolvedCountry = string.IsNullOrWhiteSpace(country) ? DefaultCountry : country.Trim().ToUpperInvariant();
        Country = RequireBounded(resolvedCountry, 2, CountryMaxLength, "کشور الزامی است.");
        ProvinceName = OptionalBounded(provinceName, ProvinceNameMaxLength, "نام استان بیش از حد بلند است.");
        CityName = RequireBounded(cityName, 1, CityNameMaxLength, "شهر الزامی است.");
        PostalCode = RequireBounded(postalCode, PostalCodeMinLength, PostalCodeMaxLength, "کدپستی معتبر نیست.");
        PostalAddress = RequireBounded(postalAddress, 1, PostalAddressMaxLength, "نشانی پستی الزامی است.");
        BuildingUnit = OptionalBounded(buildingUnit, BuildingUnitMaxLength, "واحد ساختمان بیش از حد بلند است.");
        Label = OptionalBounded(label, LabelMaxLength, "برچسب نشانی بیش از حد بلند است.");
        IsDefault = isDefault;
        UpdatedAt = now;
    }

    private static string RequireBounded(string? value, int min, int max, string message)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length < min || trimmed.Length > max)
        {
            throw new InvalidOperationException(message);
        }

        return trimmed;
    }

    private static string? OptionalBounded(string? value, int max, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > max)
        {
            throw new InvalidOperationException(message);
        }

        return trimmed;
    }
}
