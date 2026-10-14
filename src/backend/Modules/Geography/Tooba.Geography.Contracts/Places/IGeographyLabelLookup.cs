namespace Tooba.Geography.Contracts.Places;

/// <summary>
/// درز فقط-خواندنی نام‌های محلی‌شده. هویت هرگز برچسب محلی نیست؛ برچسب فقط برای نمایش است.
/// سطرهای ترجمه با <c>LanguageId</c> کلید می‌خورند و این ماژول مالک آن‌هاست.
/// </summary>
public interface IGeographyLabelLookup
{
    /// <summary>برچسب‌های محلی‌شدهٔ کشورها را برمی‌گرداند.</summary>
    /// <param name="countryIds">شناسهٔ کشورها.</param>
    /// <param name="languageId">شناسهٔ زبان درخواستی؛ تهی یعنی زبان پیش‌فرض.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyDictionary<Guid, string>> GetCountryLabelsAsync(
        IReadOnlyCollection<Guid> countryIds,
        Guid? languageId,
        CancellationToken cancellationToken);

    /// <summary>برچسب‌های محلی‌شدهٔ ناحیه‌های اداری را برمی‌گرداند.</summary>
    /// <param name="administrativeAreaIds">شناسهٔ ناحیه‌های اداری.</param>
    /// <param name="languageId">شناسهٔ زبان درخواستی؛ تهی یعنی زبان پیش‌فرض.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyDictionary<Guid, string>> GetAdministrativeAreaLabelsAsync(
        IReadOnlyCollection<Guid> administrativeAreaIds,
        Guid? languageId,
        CancellationToken cancellationToken);

    /// <summary>برچسب‌های محلی‌شدهٔ شهرها را برمی‌گرداند.</summary>
    /// <param name="cityIds">شناسهٔ شهرها.</param>
    /// <param name="languageId">شناسهٔ زبان درخواستی؛ تهی یعنی زبان پیش‌فرض.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyDictionary<Guid, string>> GetCityLabelsAsync(
        IReadOnlyCollection<Guid> cityIds,
        Guid? languageId,
        CancellationToken cancellationToken);

    /// <summary>برچسب‌های محلی‌شدهٔ ناحیه‌های خدمت را برمی‌گرداند.</summary>
    /// <param name="serviceAreaIds">شناسهٔ ناحیه‌های خدمت.</param>
    /// <param name="languageId">شناسهٔ زبان درخواستی؛ تهی یعنی زبان پیش‌فرض.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyDictionary<Guid, string>> GetServiceAreaLabelsAsync(
        IReadOnlyCollection<Guid> serviceAreaIds,
        Guid? languageId,
        CancellationToken cancellationToken);
}
