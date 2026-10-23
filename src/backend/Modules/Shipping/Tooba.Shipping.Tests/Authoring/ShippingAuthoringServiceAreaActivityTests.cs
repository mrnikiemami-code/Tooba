using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-201-R2: executable form of the saved reproduction, with a <b>lawful</b> fixture.
/// </summary>
/// <remarks>
/// <para>
/// The saved R1 reproduction was red because its fake returned the SAME city twice: two requested service
/// areas shared one city, the reader legitimately asks the bulk seam for <c>[city, city]</c>, and a fake
/// that echoes every requested id answers with two identical rows. <c>ReadVerifiedAsync</c> correctly
/// fails closed on that duplicated response identity. The real <c>GeographyPlaceSnapshotReader</c>
/// de-duplicates requested ids before querying, so a lawful fixture must also answer with unique ids.
/// </para>
/// <para>
/// These facts therefore pin the real contract: terminal activity is honoured, a terminal-only request
/// resolves its whole ancestry, and a duplicated foreign response is still refused.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringServiceAreaActivityTests
{
    private static readonly Guid Country = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid RootArea = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid DeepArea = Guid.Parse("22222222-2222-4222-8222-222222222223");
    private static readonly Guid City = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid InactiveServiceArea = Guid.Parse("44444444-4444-4444-8444-444444444446");
    private static readonly Guid ActiveServiceArea = Guid.Parse("44444444-4444-4444-8444-444444444447");
    private static readonly Guid Language = Guid.Parse("55555555-5555-4555-8555-555555555555");

    [Fact]
    public async Task Inactive_service_area_is_unresolved_while_an_otherwise_identical_active_one_resolves()
    {
        // Both terminals share ONE city; the lawful fixture answers with unique ids per call.
        var reader = LawfulGeography.Seeded();

        var facts = await new ShippingAuthoringDestinationFactsReader(reader, new NoLabels(), new ModuleCallTracer())
            .ReadAsync(
                [
                    new(ShippingAuthoringAreaKind.ServiceArea, InactiveServiceArea),
                    new(ShippingAuthoringAreaKind.ServiceArea, ActiveServiceArea),
                ],
                Language,
                CancellationToken.None);

        AssertSucceeded(facts);
        Assert.Equal(2, facts.Value.Count);

        var inactive = facts.Value[0];
        Assert.Equal(ShippingAuthoringAreaKind.ServiceArea, inactive.Kind);
        Assert.Equal(InactiveServiceArea, inactive.Id);
        Assert.Equal(ShippingAuthoringDestinationResolution.Unresolved, inactive.Resolution);
        Assert.Null(inactive.Area);

        var active = facts.Value[1];
        Assert.Equal(ShippingAuthoringAreaKind.ServiceArea, active.Kind);
        Assert.Equal(ActiveServiceArea, active.Id);
        Assert.Equal(ShippingAuthoringDestinationResolution.Resolved, active.Resolution);
        Assert.Equal(
            new ShippingAuthoringDestinationWriteShape(
                ShippingAuthoringAreaKind.ServiceArea,
                Country,
                DeepArea,
                City,
                ActiveServiceArea),
            active.Area);

        // Identity of the requested keys is preserved for both entries.
        Assert.Equal(
            new[] { InactiveServiceArea, ActiveServiceArea },
            facts.Value.Select(fact => fact.Id).ToArray());
    }

    [Fact]
    public async Task A_single_active_service_area_is_the_only_requested_key_and_still_discovers_its_whole_ancestry()
    {
        // No country/city/admin key is supplied: the reader's own parent closure must find every ancestor.
        var reader = LawfulGeography.Seeded();

        var facts = await new ShippingAuthoringDestinationFactsReader(reader, new NoLabels(), new ModuleCallTracer())
            .ReadAsync(
                [new(ShippingAuthoringAreaKind.ServiceArea, ActiveServiceArea)],
                Language,
                CancellationToken.None);

        AssertSucceeded(facts);
        var fact = Assert.Single(facts.Value);
        Assert.Equal(ShippingAuthoringDestinationResolution.Resolved, fact.Resolution);
        Assert.Equal(
            new ShippingAuthoringDestinationWriteShape(
                ShippingAuthoringAreaKind.ServiceArea,
                Country,
                DeepArea,
                City,
                ActiveServiceArea),
            fact.Area);
        Assert.Equal(ActiveServiceArea, fact.Id);
    }

    [Fact]
    public async Task A_duplicated_foreign_response_identity_is_still_refused()
    {
        // Deliberate corruption: the parent city is answered twice for the same request.
        var reader = LawfulGeography.Seeded(duplicateCityResponse: true);

        var facts = await new ShippingAuthoringDestinationFactsReader(reader, new NoLabels(), new ModuleCallTracer())
            .ReadAsync(
                [new(ShippingAuthoringAreaKind.ServiceArea, ActiveServiceArea)],
                Language,
                CancellationToken.None);

        Assert.True(facts.IsFailure);
        Assert.Contains(
            facts.Errors,
            error => error.Code == Tooba.Shipping.Contracts.Errors.ShippingErrorCodes.AuthoringConfigurationUnavailable);
    }

    [Fact]
    public async Task Duplicate_requested_keys_produce_one_unique_fact()
    {
        var reader = LawfulGeography.Seeded();

        var facts = await new ShippingAuthoringDestinationFactsReader(reader, new NoLabels(), new ModuleCallTracer())
            .ReadAsync(
                [
                    new(ShippingAuthoringAreaKind.Country, Country),
                    new(ShippingAuthoringAreaKind.Country, Country),
                ],
                Language,
                CancellationToken.None);

        AssertSucceeded(facts);
        var fact = Assert.Single(facts.Value);
        Assert.Equal(ShippingAuthoringDestinationResolution.Resolved, fact.Resolution);
    }

    private static void AssertSucceeded(
        Tooba.BuildingBlocks.Results.Result<IReadOnlyList<ShippingAuthoringDestinationFactItem>> result)
        => Assert.True(
            result.IsSuccess,
            result.IsSuccess ? null : string.Join(",", result.Errors.Select(error => error.Code)));

    /// <summary>
    /// Fixture whose output obeys the same uniqueness rule the real Geography reader guarantees.
    /// </summary>
    private sealed class LawfulGeography : IGeographyPlaceSnapshotReader
    {
        public Dictionary<Guid, CountrySnapshot> Countries { get; } = [];
        public Dictionary<Guid, AdministrativeAreaSnapshot> Areas { get; } = [];
        public Dictionary<Guid, CitySnapshot> Cities { get; } = [];
        public Dictionary<Guid, ServiceAreaSnapshot> ServiceAreas { get; } = [];

        public bool DuplicateCityResponse { get; init; }

        public static LawfulGeography Seeded(bool duplicateCityResponse = false)
        {
            var fixture = new LawfulGeography { DuplicateCityResponse = duplicateCityResponse };
            fixture.Countries[Country] = new(Country, "IR", true);
            fixture.Areas[RootArea] = new(RootArea, Country, null, "ROOT", true);
            fixture.Areas[DeepArea] = new(DeepArea, Country, RootArea, "DEEP", true);
            fixture.Cities[City] = new(City, Country, DeepArea, "CITY", true);
            fixture.ServiceAreas[InactiveServiceArea] = new(InactiveServiceArea, City, "SA-OFF", false);
            fixture.ServiceAreas[ActiveServiceArea] = new(ActiveServiceArea, City, "SA-ON", true);
            return fixture;
        }

        public Task<IReadOnlyList<CountrySnapshot>> GetCountriesAsync(
            IReadOnlyCollection<Guid> countryIds,
            CancellationToken cancellationToken)
            => Answer(countryIds, Countries, row => row.CountryId);

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> GetAdministrativeAreasAsync(
            IReadOnlyCollection<Guid> administrativeAreaIds,
            CancellationToken cancellationToken)
            => Answer(administrativeAreaIds, Areas, row => row.AdministrativeAreaId);

        public Task<IReadOnlyList<CitySnapshot>> GetCitiesAsync(
            IReadOnlyCollection<Guid> cityIds,
            CancellationToken cancellationToken)
        {
            var rows = cityIds.Distinct().Where(Cities.ContainsKey).Select(id => Cities[id]).ToArray();
            // The corruption under test: a foreign response that repeats one identity.
            var answered = DuplicateCityResponse && rows.Length > 0 ? rows.Concat(rows).ToArray() : rows;
            return Task.FromResult<IReadOnlyList<CitySnapshot>>(answered);
        }

        public Task<IReadOnlyList<ServiceAreaSnapshot>> GetServiceAreasAsync(
            IReadOnlyCollection<Guid> serviceAreaIds,
            CancellationToken cancellationToken)
            => Answer(serviceAreaIds, ServiceAreas, row => row.ServiceAreaId);

        private static Task<IReadOnlyList<T>> Answer<T>(
            IReadOnlyCollection<Guid> ids,
            Dictionary<Guid, T> rows,
            Func<T, Guid> idOf)
            => Task.FromResult<IReadOnlyList<T>>(
                ids.Distinct().Where(rows.ContainsKey).Select(id => rows[id]).ToArray());
    }

    private sealed class NoLabels : IGeographyLabelLookup
    {
        public Task<IReadOnlyDictionary<Guid, string>> GetCountryLabelsAsync(
            IReadOnlyCollection<Guid> countryIds,
            Guid? languageId,
            CancellationToken cancellationToken) => Empty();

        public Task<IReadOnlyDictionary<Guid, string>> GetAdministrativeAreaLabelsAsync(
            IReadOnlyCollection<Guid> administrativeAreaIds,
            Guid? languageId,
            CancellationToken cancellationToken) => Empty();

        public Task<IReadOnlyDictionary<Guid, string>> GetCityLabelsAsync(
            IReadOnlyCollection<Guid> cityIds,
            Guid? languageId,
            CancellationToken cancellationToken) => Empty();

        public Task<IReadOnlyDictionary<Guid, string>> GetServiceAreaLabelsAsync(
            IReadOnlyCollection<Guid> serviceAreaIds,
            Guid? languageId,
            CancellationToken cancellationToken) => Empty();

        private static Task<IReadOnlyDictionary<Guid, string>> Empty()
            => Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
    }
}
