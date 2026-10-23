namespace Tooba.Geography.Contracts.Places;

/// <summary>
/// Bulk read-by-key seam over the EXISTING Geography snapshots, for consumers that hold stored typed
/// destination keys and must learn their real parent facts.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> <see cref="IGeographyLookup"/> can only list a hierarchical scope, so a
/// caller holding an arbitrary stored city/service-area id cannot learn its ancestors without walking
/// whole countries. <see cref="IGeographyLookup.GetByIdsAsync"/> validates a caller-supplied tuple and
/// therefore cannot hydrate an unknown terminal either. This seam answers exactly that question and
/// nothing else: ids in, stored facts out.
/// </para>
/// <para>
/// <b>Contract semantics.</b> Requested ids are de-duplicated; a row is returned only for an id that
/// actually exists in this scope, and inactive rows are returned with <c>IsActive = false</c> rather
/// than hidden (activity is the consumer's policy). An absent id is a legitimate absence, not an
/// error. An empty request performs no query and returns an empty list. Provider failures and
/// cancellation propagate unchanged; nothing here catches them into an empty success.
/// </para>
/// <para>
/// No write, no schema, no catalog, no owner scoping and no label/localization decision lives here:
/// labels stay in <see cref="IGeographyLabelLookup"/> and display policy belongs to the consumer.
/// </para>
/// </remarks>
public interface IGeographyPlaceSnapshotReader
{
    /// <summary>کشورهای درخواستی را با فکت‌های ذخیره‌شده برمی‌گرداند.</summary>
    /// <param name="countryIds">شناسه‌های درخواستی؛ تکراری‌ها یک‌بار خوانده می‌شوند.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyList<CountrySnapshot>> GetCountriesAsync(
        IReadOnlyCollection<Guid> countryIds,
        CancellationToken cancellationToken);

    /// <summary>ناحیه‌های اداری درخواستی را با والد واقعی هر ناحیه برمی‌گرداند.</summary>
    /// <param name="administrativeAreaIds">شناسه‌های درخواستی.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyList<AdministrativeAreaSnapshot>> GetAdministrativeAreasAsync(
        IReadOnlyCollection<Guid> administrativeAreaIds,
        CancellationToken cancellationToken);

    /// <summary>شهرهای درخواستی را با کشور و ناحیهٔ اداری اختیاری واقعی برمی‌گرداند.</summary>
    /// <param name="cityIds">شناسه‌های درخواستی.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyList<CitySnapshot>> GetCitiesAsync(
        IReadOnlyCollection<Guid> cityIds,
        CancellationToken cancellationToken);

    /// <summary>ناحیه‌های خدمت درخواستی را با شهر مالک واقعی برمی‌گرداند.</summary>
    /// <param name="serviceAreaIds">شناسه‌های درخواستی.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyList<ServiceAreaSnapshot>> GetServiceAreasAsync(
        IReadOnlyCollection<Guid> serviceAreaIds,
        CancellationToken cancellationToken);
}
