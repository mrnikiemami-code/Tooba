using Microsoft.EntityFrameworkCore;
using Tooba.Geography.Contracts.Places;
using Tooba.Geography.Infrastructure.Persistence;

namespace Tooba.Geography.Infrastructure.Places;

/// <summary>
/// حل متن قدیمی به شناسه‌های پایدار: کاملاً فقط-خواندنی، قطعی و بدون ساخت هویت.
/// </summary>
/// <remarks>
/// <para>
/// <b>Source facts (data):</b> each place owns a stable <c>Code</c> and, per <c>LanguageId</c>, zero or more
/// rows in its own <c>*_name_translations</c> table (real foreign keys, uniqueness on
/// <c>(place, language)</c>, several languages possible per place).
/// </para>
/// <para>
/// <b>Proposed semantics (this adapter, explicit):</b>
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <b>Exact, normalised comparison - never fuzzy.</b> A candidate matches when its stored text equals the
/// input after <c>Trim()</c> under <see cref="StringComparison.OrdinalIgnoreCase"/> (culture-independent;
/// no accents folding, no substring, no edit distance, no transliteration). Nothing is auto-selected.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Two exact keys per place.</b> The input may equal either the localised label of that place in
/// <em>any</em> language, or its stable <c>Code</c>. Labels are compared across languages and then collapsed
/// per place, so the same label repeated in two languages is one candidate, not two.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Country is required.</b> Legacy destination text always carries a country context, and the stable
/// identity of every place is scoped by <c>CountryId</c>. A blank/unknown country code yields
/// <see cref="DestinationResolutionOutcome.Unresolved"/> instead of a cross-country search that could pick a
/// same-labelled place in another country.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Ambiguity is reported, never collapsed.</b> If a level matches more than one distinct place - for
/// example two cities with the same label in two different administrative areas, or two areas sharing one
/// label inside the same country - the result is <see cref="DestinationResolutionOutcome.Ambiguous"/> with
/// the number of distinct candidates, so a caller can ask for disambiguation. A supplied area therefore acts
/// as a real scope: it is what turns such a duplicated city label into a single unambiguous candidate.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>One factual derivation only.</b> When a city resolves and no area was supplied, the returned
/// identifiers carry the city's own stored <c>AdministrativeAreaId</c> (its actual parent row, not a picked
/// candidate). A service area is resolved only together with its owning city, because its ancestry cannot be
/// validated without it.
/// </description>
/// </item>
/// </list>
/// <para>
/// The resolver never inserts, never creates identity, never writes a migration and never uses a
/// fuzzy/nearest match. Identical labels in different areas are intentionally distinguishable, and an input
/// that matches nothing is <see cref="DestinationResolutionOutcome.Unresolved"/> rather than a guess.
/// </para>
/// </remarks>
public sealed partial class GeographyPlaceLookup
{
    /// <inheritdoc />
    public async Task<DestinationResolution> ResolveLegacyAsync(
        string? countryCode,
        string? administrativeAreaName,
        string? cityName,
        string? serviceAreaName,
        CancellationToken cancellationToken)
    {
        var countryKey = Normalize(countryCode);
        if (countryKey is null)
        {
            return DestinationResolution.Unresolved();
        }

        // Country codes are compared in memory under OrdinalIgnoreCase: the table is a small reference list,
        // and this keeps the comparison identical to the label comparison (a DB-side collation is not a
        // contract of this seam).
        var countries = await _db.Countries.AsNoTracking().ToListAsync(cancellationToken);
        var countryMatches = countries
            .Where(x => Matches(x.CountryCode, countryKey))
            .ToList();
        if (countryMatches.Count == 0)
        {
            return DestinationResolution.Unresolved();
        }

        if (countryMatches.Count > 1)
        {
            // Defensive only: country_code carries a unique index, so this cannot occur in a valid schema.
            return DestinationResolution.Ambiguous(countryMatches.Count);
        }

        var country = countryMatches[0];
        var areaKey = Normalize(administrativeAreaName);
        var cityKey = Normalize(cityName);
        var serviceAreaKey = Normalize(serviceAreaName);

        // ---- administrative area -------------------------------------------
        var areaId = await ResolveAreaAsync(country.CountryId, areaKey, cancellationToken);
        if (areaId.Outcome is not null)
        {
            return areaId.Outcome;
        }

        // ---- city ----------------------------------------------------------
        var city = await ResolveCityAsync(country.CountryId, areaId.Value, cityKey, cancellationToken);
        if (city.Outcome is not null)
        {
            return city.Outcome;
        }

        // ---- service area --------------------------------------------------
        var serviceAreaId = await ResolveServiceAreaAsync(city.Value, serviceAreaKey, cancellationToken);
        if (serviceAreaId.Outcome is not null)
        {
            return serviceAreaId.Outcome;
        }

        return DestinationResolution.Resolved(new DestinationIdentifiers(
            country.CountryId,
            areaId.Value ?? city.AreaId,
            city.Value,
            serviceAreaId.Value));
    }

    private async Task<ResolutionStep> ResolveAreaAsync(
        Guid countryId,
        string? areaKey,
        CancellationToken cancellationToken)
    {
        if (areaKey is null)
        {
            return ResolutionStep.Resolved(null);
        }

        var areas = await _db.AdministrativeAreas.AsNoTracking()
            .Where(x => x.CountryId == countryId)
            .ToListAsync(cancellationToken);
        if (areas.Count == 0)
        {
            return ResolutionStep.Unresolved();
        }

        var areaIds = areas.Select(x => x.AdministrativeAreaId).ToArray();
        var labels = await _db.AdministrativeAreaNameTranslations.AsNoTracking()
            .Where(x => areaIds.Contains(x.AdministrativeAreaId))
            .ToListAsync(cancellationToken);

        var matches = areas
            .Where(area => MatchesAny(
                area.Code,
                areaKey,
                labels.Where(l => l.AdministrativeAreaId == area.AdministrativeAreaId).Select(l => l.Name)))
            .ToList();

        return matches.Count switch
        {
            0 => ResolutionStep.Unresolved(),
            1 => ResolutionStep.Resolved(matches[0].AdministrativeAreaId),
            _ => ResolutionStep.Ambiguous(matches.Count),
        };
    }

    private async Task<ResolutionStep> ResolveCityAsync(
        Guid countryId,
        Guid? areaId,
        string? cityKey,
        CancellationToken cancellationToken)
    {
        if (cityKey is null)
        {
            return ResolutionStep.Resolved(null);
        }

        var cities = await _db.Cities.AsNoTracking()
            .Where(x => x.CountryId == countryId)
            .ToListAsync(cancellationToken);
        var scoped = areaId is { } scope
            ? cities.Where(x => x.AdministrativeAreaId == scope).ToList()
            : cities;
        if (scoped.Count == 0)
        {
            return ResolutionStep.Unresolved();
        }

        var cityIds = scoped.Select(x => x.CityId).ToArray();
        var labels = await _db.CityNameTranslations.AsNoTracking()
            .Where(x => cityIds.Contains(x.CityId))
            .ToListAsync(cancellationToken);

        var matches = scoped
            .Where(city => MatchesAny(
                city.Code,
                cityKey,
                labels.Where(l => l.CityId == city.CityId).Select(l => l.Name)))
            .ToList();

        return matches.Count switch
        {
            0 => ResolutionStep.Unresolved(),
            1 => ResolutionStep.Resolved(matches[0].CityId, matches[0].AdministrativeAreaId),
            _ => ResolutionStep.Ambiguous(matches.Count),
        };
    }

    private async Task<ResolutionStep> ResolveServiceAreaAsync(
        Guid? cityId,
        string? serviceAreaKey,
        CancellationToken cancellationToken)
    {
        if (serviceAreaKey is null)
        {
            return ResolutionStep.Resolved(null);
        }

        if (cityId is not { } resolvedCityId)
        {
            // A service area label alone cannot identify a service area: the same label may exist in many
            // cities, and its ancestry is the city. Refusing here is safer than a cross-city search.
            return ResolutionStep.Unresolved();
        }

        var serviceAreas = await _db.ServiceAreas.AsNoTracking()
            .Where(x => x.CityId == resolvedCityId)
            .ToListAsync(cancellationToken);
        if (serviceAreas.Count == 0)
        {
            return ResolutionStep.Unresolved();
        }

        var serviceAreaIds = serviceAreas.Select(x => x.ServiceAreaId).ToArray();
        var labels = await _db.ServiceAreaNameTranslations.AsNoTracking()
            .Where(x => serviceAreaIds.Contains(x.ServiceAreaId))
            .ToListAsync(cancellationToken);

        var matches = serviceAreas
            .Where(area => MatchesAny(
                area.Code,
                serviceAreaKey,
                labels.Where(l => l.ServiceAreaId == area.ServiceAreaId).Select(l => l.Name)))
            .ToList();

        return matches.Count switch
        {
            0 => ResolutionStep.Unresolved(),
            1 => ResolutionStep.Resolved(matches[0].ServiceAreaId),
            _ => ResolutionStep.Ambiguous(matches.Count),
        };
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static bool Matches(string stored, string key)
        => string.Equals(stored.Trim(), key, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesAny(string storedCode, string key, IEnumerable<string> storedLabels)
        => Matches(storedCode, key) || storedLabels.Any(label => Matches(label, key));

    /// <summary>
    /// نتیجهٔ یک سطح حل: مقدار حل‌شده و/یا ناحیهٔ واقعی آن، و در صورت ابهام/نبود نگاشت، نتیجهٔ نهایی.
    /// </summary>
    private readonly record struct ResolutionStep(Guid? Value, Guid? AreaId, DestinationResolution? Outcome)
    {
        public static ResolutionStep Resolved(Guid? value, Guid? areaId = null) => new(value, areaId, null);

        public static ResolutionStep Ambiguous(int candidateCount)
            => new(null, null, DestinationResolution.Ambiguous(candidateCount));

        public static ResolutionStep Unresolved()
            => new(null, null, DestinationResolution.Unresolved());
    }
}
