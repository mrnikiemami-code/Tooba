using Microsoft.EntityFrameworkCore;
using Tooba.Geography.Contracts.Places;
using Tooba.Geography.Infrastructure.Persistence;

namespace Tooba.Geography.Infrastructure.Places;

/// <summary>
/// پیاده‌سازی فقط-خواندنی <see cref="IGeographyLookup"/> روی <see cref="GeographyDbContext"/> خودِ ماژول
/// (بدون هیچ DbContext ماژول دیگر و بدون هر مسیر نوشتنی).
/// </summary>
/// <remarks>
/// <para>
/// <b>Semantics declared by this adapter (source facts vs. proposed semantics).</b> Every value returned
/// here is a stored column, never a derived guess, except for the two documented normalisations below.
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <b>Listing.</b> The three list operations return <em>all</em> rows of the requested scope
/// (<c>IsActive</c> included as a flag, inactive rows are NOT hidden). The active state is a filter
/// decision that belongs to the consumer, and this seam must not silently hide rows or invent a policy.
/// Ordering is total and deterministic (<c>Code</c> then stable id) so callers and tests see a stable
/// sequence.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>GetByIds.</b> A destination is accepted only when the country exists and is active, every provided
/// child exists, is active and belongs to the same ancestry as requested (area in the same country; city
/// in the same country and, when an area was given, exactly in that area; service area owned by the given
/// city). <c>Guid.Empty</c> in any slot is rejected rather than silently dropped, and a service area
/// without its owning city id is rejected because its ancestry cannot then be validated. Returning
/// <see langword="null"/> therefore means "this destination is not acceptable", which is the fail-closed
/// reading of "validated identifiers" (requiring the active state is a stricter choice than surfacing it;
/// <c>IsActive</c> is deliberately not part of <see cref="DestinationIdentifiers"/>).
/// </description>
/// </item>
/// </list>
/// <para>
/// The read side takes no lock, opens no transaction and performs no write. Nothing here is registered in
/// DI or exposed by an endpoint (Host wiring is out of scope for this Task).
/// </para>
/// </remarks>
public sealed partial class GeographyPlaceLookup : IGeographyLookup
{
    private readonly GeographyDbContext _db;

    /// <summary>نمونه‌ساز با DbContext ماژول.</summary>
    /// <param name="db">DbContext مکان‌نگاری همین ماژول.</param>
    public GeographyPlaceLookup(GeographyDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<CountrySnapshot>> ListCountriesAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Countries.AsNoTracking()
            .OrderBy(x => x.CountryCode)
            .ThenBy(x => x.CountryId)
            .ToListAsync(cancellationToken);
        return rows.Select(x => new CountrySnapshot(x.CountryId, x.CountryCode, x.IsActive)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdministrativeAreaSnapshot>> ListAdministrativeAreasAsync(
        Guid countryId,
        CancellationToken cancellationToken)
    {
        if (countryId == Guid.Empty)
        {
            return Array.Empty<AdministrativeAreaSnapshot>();
        }

        var rows = await _db.AdministrativeAreas.AsNoTracking()
            .Where(x => x.CountryId == countryId)
            .OrderBy(x => x.Code)
            .ThenBy(x => x.AdministrativeAreaId)
            .ToListAsync(cancellationToken);
        return rows
            .Select(x => new AdministrativeAreaSnapshot(
                x.AdministrativeAreaId,
                x.CountryId,
                x.ParentAdministrativeAreaId,
                x.Code,
                x.IsActive))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CitySnapshot>> ListCitiesAsync(
        Guid countryId,
        Guid? administrativeAreaId,
        CancellationToken cancellationToken)
    {
        if (countryId == Guid.Empty)
        {
            return Array.Empty<CitySnapshot>();
        }

        var query = _db.Cities.AsNoTracking().Where(x => x.CountryId == countryId);
        if (administrativeAreaId is { } areaId && areaId != Guid.Empty)
        {
            // The composite foreign key (administrative_area_id, country_id) already guarantees that a
            // matching area lives in this country, so the filter stays inside one country by construction.
            query = query.Where(x => x.AdministrativeAreaId == areaId);
        }

        var rows = await query
            .OrderBy(x => x.Code)
            .ThenBy(x => x.CityId)
            .ToListAsync(cancellationToken);
        return rows
            .Select(x => new CitySnapshot(x.CityId, x.CountryId, x.AdministrativeAreaId, x.Code, x.IsActive))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceAreaSnapshot>> ListServiceAreasAsync(
        Guid cityId,
        CancellationToken cancellationToken)
    {
        if (cityId == Guid.Empty)
        {
            return Array.Empty<ServiceAreaSnapshot>();
        }

        var rows = await _db.ServiceAreas.AsNoTracking()
            .Where(x => x.CityId == cityId)
            .OrderBy(x => x.Code)
            .ThenBy(x => x.ServiceAreaId)
            .ToListAsync(cancellationToken);
        return rows
            .Select(x => new ServiceAreaSnapshot(x.ServiceAreaId, x.CityId, x.Code, x.IsActive))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<DestinationIdentifiers?> GetByIdsAsync(
        DestinationIdentifiers identifiers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identifiers);

        if (identifiers.CountryId == Guid.Empty
            || identifiers.AdministrativeAreaId == Guid.Empty
            || identifiers.CityId == Guid.Empty
            || identifiers.ServiceAreaId == Guid.Empty)
        {
            return null;
        }

        var country = await _db.Countries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CountryId == identifiers.CountryId, cancellationToken);
        if (country is null || !country.IsActive)
        {
            return null;
        }

        if (identifiers.AdministrativeAreaId is { } areaId)
        {
            var area = await _db.AdministrativeAreas.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AdministrativeAreaId == areaId, cancellationToken);
            if (area is null || !area.IsActive || area.CountryId != identifiers.CountryId)
            {
                return null;
            }
        }

        if (identifiers.CityId is { } cityId)
        {
            var city = await _db.Cities.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CityId == cityId, cancellationToken);
            if (city is null || !city.IsActive || city.CountryId != identifiers.CountryId)
            {
                return null;
            }

            if (identifiers.AdministrativeAreaId is { } expectedAreaId
                && city.AdministrativeAreaId != expectedAreaId)
            {
                return null;
            }
        }

        if (identifiers.ServiceAreaId is { } serviceAreaId)
        {
            if (identifiers.CityId is null)
            {
                // A service area is owned by a city; without that city its ancestry cannot be validated.
                return null;
            }

            var serviceArea = await _db.ServiceAreas.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ServiceAreaId == serviceAreaId, cancellationToken);
            if (serviceArea is null || !serviceArea.IsActive || serviceArea.CityId != identifiers.CityId)
            {
                return null;
            }
        }

        return identifiers;
    }
}
