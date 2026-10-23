using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Infrastructure.Authoring;

/// <summary>
/// خواندن فکت‌های واقعی مقصد برای کلیدهای typed یک پیش‌نویس، فقط روی درزهای Contracts Geography و
/// برچسب محلی‌شدهٔ همان ماژول. هیچ DbContext، FK یا join میان‌طرح‌واره‌ای اینجا نیست.
/// </summary>
/// <remarks>
/// <para>
/// <b>Bounded, batched, operation-local.</b> شناسه‌ها به تفکیک نوع و یک‌بار خوانده می‌شوند؛ والدها
/// فقط به اندازهٔ لازم batch می‌شوند (بدون پیمایش کل کشورها/شهرها) و هیچ cache یا registry جهانی
/// ساخته نمی‌شود.
/// </para>
/// <para>
/// <b>Honest unresolved.</b> غیبت/غیرفعال‌بودن هویت یا والد، تناقض کشور/والد و چرخه، یک فکت
/// <c>Unresolved</c> بدون هیچ شکل نوشتنی جعلی است و انتخاب خام هرگز حذف نمی‌شود. پاسخ متناقض یا با
/// هویت اشتباه از درز پایین‌دست fail-closed است؛ استثنا/لغو فراخوان عیناً منتشر می‌شود.
/// </para>
/// <para>
/// <b>Labels are display, identity is the key.</b> برچسب از <see cref="IGeographyLabelLookup"/> برای
/// همان زبان پیش‌فرض محتوای authoritative خوانده می‌شود؛ اگر برچسب واقعی نباشد تهی می‌ماند و هیچ
/// نام جعلی، حدس locale یا GUID خام به‌جای نام ساخته نمی‌شود.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringDestinationFactsReader(
    IGeographyPlaceSnapshotReader snapshots,
    IGeographyLabelLookup labels,
    IModuleCallTracer moduleCallTracer) : IShippingAuthoringDestinationFactsReader
{
    private const string SourceModule = "Shipping";
    private const string TargetModule = "Geography";
    private const string CountriesOperation = "GetCountriesByKeysForAuthoringDraft";
    private const string AdministrativeAreasOperation = "GetAdministrativeAreasByKeysForAuthoringDraft";
    private const string CitiesOperation = "GetCitiesByKeysForAuthoringDraft";
    private const string ServiceAreasOperation = "GetServiceAreasByKeysForAuthoringDraft";
    private const string LabelsOperation = "GetDestinationLabelsForAuthoringDraft";

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ShippingAuthoringDestinationFactItem>>> ReadAsync(
        IReadOnlyList<ShippingAuthoringSelectedAreaItem> keys,
        Guid languageId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keys);

        if (languageId == Guid.Empty)
        {
            return Failure();
        }

        var distinct = DistinctKeys(keys);
        if (distinct.Count == 0)
        {
            // Zero selections means zero foreign reads: no country/city/label query at all.
            return Result.Success<IReadOnlyList<ShippingAuthoringDestinationFactItem>>([]);
        }

        var countryKeys = IdsOf(distinct, ShippingAuthoringAreaKind.Country);
        var areaKeys = IdsOf(distinct, ShippingAuthoringAreaKind.AdministrativeArea);
        var cityKeys = IdsOf(distinct, ShippingAuthoringAreaKind.City);
        var serviceAreaKeys = IdsOf(distinct, ShippingAuthoringAreaKind.ServiceArea);

        // (1) The requested terminals themselves, one batched read per kind that is actually used.
        var countries = await ReadCountriesAsync(countryKeys, cancellationToken);
        if (countries is null)
        {
            return Failure();
        }

        var areas = await ReadAreasAsync(areaKeys, cancellationToken);
        if (areas is null)
        {
            return Failure();
        }

        var cities = await ReadCitiesAsync(cityKeys, cancellationToken);
        if (cities is null)
        {
            return Failure();
        }

        var serviceAreas = await ReadServiceAreasAsync(serviceAreaKeys, cancellationToken);
        if (serviceAreas is null)
        {
            return Failure();
        }

        // (2) A service area is owned by a city, so only those owning cities are fetched next.
        var owningCityIds = serviceAreas.Values.Select(area => area.CityId).Where(id => id != Guid.Empty).ToArray();
        var owningCities = await ReadCitiesAsync(owningCityIds, cancellationToken);
        if (owningCities is null)
        {
            return Failure();
        }

        foreach (var city in owningCities.Values)
        {
            cities[city.CityId] = city;
        }

        // (3) Only the countries and administrative parents that a real terminal actually references.
        var requiredCountryIds = new HashSet<Guid>();
        requiredCountryIds.UnionWith(cities.Values.Select(city => city.CountryId));
        requiredCountryIds.UnionWith(areas.Values.Select(area => area.CountryId));

        var requiredAreaIds = new HashSet<Guid>();
        requiredAreaIds.UnionWith(cities.Values.Select(city => city.AdministrativeAreaId).OfType<Guid>());
        requiredAreaIds.UnionWith(areas.Values.Select(area => area.ParentAdministrativeAreaId).OfType<Guid>());

        var parents = await ReadClosureAsync(requiredCountryIds, requiredAreaIds, countries, areas, cancellationToken);
        if (parents is null)
        {
            return Failure();
        }

        (countries, areas) = parents.Value;

        // (4) One label batch per kind for the requested terminal identities only.
        var labelMap = await ReadLabelsAsync(distinct, languageId, cancellationToken);
        if (labelMap is null)
        {
            return Failure();
        }

        var facts = new List<ShippingAuthoringDestinationFactItem>(distinct.Count);
        foreach (var key in distinct)
        {
            facts.Add(Resolve(key, countries, areas, cities, serviceAreas, labelMap));
        }

        return Result.Success<IReadOnlyList<ShippingAuthoringDestinationFactItem>>(facts);
    }

    private static ShippingAuthoringDestinationFactItem Resolve(
        ShippingAuthoringSelectedAreaItem key,
        IReadOnlyDictionary<Guid, CountrySnapshot> countries,
        IReadOnlyDictionary<Guid, AdministrativeAreaSnapshot> areas,
        IReadOnlyDictionary<Guid, CitySnapshot> cities,
        IReadOnlyDictionary<Guid, ServiceAreaSnapshot> serviceAreas,
        IReadOnlyDictionary<(ShippingAuthoringAreaKind Kind, Guid Id), string?> labels)
    {
        var label = labels.TryGetValue((key.Kind, key.Id), out var found) ? found : null;

        switch (key.Kind)
        {
            case ShippingAuthoringAreaKind.Country:
            {
                if (!countries.TryGetValue(key.Id, out var country) || !country.IsActive)
                {
                    return Unresolved(key, label);
                }

                return Resolved(
                    key,
                    new ShippingAuthoringDestinationWriteShape(
                        ShippingAuthoringAreaKind.Country,
                        country.CountryId,
                        null,
                        null,
                        null),
                    label);
            }

            case ShippingAuthoringAreaKind.AdministrativeArea:
            {
                if (!areas.TryGetValue(key.Id, out var area) || !area.IsActive
                    || !countries.TryGetValue(area.CountryId, out var country) || !country.IsActive)
                {
                    return Unresolved(key, label);
                }

                if (!ShippingDestinationAncestry.TryBuildAdministrativeChain(
                        areas,
                        area.AdministrativeAreaId,
                        area.CountryId,
                        []))
                {
                    return Unresolved(key, label);
                }

                return Resolved(
                    key,
                    new ShippingAuthoringDestinationWriteShape(
                        ShippingAuthoringAreaKind.AdministrativeArea,
                        area.CountryId,
                        area.AdministrativeAreaId,
                        null,
                        null),
                    label);
            }

            case ShippingAuthoringAreaKind.City:
            {
                if (!cities.TryGetValue(key.Id, out var city) || !city.IsActive
                    || !countries.TryGetValue(city.CountryId, out var country) || !country.IsActive)
                {
                    return Unresolved(key, label);
                }

                if (city.AdministrativeAreaId is { } areaId)
                {
                    if (!areas.TryGetValue(areaId, out var area) || !area.IsActive || area.CountryId != city.CountryId)
                    {
                        return Unresolved(key, label);
                    }

                    if (!ShippingDestinationAncestry.TryBuildAdministrativeChain(areas, areaId, city.CountryId, []))
                    {
                        return Unresolved(key, label);
                    }
                }

                return Resolved(
                    key,
                    new ShippingAuthoringDestinationWriteShape(
                        ShippingAuthoringAreaKind.City,
                        city.CountryId,
                        city.AdministrativeAreaId,
                        city.CityId,
                        null),
                    label);
            }

            case ShippingAuthoringAreaKind.ServiceArea:
            {
                if (!serviceAreas.TryGetValue(key.Id, out var serviceArea)
                    || !cities.TryGetValue(serviceArea.CityId, out var city) || !city.IsActive
                    || !countries.TryGetValue(city.CountryId, out var country) || !country.IsActive)
                {
                    return Unresolved(key, label);
                }

                if (city.AdministrativeAreaId is { } areaId)
                {
                    if (!areas.TryGetValue(areaId, out var area) || !area.IsActive || area.CountryId != city.CountryId)
                    {
                        return Unresolved(key, label);
                    }

                    if (!ShippingDestinationAncestry.TryBuildAdministrativeChain(areas, areaId, city.CountryId, []))
                    {
                        return Unresolved(key, label);
                    }
                }

                return Resolved(
                    key,
                    new ShippingAuthoringDestinationWriteShape(
                        ShippingAuthoringAreaKind.ServiceArea,
                        city.CountryId,
                        city.AdministrativeAreaId,
                        city.CityId,
                        serviceArea.ServiceAreaId),
                    label);
            }

            default:
                return Unresolved(key, label);
        }
    }

    private static ShippingAuthoringDestinationFactItem Resolved(
        ShippingAuthoringSelectedAreaItem key,
        ShippingAuthoringDestinationWriteShape shape,
        string? label)
        => new(key.Kind, key.Id, ShippingAuthoringDestinationResolution.Resolved, shape, label);

    private static ShippingAuthoringDestinationFactItem Unresolved(
        ShippingAuthoringSelectedAreaItem key,
        string? label)
        => new(key.Kind, key.Id, ShippingAuthoringDestinationResolution.Unresolved, null, label);

    /// <summary>
    /// شناسه‌های typed و متمایز؛ جفت (نوع، شناسه) کلید است، پس یک GUID یکسان در دو نوع دو هویت
    /// مستقل می‌ماند. <c>Guid.Empty</c> هرگز یک هویت واقعی نیست.
    /// </summary>
    private static List<ShippingAuthoringSelectedAreaItem> DistinctKeys(
        IReadOnlyList<ShippingAuthoringSelectedAreaItem> keys)
    {
        var seen = new HashSet<(ShippingAuthoringAreaKind Kind, Guid Id)>();
        var distinct = new List<ShippingAuthoringSelectedAreaItem>(keys.Count);
        foreach (var key in keys)
        {
            if (key is null || key.Id == Guid.Empty)
            {
                continue;
            }

            if (seen.Add((key.Kind, key.Id)))
            {
                distinct.Add(key);
            }
        }

        return distinct;
    }

    private static HashSet<Guid> IdsOf(
        List<ShippingAuthoringSelectedAreaItem> keys,
        ShippingAuthoringAreaKind kind)
        => keys.Where(key => key.Kind == kind).Select(key => key.Id).ToHashSet();

    /// <summary>
    /// والدها را تا بستهٔ کامل (ناحیه‌های اداری بازگشتی) batch می‌خواند. چرخه با مجموعهٔ دیده‌شده
    /// محدود می‌ماند و پیمایش نامحدود نمی‌شود.
    /// </summary>
    private async Task<(Dictionary<Guid, CountrySnapshot>, Dictionary<Guid, AdministrativeAreaSnapshot>)?> ReadClosureAsync(
        HashSet<Guid> countryIds,
        HashSet<Guid> areaIds,
        Dictionary<Guid, CountrySnapshot> knownCountries,
        Dictionary<Guid, AdministrativeAreaSnapshot> knownAreas,
        CancellationToken cancellationToken)
    {
        var missingCountries = countryIds.Where(id => !knownCountries.ContainsKey(id)).ToArray();
        if (missingCountries.Length > 0)
        {
            var read = await ReadCountriesAsync(missingCountries, cancellationToken);
            if (read is null)
            {
                return null;
            }

            foreach (var row in read.Values)
            {
                knownCountries[row.CountryId] = row;
            }
        }

        var pending = areaIds.Where(id => !knownAreas.ContainsKey(id)).ToArray();
        var requested = new HashSet<Guid>(pending);
        while (pending.Length > 0)
        {
            var read = await ReadAreasAsync(pending, cancellationToken);
            if (read is null)
            {
                return null;
            }

            var next = new List<Guid>();
            foreach (var row in read.Values)
            {
                knownAreas[row.AdministrativeAreaId] = row;
                if (row.ParentAdministrativeAreaId is { } parentId && requested.Add(parentId))
                {
                    next.Add(parentId);
                }
            }

            pending = next.ToArray();
        }

        return (knownCountries, knownAreas);
    }

    private async Task<Dictionary<Guid, CountrySnapshot>?> ReadCountriesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
        => await ReadVerifiedAsync(
            CountriesOperation,
            ids,
            () => snapshots.GetCountriesAsync(ids, cancellationToken),
            row => row.CountryId,
            cancellationToken);

    private async Task<Dictionary<Guid, AdministrativeAreaSnapshot>?> ReadAreasAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
        => await ReadVerifiedAsync(
            AdministrativeAreasOperation,
            ids,
            () => snapshots.GetAdministrativeAreasAsync(ids, cancellationToken),
            row => row.AdministrativeAreaId,
            cancellationToken);

    private async Task<Dictionary<Guid, CitySnapshot>?> ReadCitiesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
        => await ReadVerifiedAsync(
            CitiesOperation,
            ids,
            () => snapshots.GetCitiesAsync(ids, cancellationToken),
            row => row.CityId,
            cancellationToken);

    private async Task<Dictionary<Guid, ServiceAreaSnapshot>?> ReadServiceAreasAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
        => await ReadVerifiedAsync(
            ServiceAreasOperation,
            ids,
            () => snapshots.GetServiceAreasAsync(ids, cancellationToken),
            row => row.ServiceAreaId,
            cancellationToken);

    /// <summary>
    /// یک خواندن batch واقعی را با span فرزند canonical می‌پوشاند و پاسخ را از نظر هویت می‌سنجد:
    /// برچسب‌ها/شناسه‌های بیرون از مجموعهٔ درخواستی یا تکرار هویت، پاسخ متناقض است و fail-closed
    /// می‌شود. درخواست خالی هیچ فراخوانی و هیچ span نمی‌سازد. استثنا/لغو عیناً منتشر می‌شود.
    /// </summary>
    private async Task<Dictionary<Guid, T>?> ReadVerifiedAsync<T>(
        string operation,
        IReadOnlyCollection<Guid> ids,
        Func<Task<IReadOnlyList<T>>> call,
        Func<T, Guid> idOf,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await CallAsync(operation, call);
        var requested = new HashSet<Guid>(ids);
        var result = new Dictionary<Guid, T>(rows.Count);
        foreach (var row in rows)
        {
            var id = idOf(row);
            if (!requested.Contains(id) || !result.TryAdd(id, row))
            {
                return null;
            }
        }

        return result;
    }

    private async Task<Dictionary<(ShippingAuthoringAreaKind Kind, Guid Id), string?>?> ReadLabelsAsync(
        List<ShippingAuthoringSelectedAreaItem> keys,
        Guid languageId,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<(ShippingAuthoringAreaKind Kind, Guid Id), string?>(keys.Count);

        async Task AddAsync(
            ShippingAuthoringAreaKind kind,
            IReadOnlyCollection<Guid> ids,
            Func<IReadOnlyCollection<Guid>, Guid?, CancellationToken, Task<IReadOnlyDictionary<Guid, string>>> read)
        {
            if (ids.Count == 0)
            {
                return;
            }

            var labelsById = await CallAsync(LabelsOperation, () => read(ids, languageId, cancellationToken));
            foreach (var id in ids)
            {
                map[(kind, id)] = labelsById.TryGetValue(id, out var label) && !string.IsNullOrWhiteSpace(label)
                    ? label
                    : null;
            }
        }

        await AddAsync(ShippingAuthoringAreaKind.Country, IdsOf(keys, ShippingAuthoringAreaKind.Country), labels.GetCountryLabelsAsync);
        await AddAsync(
            ShippingAuthoringAreaKind.AdministrativeArea,
            IdsOf(keys, ShippingAuthoringAreaKind.AdministrativeArea),
            labels.GetAdministrativeAreaLabelsAsync);
        await AddAsync(ShippingAuthoringAreaKind.City, IdsOf(keys, ShippingAuthoringAreaKind.City), labels.GetCityLabelsAsync);
        await AddAsync(
            ShippingAuthoringAreaKind.ServiceArea,
            IdsOf(keys, ShippingAuthoringAreaKind.ServiceArea),
            labels.GetServiceAreaLabelsAsync);

        return map;
    }

    private static Result<IReadOnlyList<ShippingAuthoringDestinationFactItem>> Failure()
        => Result.Failure<IReadOnlyList<ShippingAuthoringDestinationFactItem>>(
            new SemanticError(ShippingErrorCodes.AuthoringConfigurationUnavailable));

    /// <summary>
    /// یک خواندن واقعی Geography را با span فرزند canonical می‌پوشاند. هیچ شناسهٔ مقصد/مالک/نام
    /// به‌عنوان tag نوشته نمی‌شود و استثنا/لغو عیناً منتشر می‌شود.
    /// </summary>
    private async Task<T> CallAsync<T>(string operation, Func<Task<T>> call)
    {
        using var moduleCall = moduleCallTracer.Begin(SourceModule, TargetModule, operation);
        try
        {
            var value = await call();
            moduleCall.SetOk();
            return value;
        }
        catch (Exception exception)
        {
            moduleCall.SetError(exception);
            throw;
        }
    }
}
