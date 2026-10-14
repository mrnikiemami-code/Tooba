namespace Tooba.Geography.Contracts.Places;

/// <summary>
/// درز فقط-خواندنی فهرست مکان‌نگاری برای مصرف بین‌ماژولی.
/// هیچ DbContext یا موجودیت دامنه‌ای در اینجا نمایانده نمی‌شود و هیچ درز نوشتنی وجود ندارد.
/// </summary>
public interface IGeographyLookup
{
    /// <summary>فهرست کشورها را برمی‌گرداند.</summary>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyList<CountrySnapshot>> ListCountriesAsync(CancellationToken cancellationToken);

    /// <summary>ناحیه‌های اداری یک کشور را برمی‌گرداند.</summary>
    /// <param name="countryId">شناسهٔ کشور.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyList<AdministrativeAreaSnapshot>> ListAdministrativeAreasAsync(
        Guid countryId,
        CancellationToken cancellationToken);

    /// <summary>شهرهای یک کشور، به‌صورت اختیاری محدود به یک ناحیهٔ اداری.</summary>
    /// <param name="countryId">شناسهٔ کشور.</param>
    /// <param name="administrativeAreaId">شناسهٔ ناحیهٔ اداری اختیاری.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyList<CitySnapshot>> ListCitiesAsync(
        Guid countryId,
        Guid? administrativeAreaId,
        CancellationToken cancellationToken);

    /// <summary>ناحیه‌های خدمت یک شهر را برمی‌گرداند.</summary>
    /// <param name="cityId">شناسهٔ شهر.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyList<ServiceAreaSnapshot>> ListServiceAreasAsync(
        Guid cityId,
        CancellationToken cancellationToken);

    /// <summary>شناسه‌های مقصد را اعتبارسنجی و در صورت معتبر بودن بازمی‌گرداند.</summary>
    /// <param name="identifiers">شناسه‌های مقصد.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<DestinationIdentifiers?> GetByIdsAsync(
        DestinationIdentifiers identifiers,
        CancellationToken cancellationToken);

    /// <summary>
    /// ورودی متنی قدیمی (کشور/ناحیه/شهر/ناحیهٔ خدمت) را به شناسه‌های پایدار نگاشت می‌کند.
    /// این عملیات هویت نمی‌سازد و در صورت ابهام یا نبود نگاشت، نتیجهٔ امن برمی‌گرداند.
    /// </summary>
    /// <param name="countryCode">کد کشور متنی.</param>
    /// <param name="administrativeAreaName">نام متنی ناحیهٔ اداری.</param>
    /// <param name="cityName">نام متنی شهر.</param>
    /// <param name="serviceAreaName">نام متنی ناحیهٔ خدمت.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<DestinationResolution> ResolveLegacyAsync(
        string? countryCode,
        string? administrativeAreaName,
        string? cityName,
        string? serviceAreaName,
        CancellationToken cancellationToken);
}
