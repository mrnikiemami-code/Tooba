using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Geography.Domain.Aggregates;
using Tooba.Geography.Infrastructure.Persistence;
using Tooba.Localization.Contracts.Ports;

namespace Tooba.Geography.Infrastructure.Development;

/// <summary>
/// Bounded, idempotent Development commissioning of the MINIMUM real reference geography a task
/// installation needs in order to configure shipping: Iran, one Tehran province and one Tehran city,
/// each with real registered FA/EN name translations.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ownership.</b> This lives in <c>Geography.Infrastructure</c> and writes ONLY through the module's own
/// <see cref="GeographyDbContext"/> and existing Domain factories. Language identities are read through the
/// sanctioned <see cref="ILanguageLookup"/> Contracts seam — never from Localization's Domain/Application/
/// Infrastructure — and no language id is invented.
/// </para>
/// <para>
/// <b>Codes are seed codes, not official ones.</b> <see cref="TehranAreaCode"/> and
/// <see cref="TehranCityCode"/> are deliberately documented local seed codes; they are NOT claimed to be
/// ISO 3166-2 subdivisions. The country code <c>IR</c> is the real ISO 3166-1 alpha-2 code.
/// </para>
/// <para>
/// <b>Idempotent, create-missing only.</b> Matching is by natural keys (country code, country-scoped area code,
/// country/area-scoped city code, and <c>(place, language)</c> translations). An existing row is never renamed,
/// reparented, reactivated or deleted, so ids and operator edits survive every restart. A contradictory existing
/// shape (for example a Tehran city code attached to a different area) refuses explicitly instead of creating a
/// second apparent place.
/// </para>
/// </remarks>
public static class GeographyDevelopmentSeed
{
    /// <summary>Real ISO 3166-1 alpha-2 code of Iran.</summary>
    public const string IranCountryCode = "IR";

    /// <summary>Documented local Development seed code of the Tehran province row.</summary>
    public const string TehranAreaCode = "IR-DEV-TEH";

    /// <summary>Documented local Development seed code of the Tehran city row.</summary>
    public const string TehranCityCode = "IR-DEV-TEH-THR";

    /// <summary>Persian display name of Iran.</summary>
    public const string IranNameFa = "ایران";

    /// <summary>English display name of Iran.</summary>
    public const string IranNameEn = "Iran";

    /// <summary>Persian display name of Tehran.</summary>
    public const string TehranNameFa = "تهران";

    /// <summary>English display name of Tehran.</summary>
    public const string TehranNameEn = "Tehran";

    /// <summary>Language codes that must already exist in the real registry before seeding.</summary>
    public static readonly string[] RequiredLanguageCodes = ["fa-IR", "en-US"];

    /// <summary>
    /// Ensures the bounded seed geography exists. Resolves its own context, so the caller must already be
    /// inside an assigned commerce scope; Development/marketplace gating belongs to the composing host.
    /// </summary>
    /// <param name="provider">Scope service provider inside the assigned commerce context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<GeographyDevSeedOutcome> EnsureAsync(
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var languages = provider.GetRequiredService<ILanguageLookup>();
        var registered = await languages.ListAsync(cancellationToken);
        var languageIdByCode = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var language in registered)
        {
            languageIdByCode[language.Code] = language.LanguageId;
        }

        foreach (var required in RequiredLanguageCodes)
        {
            if (!languageIdByCode.ContainsKey(required))
            {
                // Localization commissions the canonical set first; absence is a composition error, never a
                // reason to invent a language identity here.
                return GeographyDevSeedOutcome.LanguagesMissing;
            }
        }

        var db = provider.GetRequiredService<GeographyDbContext>();
        var now = DateTimeOffset.UtcNow;

        // TB240-R2: VALIDATE the existing shape BEFORE staging any new row. A contradictory existing shape used
        // to return with newly added Country/Area/City still pending in the change tracker, so any later
        // SaveChanges on the same scope could silently publish a partially-staged seed after an early refusal.
        var country = await db.Countries.SingleOrDefaultAsync(x => x.CountryCode == IranCountryCode, cancellationToken);
        AdministrativeArea? area = null;
        City? city = null;
        if (country is not null)
        {
            area = await db.AdministrativeAreas
                .SingleOrDefaultAsync(
                    x => x.CountryId == country.CountryId && x.Code == TehranAreaCode,
                    cancellationToken);
            city = await db.Cities
                .SingleOrDefaultAsync(
                    x => x.CountryId == country.CountryId && x.Code == TehranCityCode,
                    cancellationToken);

            if (area is not null && area.ParentAdministrativeAreaId is not null)
            {
                return GeographyDevSeedOutcome.ContradictoryExistingShape;
            }

            if (city is not null
                && (area is null || city.AdministrativeAreaId != area.AdministrativeAreaId))
            {
                // A city carrying the seed code must belong to the seed province; never create a second
                // apparent place beside it.
                return GeographyDevSeedOutcome.ContradictoryExistingShape;
            }
        }

        var changed = false;
        if (country is null)
        {
            country = Country.Create(IranCountryCode, now);
            db.Countries.Add(country);
            changed = true;
        }

        if (area is null)
        {
            area = AdministrativeArea.Create(country.CountryId, TehranAreaCode, parentAdministrativeAreaId: null, now);
            db.AdministrativeAreas.Add(area);
            changed = true;
        }

        if (city is null)
        {
            city = City.Create(country.CountryId, area.AdministrativeAreaId, TehranCityCode, now);
            db.Cities.Add(city);
            changed = true;
        }
        else if (city.CountryId != country.CountryId || city.AdministrativeAreaId != area.AdministrativeAreaId)
        {
            return GeographyDevSeedOutcome.ContradictoryExistingShape;
        }

        changed |= await EnsureCountryNameAsync(db, country.CountryId, languageIdByCode, now, cancellationToken);
        changed |= await EnsureAreaNameAsync(db, area.AdministrativeAreaId, languageIdByCode, now, cancellationToken);
        changed |= await EnsureCityNameAsync(db, city.CityId, languageIdByCode, now, cancellationToken);

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return new GeographyDevSeedOutcome(
            GeographyDevSeedStatus.Completed,
            Created: changed,
            CountryId: country.CountryId,
            AdministrativeAreaId: area.AdministrativeAreaId,
            CityId: city.CityId);
    }

    private static async Task<bool> EnsureCountryNameAsync(
        GeographyDbContext db,
        Guid countryId,
        IReadOnlyDictionary<string, Guid> languageIdByCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var changed = false;
        foreach (var (code, name) in new[] { ("fa-IR", IranNameFa), ("en-US", IranNameEn) })
        {
            var languageId = languageIdByCode[code];
            var exists = await db.CountryNameTranslations
                .AnyAsync(x => x.CountryId == countryId && x.LanguageId == languageId, cancellationToken);
            if (!exists)
            {
                db.CountryNameTranslations.Add(CountryNameTranslation.Create(countryId, languageId, name, now));
                changed = true;
            }
        }

        return changed;
    }

    private static async Task<bool> EnsureAreaNameAsync(
        GeographyDbContext db,
        Guid areaId,
        IReadOnlyDictionary<string, Guid> languageIdByCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var changed = false;
        foreach (var (code, name) in new[] { ("fa-IR", TehranNameFa), ("en-US", TehranNameEn) })
        {
            var languageId = languageIdByCode[code];
            var exists = await db.AdministrativeAreaNameTranslations
                .AnyAsync(x => x.AdministrativeAreaId == areaId && x.LanguageId == languageId, cancellationToken);
            if (!exists)
            {
                db.AdministrativeAreaNameTranslations.Add(
                    AdministrativeAreaNameTranslation.Create(areaId, languageId, name, now));
                changed = true;
            }
        }

        return changed;
    }

    private static async Task<bool> EnsureCityNameAsync(
        GeographyDbContext db,
        Guid cityId,
        IReadOnlyDictionary<string, Guid> languageIdByCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var changed = false;
        foreach (var (code, name) in new[] { ("fa-IR", TehranNameFa), ("en-US", TehranNameEn) })
        {
            var languageId = languageIdByCode[code];
            var exists = await db.CityNameTranslations
                .AnyAsync(x => x.CityId == cityId && x.LanguageId == languageId, cancellationToken);
            if (!exists)
            {
                db.CityNameTranslations.Add(CityNameTranslation.Create(cityId, languageId, name, now));
                changed = true;
            }
        }

        return changed;
    }
}

/// <summary>Explicit result kind of the bounded Development geography commissioning.</summary>
public enum GeographyDevSeedStatus
{
    /// <summary>Seed geography is present; <c>Created</c> says whether this call added rows.</summary>
    Completed = 0,

    /// <summary>The canonical languages were not registered yet; nothing was written.</summary>
    LanguagesMissing = 1,

    /// <summary>Existing rows contradict the seed shape; nothing was written or repaired.</summary>
    ContradictoryExistingShape = 2,
}

/// <summary>Outcome of the bounded Development geography commissioning.</summary>
/// <param name="Status">Explicit result kind.</param>
/// <param name="Created">True when at least one row was added by this call.</param>
/// <param name="CountryId">Real country identity (set only when the seed is complete).</param>
/// <param name="AdministrativeAreaId">Real province identity.</param>
/// <param name="CityId">Real city identity.</param>
public sealed record GeographyDevSeedOutcome(
    GeographyDevSeedStatus Status,
    bool Created,
    Guid? CountryId,
    Guid? AdministrativeAreaId,
    Guid? CityId)
{
    /// <summary>The canonical languages were not registered yet; nothing was written.</summary>
    public static readonly GeographyDevSeedOutcome LanguagesMissing =
        new(GeographyDevSeedStatus.LanguagesMissing, false, null, null, null);

    /// <summary>Existing rows contradict the seed shape; nothing was written or repaired.</summary>
    public static readonly GeographyDevSeedOutcome ContradictoryExistingShape =
        new(GeographyDevSeedStatus.ContradictoryExistingShape, false, null, null, null);
}
