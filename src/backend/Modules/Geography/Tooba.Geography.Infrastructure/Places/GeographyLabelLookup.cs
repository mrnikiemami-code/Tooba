using Microsoft.EntityFrameworkCore;
using Tooba.Geography.Contracts.Places;
using Tooba.Geography.Infrastructure.Persistence;
using Tooba.Localization.Contracts.Ports;

namespace Tooba.Geography.Infrastructure.Places;

/// <summary>
/// پیاده‌سازی فقط-خواندنی <see cref="IGeographyLabelLookup"/> روی ترجمه‌های خودِ ماژول مکان‌نگاری.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ownership.</b> The label rows are Geography data (<c>*_name_translations</c>, one table per place kind,
/// real parent foreign key, unique <c>(place, language)</c>). The <em>language set and the default language</em>
/// are Localization data and are read only through the sanctioned Contracts port
/// <see cref="ILanguageLookup"/> (ADR-036 D9/E14: "Geography uses <c>ILanguageLookup</c> to know the language
/// set and default; never owns the language list"). No Localization DbContext is read, no global translation
/// service is invented, and no language fact is duplicated.
/// </para>
/// <para>
/// <b>Fallback chain (ADR-036 D9), declared explicitly:</b>
/// <c>requested language → default language (IsDefault) → stable code</c>.
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// A requested <c>LanguageId</c> is honoured only when the Localization port actually knows that language.
/// An unknown/fabricated id is not used to surface a stale translation row: the chain falls through instead.
/// </description>
/// </item>
/// <item>
/// <description>
/// The default step uses <c>IsDefault</c> only. No "first language wins" fallback was added, because
/// selecting an arbitrary language is exactly the kind of invented policy this seam must avoid.
/// </description>
/// </item>
/// <item>
/// <description>
/// The last step is the place's own stable <c>Code</c>, matching the established repo convention
/// (<c>picked?.Name ?? u.Code</c> in Catalog's unit directory). If even the code is unavailable - i.e. the id
/// does not exist at all - the id is <em>omitted</em> from the result; a label is never fabricated.
/// </description>
/// </item>
/// </list>
/// <para>
/// Identical labels in different places are irrelevant here: the result is keyed by stable id, and a
/// translation row belongs to exactly one place through a real foreign key. Nothing in this adapter writes,
/// takes a lock or opens a transaction.
/// </para>
/// </remarks>
public sealed class GeographyLabelLookup : IGeographyLabelLookup
{
    private static readonly IReadOnlyDictionary<Guid, string> NoLabels = new Dictionary<Guid, string>();

    private readonly GeographyDbContext _db;
    private readonly ILanguageLookup _languages;

    /// <summary>نمونه‌ساز با DbContext ماژول و درز زبانی Contracts.</summary>
    /// <param name="db">DbContext مکان‌نگاری همین ماژول.</param>
    /// <param name="languages">درز فقط-خواندنی زبان (Localization.Contracts).</param>
    public GeographyLabelLookup(GeographyDbContext db, ILanguageLookup languages)
    {
        _db = db;
        _languages = languages;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetCountryLabelsAsync(
        IReadOnlyCollection<Guid> countryIds,
        Guid? languageId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(countryIds);
        var ids = Scope(countryIds);
        if (ids.Length == 0)
        {
            return NoLabels;
        }

        var languages = await ResolveLanguagesAsync(languageId, cancellationToken);
        var rows = await _db.CountryNameTranslations.AsNoTracking()
            .Where(x => ids.Contains(x.CountryId))
            .Select(x => new LabelRow(x.CountryId, x.LanguageId, x.Name))
            .ToListAsync(cancellationToken);
        var codes = await _db.Countries.AsNoTracking()
            .Where(x => ids.Contains(x.CountryId))
            .Select(x => new PlaceCode(x.CountryId, x.CountryCode))
            .ToListAsync(cancellationToken);
        return Compose(ids, rows, codes, languages);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetAdministrativeAreaLabelsAsync(
        IReadOnlyCollection<Guid> administrativeAreaIds,
        Guid? languageId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(administrativeAreaIds);
        var ids = Scope(administrativeAreaIds);
        if (ids.Length == 0)
        {
            return NoLabels;
        }

        var languages = await ResolveLanguagesAsync(languageId, cancellationToken);
        var rows = await _db.AdministrativeAreaNameTranslations.AsNoTracking()
            .Where(x => ids.Contains(x.AdministrativeAreaId))
            .Select(x => new LabelRow(x.AdministrativeAreaId, x.LanguageId, x.Name))
            .ToListAsync(cancellationToken);
        var codes = await _db.AdministrativeAreas.AsNoTracking()
            .Where(x => ids.Contains(x.AdministrativeAreaId))
            .Select(x => new PlaceCode(x.AdministrativeAreaId, x.Code))
            .ToListAsync(cancellationToken);
        return Compose(ids, rows, codes, languages);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetCityLabelsAsync(
        IReadOnlyCollection<Guid> cityIds,
        Guid? languageId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cityIds);
        var ids = Scope(cityIds);
        if (ids.Length == 0)
        {
            return NoLabels;
        }

        var languages = await ResolveLanguagesAsync(languageId, cancellationToken);
        var rows = await _db.CityNameTranslations.AsNoTracking()
            .Where(x => ids.Contains(x.CityId))
            .Select(x => new LabelRow(x.CityId, x.LanguageId, x.Name))
            .ToListAsync(cancellationToken);
        var codes = await _db.Cities.AsNoTracking()
            .Where(x => ids.Contains(x.CityId))
            .Select(x => new PlaceCode(x.CityId, x.Code))
            .ToListAsync(cancellationToken);
        return Compose(ids, rows, codes, languages);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetServiceAreaLabelsAsync(
        IReadOnlyCollection<Guid> serviceAreaIds,
        Guid? languageId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceAreaIds);
        var ids = Scope(serviceAreaIds);
        if (ids.Length == 0)
        {
            return NoLabels;
        }

        var languages = await ResolveLanguagesAsync(languageId, cancellationToken);
        var rows = await _db.ServiceAreaNameTranslations.AsNoTracking()
            .Where(x => ids.Contains(x.ServiceAreaId))
            .Select(x => new LabelRow(x.ServiceAreaId, x.LanguageId, x.Name))
            .ToListAsync(cancellationToken);
        var codes = await _db.ServiceAreas.AsNoTracking()
            .Where(x => ids.Contains(x.ServiceAreaId))
            .Select(x => new PlaceCode(x.ServiceAreaId, x.Code))
            .ToListAsync(cancellationToken);
        return Compose(ids, rows, codes, languages);
    }

    private static Guid[] Scope(IReadOnlyCollection<Guid> requestedIds)
        => requestedIds.Where(id => id != Guid.Empty).Distinct().ToArray();

    /// <summary>
    /// زبان‌های مؤثر: زبان درخواستی فقط اگر در فهرست Localization موجود باشد، و زبان پیش‌فرض بر پایهٔ
    /// <c>IsDefault</c> (بدون هیچ انتخاب دلبخواه).
    /// </summary>
    private async Task<(Guid? Honoured, Guid? Default)> ResolveLanguagesAsync(
        Guid? languageId,
        CancellationToken cancellationToken)
    {
        var requested = languageId is { } value && value != Guid.Empty ? value : (Guid?)null;
        var known = await _languages.ListAsync(cancellationToken);
        var honoured = requested is { } candidate && known.Any(x => x.LanguageId == candidate)
            ? candidate
            : (Guid?)null;
        var @default = known.Where(x => x.IsDefault).Select(x => (Guid?)x.LanguageId).FirstOrDefault();
        return (honoured, @default);
    }

    private static IReadOnlyDictionary<Guid, string> Compose(
        IReadOnlyList<Guid> ids,
        IReadOnlyList<LabelRow> rows,
        IReadOnlyList<PlaceCode> codes,
        (Guid? Honoured, Guid? Default) languages)
    {
        var codeMap = codes.GroupBy(x => x.OwnerId).ToDictionary(g => g.Key, g => g.First().StableCode);
        var result = new Dictionary<Guid, string>(ids.Count);
        foreach (var id in ids)
        {
            var placeRows = rows.Where(x => x.OwnerId == id).ToList();
            var label = Pick(placeRows, languages.Honoured)
                ?? Pick(placeRows, languages.Default)
                ?? (codeMap.TryGetValue(id, out var stableCode) ? stableCode : null);
            if (label is not null)
            {
                result[id] = label;
            }
        }

        return result;
    }

    private static string? Pick(IReadOnlyList<LabelRow> rows, Guid? language)
    {
        if (language is not { } languageId)
        {
            return null;
        }

        foreach (var row in rows)
        {
            if (row.LanguageId == languageId)
            {
                return row.Name;
            }
        }

        return null;
    }

    private sealed record LabelRow(Guid OwnerId, Guid LanguageId, string Name);

    private sealed record PlaceCode(Guid OwnerId, string StableCode);
}
