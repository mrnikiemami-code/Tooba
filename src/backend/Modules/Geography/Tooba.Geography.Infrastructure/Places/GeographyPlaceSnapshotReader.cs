using Microsoft.EntityFrameworkCore;
using Tooba.Geography.Contracts.Places;
using Tooba.Geography.Infrastructure.Persistence;

namespace Tooba.Geography.Infrastructure.Places;

/// <summary>
/// پیاده‌سازی فقط-خواندنی <see cref="IGeographyPlaceSnapshotReader"/> روی <see cref="GeographyDbContext"/>
/// خودِ این ماژول.
/// </summary>
/// <remarks>
/// <para>
/// هر پرس‌وجو برای همان محورِ صریحاً درخواستی و روی ستون هویت پایدار فیلتر می‌شود؛ هیچ اسکن جهانی،
/// هیچ صفحه‌بندی، هیچ join میان‌طرح‌واره‌ای، هیچ DbContext ماژول دیگر و هیچ cache سراسری اینجا نیست.
/// </para>
/// <para>
/// <b>Empty and duplicate input.</b> درخواست خالی <b>هیچ</b> کوئری‌ای اجرا نمی‌کند و فهرست خالی
/// برمی‌گرداند؛ شناسه‌های تکراری یک‌بار خوانده می‌شوند. شناسهٔ <c>Guid.Empty</c> یک هویت واقعی نیست و
/// حذف می‌شود.
/// </para>
/// <para>
/// <b>Honest facts.</b> تنها سطرهای موجود برگردانده می‌شوند؛ غیبت یک شناسه یعنی «چنین هویتی در این
/// scope نیست» و خطای provider یا لغو فراخوان عیناً منتشر می‌شود، هرگز به فهرست خالی تبدیل نمی‌شود.
/// ردیف غیرفعال با <c>IsActive = false</c> برگردانده می‌شود؛ پنهان‌کردن آن تصمیم مصرف‌کننده است.
/// </para>
/// <para>
/// این کلاس هیچ write، migration، seed یا تغییر schema ندارد و تنها از طریق DI همین ماژول ثبت می‌شود.
/// </para>
/// </remarks>
public sealed class GeographyPlaceSnapshotReader(GeographyDbContext db) : IGeographyPlaceSnapshotReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CountrySnapshot>> GetCountriesAsync(
        IReadOnlyCollection<Guid> countryIds,
        CancellationToken cancellationToken)
    {
        var ids = DistinctIdentifiers(countryIds);
        if (ids.Count == 0)
        {
            return Array.Empty<CountrySnapshot>();
        }

        var rows = await db.Countries.AsNoTracking()
            .Where(x => ids.Contains(x.CountryId))
            .OrderBy(x => x.CountryCode)
            .ThenBy(x => x.CountryId)
            .ToListAsync(cancellationToken);
        return rows.Select(x => new CountrySnapshot(x.CountryId, x.CountryCode, x.IsActive)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdministrativeAreaSnapshot>> GetAdministrativeAreasAsync(
        IReadOnlyCollection<Guid> administrativeAreaIds,
        CancellationToken cancellationToken)
    {
        var ids = DistinctIdentifiers(administrativeAreaIds);
        if (ids.Count == 0)
        {
            return Array.Empty<AdministrativeAreaSnapshot>();
        }

        var rows = await db.AdministrativeAreas.AsNoTracking()
            .Where(x => ids.Contains(x.AdministrativeAreaId))
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
    public async Task<IReadOnlyList<CitySnapshot>> GetCitiesAsync(
        IReadOnlyCollection<Guid> cityIds,
        CancellationToken cancellationToken)
    {
        var ids = DistinctIdentifiers(cityIds);
        if (ids.Count == 0)
        {
            return Array.Empty<CitySnapshot>();
        }

        var rows = await db.Cities.AsNoTracking()
            .Where(x => ids.Contains(x.CityId))
            .OrderBy(x => x.Code)
            .ThenBy(x => x.CityId)
            .ToListAsync(cancellationToken);
        return rows
            .Select(x => new CitySnapshot(x.CityId, x.CountryId, x.AdministrativeAreaId, x.Code, x.IsActive))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceAreaSnapshot>> GetServiceAreasAsync(
        IReadOnlyCollection<Guid> serviceAreaIds,
        CancellationToken cancellationToken)
    {
        var ids = DistinctIdentifiers(serviceAreaIds);
        if (ids.Count == 0)
        {
            return Array.Empty<ServiceAreaSnapshot>();
        }

        var rows = await db.ServiceAreas.AsNoTracking()
            .Where(x => ids.Contains(x.ServiceAreaId))
            .OrderBy(x => x.Code)
            .ThenBy(x => x.ServiceAreaId)
            .ToListAsync(cancellationToken);
        return rows
            .Select(x => new ServiceAreaSnapshot(x.ServiceAreaId, x.CityId, x.Code, x.IsActive))
            .ToList();
    }

    /// <summary>
    /// شناسه‌های واقعی و یکتا؛ <c>null</c> مجموعه یک درخواست خالی است و <c>Guid.Empty</c> هرگز یک
    /// هویت واقعی نیست.
    /// </summary>
    private static HashSet<Guid> DistinctIdentifiers(IReadOnlyCollection<Guid> identifiers)
    {
        if (identifiers is null || identifiers.Count == 0)
        {
            return [];
        }

        var ids = new HashSet<Guid>(identifiers.Count);
        foreach (var id in identifiers)
        {
            if (id != Guid.Empty)
            {
                ids.Add(id);
            }
        }

        return ids;
    }
}
