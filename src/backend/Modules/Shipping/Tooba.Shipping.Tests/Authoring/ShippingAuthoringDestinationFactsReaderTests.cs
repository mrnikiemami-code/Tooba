using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// اثبات فشردهٔ خواندن فکت‌های مقصد (TB-201) روی درزهای Contracts Geography با منبع آزمون
/// کنترل‌شده. این فایل هیچ DB، کانتینر یا HTTP راه نمی‌اندازد.
/// </summary>
public sealed class ShippingAuthoringDestinationFactsReaderTests
{
    private static readonly Guid Country = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid CountryInactive = Guid.Parse("11111111-1111-4111-8111-111111111112");
    private static readonly Guid RootArea = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid DeepArea = Guid.Parse("22222222-2222-4222-8222-222222222223");
    private static readonly Guid CityWithArea = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid CityNoArea = Guid.Parse("33333333-3333-4333-8333-333333333334");
    private static readonly Guid ServiceArea = Guid.Parse("44444444-4444-4444-8444-444444444444");
    private static readonly Guid OrphanServiceArea = Guid.Parse("44444444-4444-4444-8444-444444444445");
    private static readonly Guid CyclicArea = Guid.Parse("22222222-2222-4222-8222-222222222224");
    private static readonly Guid Language = Guid.Parse("55555555-5555-4555-8555-555555555555");

    [Fact]
    public async Task Resolves_every_kind_with_authoritative_parents_and_keeps_same_guid_in_two_kinds_apart()
    {
        var reader = new FakeSnapshots
        {
            Countries =
            {
                [Country] = new(Country, "IR", true),
            },
            Areas =
            {
                [RootArea] = new(RootArea, Country, null, "ROOT", true),
                [DeepArea] = new(DeepArea, Country, RootArea, "DEEP", true),
            },
            Cities =
            {
                [CityWithArea] = new(CityWithArea, Country, DeepArea, "CITY-A", true),
                [CityNoArea] = new(CityNoArea, Country, null, "CITY-B", true),
            },
            ServiceAreas =
            {
                [ServiceArea] = new(ServiceArea, CityWithArea, "SA", true),
            },
        };
        var labels = new FakeLabels { [Country] = "ایران", [DeepArea] = "منطقه", [CityWithArea] = "شهر" };
        var facts = await new ShippingAuthoringDestinationFactsReader(reader, labels, new ModuleCallTracer()).ReadAsync(
            [
                new(ShippingAuthoringAreaKind.Country, Country),
                new(ShippingAuthoringAreaKind.AdministrativeArea, DeepArea),
                new(ShippingAuthoringAreaKind.City, CityWithArea),
                new(ShippingAuthoringAreaKind.ServiceArea, ServiceArea),
                // Same GUID as the country key but a different KIND: a distinct identity, not a duplicate.
                new(ShippingAuthoringAreaKind.City, Country),
            ],
            Language,
            CancellationToken.None);

        Assert.True(facts.IsSuccess);
        var items = facts.Value;
        Assert.Equal(5, items.Count);
        Assert.All(items.Take(4), item => Assert.Equal(ShippingAuthoringDestinationResolution.Resolved, item.Resolution));

        Assert.Equal(new ShippingAuthoringDestinationWriteShape(ShippingAuthoringAreaKind.Country, Country, null, null, null), items[0].Area);
        Assert.Equal(new ShippingAuthoringDestinationWriteShape(ShippingAuthoringAreaKind.AdministrativeArea, Country, DeepArea, null, null), items[1].Area);
        Assert.Equal(new ShippingAuthoringDestinationWriteShape(ShippingAuthoringAreaKind.City, Country, DeepArea, CityWithArea, null), items[2].Area);
        Assert.Equal(new ShippingAuthoringDestinationWriteShape(ShippingAuthoringAreaKind.ServiceArea, Country, DeepArea, CityWithArea, ServiceArea), items[3].Area);
        Assert.Equal("ایران", items[0].Label);
        Assert.Equal("منطقه", items[1].Label);
        Assert.Equal("شهر", items[2].Label);
        Assert.Null(items[3].Label);
        // The unresolved same-GUID city is a separate entry, proven absent rather than merged.
        Assert.Equal(ShippingAuthoringAreaKind.City, items[4].Kind);
        Assert.Equal(ShippingAuthoringDestinationResolution.Unresolved, items[4].Resolution);
        Assert.Null(items[4].Area);
        Assert.All(labels.RequestedLanguages, id => Assert.Equal(Language, id));
    }

    [Fact]
    public async Task City_without_administrative_parent_resolves_without_a_fabricated_parent()
    {
        var reader = new FakeSnapshots
        {
            Countries = { [Country] = new(Country, "IR", true) },
            Cities = { [CityNoArea] = new(CityNoArea, Country, null, "CITY-B", true) },
        };
        var facts = await new ShippingAuthoringDestinationFactsReader(reader, new FakeLabels(), new ModuleCallTracer())
            .ReadAsync([new(ShippingAuthoringAreaKind.City, CityNoArea)], Language, CancellationToken.None);

        Assert.True(facts.IsSuccess);
        var area = Assert.Single(facts.Value).Area;
        Assert.NotNull(area);
        Assert.Null(area!.AdministrativeAreaId);
        Assert.Equal(CityNoArea, area.CityId);
        Assert.Equal(Country, area.CountryId);
        // Only the city itself and its country are read: no world scan of areas.
        Assert.DoesNotContain(reader.AreaReads.SelectMany(x => x), _ => true);
    }

    [Fact]
    public async Task Missing_inactive_contradictory_and_cyclic_chains_are_unresolved_with_the_raw_key_kept()
    {
        var reader = new FakeSnapshots
        {
            Countries = { [Country] = new(Country, "IR", true), [CountryInactive] = new(CountryInactive, "XX", false) },
            Areas =
            {
                // A cycle: A -> B -> A.
                [CyclicArea] = new(CyclicArea, Country, DeepArea, "CYC-A", true),
                [DeepArea] = new(DeepArea, Country, CyclicArea, "CYC-B", true),
                // An inactive admin parent.
                [RootArea] = new(RootArea, Country, null, "ROOT", false),
            },
            Cities =
            {
                // Country disagrees with the city's admin area.
                [CityWithArea] = new(CityWithArea, Country, Guid.Parse("22222222-2222-4222-8222-2222222222ff"), "BAD", true),
                [CityNoArea] = new(CityNoArea, Country, RootArea, "INACTIVE-PARENT", true),
            },
            ServiceAreas = { [OrphanServiceArea] = new(OrphanServiceArea, Guid.Parse("33333333-3333-4333-8333-3333333333ff"), "GHOST", true) },
        };
        var keys = new List<ShippingAuthoringSelectedAreaItem>
        {
            new(ShippingAuthoringAreaKind.City, Guid.Parse("33333333-3333-4333-8333-333333333300")), // absent
            new(ShippingAuthoringAreaKind.Country, CountryInactive),                                 // inactive
            new(ShippingAuthoringAreaKind.City, CityWithArea),                                      // contradictory parent
            new(ShippingAuthoringAreaKind.AdministrativeArea, CyclicArea),                          // cycle
            new(ShippingAuthoringAreaKind.City, CityNoArea),                                        // inactive parent
            new(ShippingAuthoringAreaKind.ServiceArea, OrphanServiceArea),                          // ghost owning city
        };
        var facts = await new ShippingAuthoringDestinationFactsReader(reader, new FakeLabels(), new ModuleCallTracer())
            .ReadAsync(keys, Language, CancellationToken.None);

        Assert.True(facts.IsSuccess);
        Assert.Equal(keys.Count, facts.Value.Count);
        foreach (var item in facts.Value)
        {
            Assert.Equal(ShippingAuthoringDestinationResolution.Unresolved, item.Resolution);
            Assert.Null(item.Area);
        }

        Assert.Equal(keys.Select(key => (key.Kind, key.Id)), facts.Value.Select(fact => (fact.Kind, fact.Id)));
    }

    [Fact]
    public async Task Empty_or_blank_selection_performs_zero_geography_reads()
    {
        var reader = new FakeSnapshots();
        var labels = new FakeLabels();
        var facts = await new ShippingAuthoringDestinationFactsReader(reader, labels, new ModuleCallTracer())
            .ReadAsync([], Language, CancellationToken.None);

        Assert.True(facts.IsSuccess);
        Assert.Empty(facts.Value);
        Assert.Equal(0, reader.TotalCalls);
        Assert.Equal(0, labels.TotalCalls);

        var blank = await new ShippingAuthoringDestinationFactsReader(reader, labels, new ModuleCallTracer())
            .ReadAsync([new(ShippingAuthoringAreaKind.Country, Guid.Empty)], Language, CancellationToken.None);
        Assert.True(blank.IsSuccess);
        Assert.Empty(blank.Value);
        Assert.Equal(0, reader.TotalCalls);
    }

    [Fact]
    public async Task A_contract_response_with_a_foreign_identity_fails_closed_instead_of_being_trusted()
    {
        var reader = new FakeSnapshots { ReturnAllCountries = true };
        reader.Countries[Guid.Parse("99999999-9999-4999-8999-999999999999")] = new(Guid.Parse("99999999-9999-4999-8999-999999999999"), "ZZ", true);
        var facts = await new ShippingAuthoringDestinationFactsReader(reader, new FakeLabels(), new ModuleCallTracer())
            .ReadAsync([new(ShippingAuthoringAreaKind.Country, Country)], Language, CancellationToken.None);

        Assert.True(facts.IsFailure);
        Assert.NotEmpty(facts.Errors);
    }

    [Fact]
    public async Task Dependency_exception_and_cancellation_propagate_unchanged()
    {
        var failure = new InvalidOperationException("provider-down");
        var reader = new FakeSnapshots { ThrowOnCountryRead = failure };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ShippingAuthoringDestinationFactsReader(reader, new FakeLabels(), new ModuleCallTracer())
                .ReadAsync([new(ShippingAuthoringAreaKind.Country, Country)], Language, CancellationToken.None));

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancelled = new FakeSnapshots { ThrowOnCountryRead = new OperationCanceledException(cts.Token) };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ShippingAuthoringDestinationFactsReader(cancelled, new FakeLabels(), new ModuleCallTracer())
                .ReadAsync([new(ShippingAuthoringAreaKind.Country, Country)], Language, CancellationToken.None));
    }

    private sealed class FakeSnapshots : IGeographyPlaceSnapshotReader
    {
        public Dictionary<Guid, CountrySnapshot> Countries { get; } = [];
        public Dictionary<Guid, AdministrativeAreaSnapshot> Areas { get; } = [];
        public Dictionary<Guid, CitySnapshot> Cities { get; } = [];
        public Dictionary<Guid, ServiceAreaSnapshot> ServiceAreas { get; } = [];

        public List<IReadOnlyCollection<Guid>> AreaReads { get; } = [];
        public int TotalCalls { get; private set; }
        public Exception? ThrowOnCountryRead { get; init; }
        public bool ReturnAllCountries { get; init; }

        public Task<IReadOnlyList<CountrySnapshot>> GetCountriesAsync(
            IReadOnlyCollection<Guid> countryIds,
            CancellationToken cancellationToken)
        {
            TotalCalls++;
            if (ThrowOnCountryRead is not null)
            {
                throw ThrowOnCountryRead;
            }

            return Task.FromResult<IReadOnlyList<CountrySnapshot>>(
                ReturnAllCountries
                    ? Countries.Values.ToArray()
                    : countryIds.Where(Countries.ContainsKey).Select(id => Countries[id]).ToArray());
        }

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> GetAdministrativeAreasAsync(
            IReadOnlyCollection<Guid> administrativeAreaIds,
            CancellationToken cancellationToken)
        {
            TotalCalls++;
            AreaReads.Add(administrativeAreaIds);
            return Task.FromResult<IReadOnlyList<AdministrativeAreaSnapshot>>(
                administrativeAreaIds.Where(Areas.ContainsKey).Select(id => Areas[id]).ToArray());
        }

        public Task<IReadOnlyList<CitySnapshot>> GetCitiesAsync(
            IReadOnlyCollection<Guid> cityIds,
            CancellationToken cancellationToken)
        {
            TotalCalls++;
            return Task.FromResult<IReadOnlyList<CitySnapshot>>(
                cityIds.Where(Cities.ContainsKey).Select(id => Cities[id]).ToArray());
        }

        public Task<IReadOnlyList<ServiceAreaSnapshot>> GetServiceAreasAsync(
            IReadOnlyCollection<Guid> serviceAreaIds,
            CancellationToken cancellationToken)
        {
            TotalCalls++;
            return Task.FromResult<IReadOnlyList<ServiceAreaSnapshot>>(
                serviceAreaIds.Where(ServiceAreas.ContainsKey).Select(id => ServiceAreas[id]).ToArray());
        }
    }

    private sealed class FakeLabels : IGeographyLabelLookup
    {
        private readonly Dictionary<Guid, string> _labels = [];
        public List<Guid?> RequestedLanguages { get; } = [];
        public int TotalCalls { get; private set; }

        public string this[Guid id]
        {
            set => _labels[id] = value;
        }

        public Task<IReadOnlyDictionary<Guid, string>> GetCountryLabelsAsync(
            IReadOnlyCollection<Guid> countryIds,
            Guid? languageId,
            CancellationToken cancellationToken) => Read(countryIds, languageId);

        public Task<IReadOnlyDictionary<Guid, string>> GetAdministrativeAreaLabelsAsync(
            IReadOnlyCollection<Guid> administrativeAreaIds,
            Guid? languageId,
            CancellationToken cancellationToken) => Read(administrativeAreaIds, languageId);

        public Task<IReadOnlyDictionary<Guid, string>> GetCityLabelsAsync(
            IReadOnlyCollection<Guid> cityIds,
            Guid? languageId,
            CancellationToken cancellationToken) => Read(cityIds, languageId);

        public Task<IReadOnlyDictionary<Guid, string>> GetServiceAreaLabelsAsync(
            IReadOnlyCollection<Guid> serviceAreaIds,
            Guid? languageId,
            CancellationToken cancellationToken) => Read(serviceAreaIds, languageId);

        private Task<IReadOnlyDictionary<Guid, string>> Read(IReadOnlyCollection<Guid> ids, Guid? languageId)
        {
            TotalCalls++;
            RequestedLanguages.Add(languageId);
            IReadOnlyDictionary<Guid, string> found = ids
                .Where(_labels.ContainsKey)
                .ToDictionary(id => id, id => _labels[id]);
            return Task.FromResult(found);
        }
    }
}
