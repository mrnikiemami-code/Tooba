using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Infrastructure.Authoring;

/// <summary>
/// اثبات جغرافیای انتخاب‌های یک پیش‌نویس روی درز فقط-خواندنی Geography Contracts. هیچ DbContext،
/// FK یا join میان‌طرح‌واره‌ای اینجا نیست.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ancestry proof, not GUID equality.</b> ناحیهٔ اداری می‌تواند در هر عمقی والد داشته باشد،
/// بنابراین زنجیرهٔ واقعی از فهرست مرجع همان کشور پیموده می‌شود؛ والد غایب، غیرفعال، متعلق به
/// کشور دیگر یا چرخه یک زنجیرهٔ معتبر نیست. شناسهٔ والدِ اعلام‌شده توسط مالک فقط ادعاست و با
/// رابطهٔ واقعی شهر↔ناحیه و ناحیهٔ خدمت↔شهر سنجیده می‌شود.
/// </para>
/// <para>
/// <b>Bounded per operation.</b> مراجع تکراری فقط داخل همین عملیات cache می‌شوند؛ هیچ cache
/// سراسری، TTL یا منبع دوّم حقیقت ساخته نمی‌شود.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringDestinationResolver(
    IGeographyLookup geography,
    IModuleCallTracer moduleCallTracer) : IShippingAuthoringDestinationResolver
{
    private const string SourceModule = "Shipping";
    private const string TargetModule = "Geography";
    private const string CountriesOperation = "ListCountriesForAuthoringDraft";
    private const string AdministrativeAreasOperation = "ListAdministrativeAreasForAuthoringDraft";
    private const string CitiesOperation = "ListCitiesForAuthoringDraft";
    private const string ServiceAreasOperation = "ListServiceAreasForAuthoringDraft";

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ShippingDestinationSelectionPath>>> ResolveAsync(
        IReadOnlyList<ShippingAuthoringSelectedAreaWrite> selections,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selections);
        if (selections.Count == 0)
        {
            return Result.Success<IReadOnlyList<ShippingDestinationSelectionPath>>([]);
        }

        var countries = await CallAsync(
            CountriesOperation,
            () => geography.ListCountriesAsync(cancellationToken));
        var administrativeAreas = new Dictionary<Guid, IReadOnlyList<AdministrativeAreaSnapshot>>();
        var cities = new Dictionary<Guid, IReadOnlyList<CitySnapshot>>();
        var serviceAreas = new Dictionary<Guid, IReadOnlyList<ServiceAreaSnapshot>>();

        var paths = new List<ShippingDestinationSelectionPath>(selections.Count);
        foreach (var selection in selections)
        {
            if (selection is null)
            {
                return Failure();
            }

            var country = countries.FirstOrDefault(candidate => candidate.CountryId == selection.CountryId);
            if (country is null || !country.IsActive)
            {
                return Failure();
            }

            var chain = new List<ShippingDestinationNode>
            {
                new(ShippingDestinationNodeKind.Country, country.CountryId),
            };

            if (selection.Kind == ShippingAuthoringAreaKind.Country)
            {
                if (selection.AdministrativeAreaId is not null
                    || selection.CityId is not null
                    || selection.ServiceAreaId is not null)
                {
                    return Failure();
                }

                paths.Add(ShippingDestinationSelectionPath.FromChain(chain));
                continue;
            }

            if (selection.Kind == ShippingAuthoringAreaKind.AdministrativeArea)
            {
                if (selection.AdministrativeAreaId is not { } selectedArea
                    || selection.CityId is not null
                    || selection.ServiceAreaId is not null)
                {
                    return Failure();
                }

                var areaChain = await BuildAdministrativeChainAsync(country.CountryId, administrativeAreas, cancellationToken);
                if (!TryAppendAreaChain(areaChain, country.CountryId, selectedArea, chain))
                {
                    return Failure();
                }

                paths.Add(ShippingDestinationSelectionPath.FromChain(chain));
                continue;
            }

            if (selection.CityId is not { } cityId || cityId == Guid.Empty)
            {
                return Failure();
            }

            var countryCities = cities.TryGetValue(country.CountryId, out var cachedCities)
                ? cachedCities
                : cities[country.CountryId] = await CallAsync(
                    CitiesOperation,
                    () => geography.ListCitiesAsync(country.CountryId, null, cancellationToken));
            var city = countryCities.FirstOrDefault(candidate => candidate.CityId == cityId);
            if (city is null || !city.IsActive)
            {
                return Failure();
            }

            // The owner-declared administrative parent must agree with the authoritative city fact,
            // including the case where the city has no administrative parent at all.
            if (selection.AdministrativeAreaId is { } declaredArea)
            {
                if (city.AdministrativeAreaId != declaredArea)
                {
                    return Failure();
                }
            }

            if (city.AdministrativeAreaId is { } areaId)
            {
                var areaChain = await BuildAdministrativeChainAsync(country.CountryId, administrativeAreas, cancellationToken);
                if (!TryAppendAreaChain(areaChain, country.CountryId, areaId, chain))
                {
                    return Failure();
                }
            }

            chain.Add(new(ShippingDestinationNodeKind.City, city.CityId));

            if (selection.Kind == ShippingAuthoringAreaKind.City)
            {
                if (selection.ServiceAreaId is not null)
                {
                    return Failure();
                }

                paths.Add(ShippingDestinationSelectionPath.FromChain(chain));
                continue;
            }

            if (selection.Kind != ShippingAuthoringAreaKind.ServiceArea
                || selection.ServiceAreaId is not { } serviceAreaId
                || serviceAreaId == Guid.Empty)
            {
                return Failure();
            }

            var cityServiceAreas = serviceAreas.TryGetValue(city.CityId, out var cachedServiceAreas)
                ? cachedServiceAreas
                : serviceAreas[city.CityId] = await CallAsync(
                    ServiceAreasOperation,
                    () => geography.ListServiceAreasAsync(city.CityId, cancellationToken));
            var serviceArea = cityServiceAreas.FirstOrDefault(candidate => candidate.ServiceAreaId == serviceAreaId);
            if (serviceArea is null || !serviceArea.IsActive)
            {
                return Failure();
            }

            chain.Add(new(ShippingDestinationNodeKind.ServiceArea, serviceArea.ServiceAreaId));
            paths.Add(ShippingDestinationSelectionPath.FromChain(chain));
        }

        return Result.Success<IReadOnlyList<ShippingDestinationSelectionPath>>(paths);
    }

    private async Task<IReadOnlyList<AdministrativeAreaSnapshot>> BuildAdministrativeChainAsync(
        Guid countryId,
        Dictionary<Guid, IReadOnlyList<AdministrativeAreaSnapshot>> cache,
        CancellationToken cancellationToken)
        => cache.TryGetValue(countryId, out var cached)
            ? cached
            : cache[countryId] = await CallAsync(
                AdministrativeAreasOperation,
                () => geography.ListAdministrativeAreasAsync(countryId, cancellationToken));

    /// <summary>
    /// زنجیرهٔ واقعی یک ناحیهٔ اداری (ریشه تا خود ناحیه) را به زنجیره اضافه می‌کند. قاعدهٔ canonical
    /// در <see cref="ShippingDestinationAncestry"/> نگه داشته می‌شود تا خواندن و نوشتن یک حکم داشته باشند.
    /// </summary>
    private static bool TryAppendAreaChain(
        IReadOnlyList<AdministrativeAreaSnapshot> areas,
        Guid countryId,
        Guid areaId,
        List<ShippingDestinationNode> chain)
    {
        // The canonical chain rule lives in one helper so the read projection and the write validation
        // cannot drift apart.
        if (!ShippingDestinationAncestry.TryBuildAdministrativeChain(
                areas.ToDictionary(area => area.AdministrativeAreaId),
                areaId,
                countryId,
                chain))
        {
            return false;
        }

        return true;
    }

    private static Result<IReadOnlyList<ShippingDestinationSelectionPath>> Failure()
        => Result.Failure<IReadOnlyList<ShippingDestinationSelectionPath>>(
            new SemanticError(ShippingErrorCodes.DestinationScopeInvalid));

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
