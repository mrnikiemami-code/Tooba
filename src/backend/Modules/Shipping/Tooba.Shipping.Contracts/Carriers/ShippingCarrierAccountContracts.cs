namespace Tooba.Shipping.Contracts.Carriers;

/// <summary>محیط اتصال حامل؛ تست و تولید جدا نگه داشته می‌شوند.</summary>
public enum ShippingCarrierEnvironment
{
    /// <summary>محیط آزمایشی provider.</summary>
    Test = 0,

    /// <summary>محیط تولید provider.</summary>
    Production = 1,
}

/// <summary>گونهٔ مقدار مرجع همگام‌شده از حامل.</summary>
public enum ShippingCarrierReferenceKind
{
    /// <summary>خدمت حمل.</summary>
    Service = 0,

    /// <summary>نوع پرداخت.</summary>
    PaymentType = 1,

    /// <summary>نوع Pickup.</summary>
    PickupType = 2,

    /// <summary>نوع توزیع.</summary>
    DistributionType = 3,

    /// <summary>نوع بسته‌بندی.</summary>
    Packing = 4,

    /// <summary>محتوای بسته.</summary>
    PackageContent = 5,

    /// <summary>نوع پک.</summary>
    PackType = 6,
}

/// <summary>نتیجهٔ آزمون اتصال حامل بدون افشای راز.</summary>
public enum ShippingCarrierConnectionTestOutcome
{
    /// <summary>اتصال و احراز هویت موفق بود.</summary>
    Succeeded = 0,

    /// <summary>حساب غیرفعال است.</summary>
    Disabled = 1,

    /// <summary>ارجاع راز موجود نیست یا نسخه ناسازگار است.</summary>
    SecretMissing = 2,

    /// <summary>احراز هویت رد شد.</summary>
    Unauthorized = 3,

    /// <summary>provider موقتاً در دسترس نیست.</summary>
    TemporarilyUnavailable = 4,

    /// <summary>پاسخ provider رد شد.</summary>
    ProviderRejected = 5,
}

/// <summary>قابلیت‌های خنثی یک provider؛ عملیات پشتیبانی‌نشده صریح‌اند.</summary>
/// <param name="ProviderCode">کد provider.</param>
/// <param name="SupportsRateQuote">پشتیبانی قیمت.</param>
/// <param name="SupportsReferenceSync">پشتیبانی همگام مرجع.</param>
/// <param name="SupportsConnectionTest">پشتیبانی آزمون اتصال.</param>
/// <param name="SupportsShipmentLifecycle">پشتیبانی چرخه محموله (R4؛ اینجا معمولاً false).</param>
public sealed record ShippingCarrierProviderCapabilities(
    string ProviderCode,
    bool SupportsRateQuote,
    bool SupportsReferenceSync,
    bool SupportsConnectionTest,
    bool SupportsShipmentLifecycle);

/// <summary>نمایهٔ حساب حامل مالک؛ راز هرگز در این DTO نیست.</summary>
/// <param name="AccountId">شناسهٔ حساب.</param>
/// <param name="ProviderCode">کد provider خنثی (مثلاً tipax).</param>
/// <param name="DisplayName">نام نمایشی بازرگان.</param>
/// <param name="Environment">محیط تست/تولید.</param>
/// <param name="SecretReference">ارجاع راز سمت سرور؛ مقدار راز نیست.</param>
/// <param name="IsActive">فعال بودن حساب.</param>
/// <param name="ConfigurationVersion">نسخهٔ پیکربندی برای ابطال کش توکن.</param>
/// <param name="LastConnectionOutcome">آخرین نتیجهٔ آزمون اتصال یا تهی.</param>
/// <param name="LastConnectionAt">زمان آخرین آزمون یا تهی.</param>
/// <param name="LastReferenceSyncAt">زمان آخرین همگام‌سازی مرجع یا تهی.</param>
public sealed record ShippingCarrierAccountView(
    Guid AccountId,
    string ProviderCode,
    string DisplayName,
    ShippingCarrierEnvironment Environment,
    string SecretReference,
    bool IsActive,
    int ConfigurationVersion,
    ShippingCarrierConnectionTestOutcome? LastConnectionOutcome,
    DateTimeOffset? LastConnectionAt,
    DateTimeOffset? LastReferenceSyncAt);

/// <summary>یک مقدار مرجع ذخیره‌شده از provider.</summary>
/// <param name="Kind">گونهٔ مرجع.</param>
/// <param name="ExternalId">شناسهٔ خارجی.</param>
/// <param name="Title">عنوان.</param>
/// <param name="IsActive">فعال بودن.</param>
public sealed record ShippingCarrierReferenceValueView(
    ShippingCarrierReferenceKind Kind,
    string ExternalId,
    string Title,
    bool IsActive);

/// <summary>نگاشت مقصد Geography به شناسهٔ خارجی حامل.</summary>
/// <param name="MappingId">شناسهٔ نگاشت.</param>
/// <param name="AccountId">حساب حامل.</param>
/// <param name="ProviderCode">کد provider.</param>
/// <param name="GeographyCityId">شناسهٔ شهر Geography.</param>
/// <param name="ExternalLocationCode">کد خارجی.</param>
/// <param name="Provenance">منبع نگاشت.</param>
/// <param name="UpdatedAt">زمان به‌روزرسانی.</param>
public sealed record ShippingCarrierDestinationMappingView(
    Guid MappingId,
    Guid AccountId,
    string ProviderCode,
    Guid GeographyCityId,
    string ExternalLocationCode,
    string Provenance,
    DateTimeOffset UpdatedAt);

/// <summary>اتصال CarrierApi روی پیش‌نویس؛ شناسه‌های مرجع provider صریح‌اند.</summary>
/// <param name="BindingId">شناسهٔ اتصال.</param>
/// <param name="DraftId">پیش‌نویس.</param>
/// <param name="CarrierAccountId">حساب حامل.</param>
/// <param name="ProviderCode">کد provider.</param>
/// <param name="ProviderServiceId">شناسهٔ خدمت.</param>
/// <param name="PaymentTypeId">شناسهٔ پرداخت.</param>
/// <param name="PickupTypeId">شناسهٔ Pickup.</param>
/// <param name="DistributionTypeId">شناسهٔ توزیع.</param>
/// <param name="PackingId">شناسهٔ بسته‌بندی.</param>
/// <param name="PackageContentId">شناسهٔ محتوا.</param>
/// <param name="PackTypeId">شناسهٔ نوع پک.</param>
/// <param name="OriginExternalCityCode">کد شهر مبدأ خارجی.</param>
/// <param name="OriginAddressLine">آدرس مبدأ یا تهی.</param>
/// <param name="OriginLatitude">عرض جغرافیایی یا تهی.</param>
/// <param name="OriginLongitude">طول جغرافیایی یا تهی.</param>
/// <param name="DefaultLengthCentimeters">طول پیش‌فرض بسته.</param>
/// <param name="DefaultWidthCentimeters">عرض پیش‌فرض بسته.</param>
/// <param name="DefaultHeightCentimeters">ارتفاع پیش‌فرض بسته.</param>
/// <param name="ConfigurationVersion">نسخهٔ پیکربندی حساب در زمان اتصال.</param>
/// <param name="UpdatedAt">زمان به‌روزرسانی.</param>
public sealed record ShippingCarrierMethodBindingView(
    Guid BindingId,
    Guid DraftId,
    Guid CarrierAccountId,
    string ProviderCode,
    string ProviderServiceId,
    string PaymentTypeId,
    string PickupTypeId,
    string DistributionTypeId,
    string PackingId,
    string PackageContentId,
    string PackTypeId,
    string OriginExternalCityCode,
    string? OriginAddressLine,
    decimal? OriginLatitude,
    decimal? OriginLongitude,
    decimal DefaultLengthCentimeters,
    decimal DefaultWidthCentimeters,
    decimal DefaultHeightCentimeters,
    int ConfigurationVersion,
    DateTimeOffset UpdatedAt);

/// <summary>درخواست ایجاد/به‌روزرسانی حساب حامل؛ رمز فقط برای نوشتن راز ارجاع‌شده است.</summary>
/// <param name="ProviderCode">کد provider.</param>
/// <param name="DisplayName">نام نمایشی.</param>
/// <param name="Environment">محیط.</param>
/// <param name="Username">نام کاربری راز یا تهی در به‌روزرسانی بدون چرخش.</param>
/// <param name="Password">رمز راز یا تهی در به‌روزرسانی بدون چرخش.</param>
/// <param name="IsActive">فعال بودن.</param>
/// <param name="ApiBaseUrl">آدرس API اختیاری.</param>
/// <param name="TokenUrl">آدرس توکن اختیاری.</param>
/// <param name="CurrencyCode">ارز اختیاری.</param>
public sealed record ShippingCarrierAccountUpsertRequest(
    string ProviderCode,
    string DisplayName,
    ShippingCarrierEnvironment Environment,
    string? Username,
    string? Password,
    bool IsActive,
    string? ApiBaseUrl = null,
    string? TokenUrl = null,
    string? CurrencyCode = null);

/// <summary>درخواست جایگزینی نگاشت مقصد.</summary>
/// <param name="GeographyCityId">شهر Geography.</param>
/// <param name="ExternalLocationCode">کد خارجی.</param>
/// <param name="Provenance">منبع.</param>
public sealed record ShippingCarrierDestinationMappingUpsertRequest(
    Guid GeographyCityId,
    string ExternalLocationCode,
    string Provenance);

/// <summary>درخواست جایگزینی اتصال CarrierApi روی پیش‌نویس.</summary>
/// <param name="ExpectedRevision">بازبینی ریشهٔ مورد انتظار.</param>
/// <param name="CarrierAccountId">حساب حامل.</param>
/// <param name="ProviderServiceId">خدمت.</param>
/// <param name="PaymentTypeId">پرداخت.</param>
/// <param name="PickupTypeId">Pickup.</param>
/// <param name="DistributionTypeId">توزیع.</param>
/// <param name="PackingId">بسته‌بندی.</param>
/// <param name="PackageContentId">محتوا.</param>
/// <param name="PackTypeId">نوع پک.</param>
/// <param name="OriginExternalCityCode">شهر مبدأ خارجی.</param>
/// <param name="OriginAddressLine">آدرس مبدأ.</param>
/// <param name="OriginLatitude">عرض.</param>
/// <param name="OriginLongitude">طول.</param>
/// <param name="DefaultLengthCentimeters">طول.</param>
/// <param name="DefaultWidthCentimeters">عرض.</param>
/// <param name="DefaultHeightCentimeters">ارتفاع.</param>
public sealed record ShippingCarrierMethodBindingReplaceRequest(
    long ExpectedRevision,
    Guid CarrierAccountId,
    string ProviderServiceId,
    string PaymentTypeId,
    string PickupTypeId,
    string DistributionTypeId,
    string PackingId,
    string PackageContentId,
    string PackTypeId,
    string OriginExternalCityCode,
    string? OriginAddressLine,
    decimal? OriginLatitude,
    decimal? OriginLongitude,
    decimal DefaultLengthCentimeters,
    decimal DefaultWidthCentimeters,
    decimal DefaultHeightCentimeters);
