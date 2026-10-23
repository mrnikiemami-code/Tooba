using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Geography.Contracts.Places;
using Tooba.Geography.Domain.Aggregates;
using Tooba.Geography.Infrastructure.DependencyInjection;
using Tooba.Geography.Infrastructure.Persistence;
using Tooba.Geography.Infrastructure.Places;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.DependencyInjection;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-201-R2 — the real composition roots and the real Geography EF reader, not a hand-copied adapter.
/// </summary>
/// <remarks>
/// <para>
/// Both modules are composed by their OWN <c>AddServices</c>. Only the provider/data boundary is
/// replaced (EFInMemory options for the two module contexts) and a controlled label data source is
/// substituted where a label fact is needed; neither new adapter is hand-registered.
/// </para>
/// <para>
/// This proves the modules' own EF query behaviour and lifetimes with controlled in-memory data. It is
/// NOT a PostgreSQL SQL/schema parity proof, and no external provider, migrator or hosted service starts.
/// </para>
/// </remarks>
[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingAuthoringDestinationReadCompositionTests
{
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid CountryInactiveId = Guid.Parse("11111111-1111-4111-8111-111111111112");
    private static readonly Guid UnrelatedCountryId = Guid.Parse("11111111-1111-4111-8111-111111111113");
    private static readonly Guid RootAreaId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid DeepAreaId = Guid.Parse("22222222-2222-4222-8222-222222222223");
    private static readonly Guid CityWithAreaId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid CityNoAreaId = Guid.Parse("33333333-3333-4333-8333-333333333334");
    private static readonly Guid ServiceAreaId = Guid.Parse("44444444-4444-4444-8444-444444444444");
    private static readonly Guid LanguageId = Guid.Parse("55555555-5555-4555-8555-555555555555");

    [Fact]
    public void The_real_modules_register_one_scoped_adapter_each_and_the_canonical_tracer_once()
    {
        var services = new ServiceCollection();
        Compose(services);

        var snapshotDescriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IGeographyPlaceSnapshotReader))
            .ToArray();
        var snapshotDescriptor = Assert.Single(snapshotDescriptors);
        Assert.Equal(ServiceLifetime.Scoped, snapshotDescriptor.Lifetime);
        Assert.Equal(typeof(GeographyPlaceSnapshotReader), snapshotDescriptor.ImplementationType);

        var factsDescriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IShippingAuthoringDestinationFactsReader))
            .ToArray();
        var factsDescriptor = Assert.Single(factsDescriptors);
        Assert.Equal(ServiceLifetime.Scoped, factsDescriptor.Lifetime);
        Assert.Equal(typeof(ShippingAuthoringDestinationFactsReader), factsDescriptor.ImplementationType);

        Assert.Single(services.Where(candidate => candidate.ServiceType == typeof(IModuleCallTracer)));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var a = first.ServiceProvider.GetRequiredService<IGeographyPlaceSnapshotReader>();
        var b = first.ServiceProvider.GetRequiredService<IGeographyPlaceSnapshotReader>();
        var c = second.ServiceProvider.GetRequiredService<IGeographyPlaceSnapshotReader>();
        Assert.Same(a, b);
        Assert.NotSame(a, c);

        var factsA = first.ServiceProvider.GetRequiredService<IShippingAuthoringDestinationFactsReader>();
        var factsB = first.ServiceProvider.GetRequiredService<IShippingAuthoringDestinationFactsReader>();
        var factsC = second.ServiceProvider.GetRequiredService<IShippingAuthoringDestinationFactsReader>();
        Assert.IsType<ShippingAuthoringDestinationFactsReader>(factsA);
        Assert.Same(factsA, factsB);
        Assert.NotSame(factsA, factsC);
    }

    [Fact]
    public async Task The_real_Geography_reader_answers_all_four_bulk_axes_truthfully()
    {
        var database = $"geography-read-{Guid.NewGuid():N}";
        using var provider = BuildProvider(database);
        using var scope = provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IGeographyPlaceSnapshotReader>();
        var db = scope.ServiceProvider.GetRequiredService<GeographyDbContext>();

        await SeedAsync(db);

        // Requested ids are filtered, duplicates collapse, and unrelated rows never leak in.
        var countries = await reader.GetCountriesAsync(
            [CountryId, CountryId, CountryInactiveId],
            CancellationToken.None);
        Assert.Equal(
            new[] { CountryInactiveId, CountryId }.OrderBy(id => id).ToArray(),
            countries.Select(row => row.CountryId).OrderBy(id => id).ToArray());
        Assert.DoesNotContain(countries, row => row.CountryId == UnrelatedCountryId);
        // An inactive fact is returned truthfully rather than hidden.
        Assert.False(countries.Single(row => row.CountryId == CountryInactiveId).IsActive);

        // A genuinely missing id is an absence, not an error and not a substitute row.
        var missing = await reader.GetCountriesAsync([Guid.Parse("11111111-1111-4111-8111-1111111111ff")], CancellationToken.None);
        Assert.Empty(missing);

        var areas = await reader.GetAdministrativeAreasAsync([RootAreaId, DeepAreaId], CancellationToken.None);
        Assert.Equal(2, areas.Count);
        Assert.Null(areas.Single(row => row.AdministrativeAreaId == RootAreaId).ParentAdministrativeAreaId);
        Assert.Equal(RootAreaId, areas.Single(row => row.AdministrativeAreaId == DeepAreaId).ParentAdministrativeAreaId);
        Assert.Equal(CountryId, areas.Single(row => row.AdministrativeAreaId == DeepAreaId).CountryId);

        // A city without an administrative parent stays lawful with a null parent.
        var cities = await reader.GetCitiesAsync([CityWithAreaId, CityNoAreaId], CancellationToken.None);
        Assert.Equal(2, cities.Count);
        Assert.Null(cities.Single(row => row.CityId == CityNoAreaId).AdministrativeAreaId);
        Assert.Equal(DeepAreaId, cities.Single(row => row.CityId == CityWithAreaId).AdministrativeAreaId);

        var serviceAreas = await reader.GetServiceAreasAsync([ServiceAreaId], CancellationToken.None);
        var serviceArea = Assert.Single(serviceAreas);
        Assert.Equal(CityWithAreaId, serviceArea.CityId);
        Assert.True(serviceArea.IsActive);

        // No query tracking survives: the reader never mutates or attaches the seeded rows.
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task An_empty_request_does_no_provider_work_even_with_an_unusable_context()
    {
        var db = new GeographyDbContext(new DbContextOptionsBuilder<GeographyDbContext>()
            .UseInMemoryDatabase($"geography-empty-{Guid.NewGuid():N}")
            .Options);
        await db.DisposeAsync();

        var reader = new GeographyPlaceSnapshotReader(db);
        Assert.Empty(await reader.GetCountriesAsync([], CancellationToken.None));
        Assert.Empty(await reader.GetAdministrativeAreasAsync([], CancellationToken.None));
        Assert.Empty(await reader.GetCitiesAsync([], CancellationToken.None));
        Assert.Empty(await reader.GetServiceAreasAsync([], CancellationToken.None));
    }

    [Fact]
    public async Task A_read_through_real_di_emits_one_child_per_foreign_read_with_parent_and_correlation()
    {
        var database = $"geography-trace-{Guid.NewGuid():N}";
        var labels = new ControlledLabels();
        using var provider = BuildProvider(database, labels);
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeographyDbContext>();
        await SeedAsync(db);
        var reader = scope.ServiceProvider.GetRequiredService<IShippingAuthoringDestinationFactsReader>();

        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.DisplayName.StartsWith("module.Shipping.Geography.", StringComparison.Ordinal))
                {
                    spans.Add(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var correlation = Guid.NewGuid().ToString("N");
        using var correlationScope = CorrelationIdContext.BeginScope(correlation);
        using var parent = new ActivitySource("Tooba.Shipping.Tests.Parent").StartActivity("parent.read");
        Assert.NotNull(parent);

        var facts = await reader.ReadAsync([new(ShippingAuthoringAreaKind.Country, CountryId)], LanguageId, CancellationToken.None);
        Assert.True(facts.IsSuccess, facts.IsSuccess ? null : string.Join(",", facts.Errors.Select(error => error.Code)));
        Assert.Same(parent, Activity.Current);
        Assert.Equal(ShippingAuthoringDestinationResolution.Resolved, Assert.Single(facts.Value).Resolution);

        // Exactly one child per foreign read: the country snapshot and its label.
        Assert.Equal(2, spans.Count);
        Assert.Contains(spans, span => Equals(span.GetTagItem(TracingTagNames.Operation), "GetCountriesByKeysForAuthoringDraft"));
        Assert.Contains(spans, span => Equals(span.GetTagItem(TracingTagNames.Operation), "GetDestinationLabelsForAuthoringDraft"));
        foreach (var span in spans)
        {
            Assert.Equal(parent!.SpanId, span.ParentSpanId);
            Assert.Equal(parent.TraceId, span.TraceId);
            Assert.Equal("Shipping", span.GetTagItem(TracingTagNames.ModuleSource));
            Assert.Equal("Geography", span.GetTagItem(TracingTagNames.ModuleTarget));
            Assert.Equal(TracingRequestKinds.ModuleCall, span.GetTagItem(TracingTagNames.RequestKind));
            Assert.Equal(correlation, span.GetTagItem(TracingTagNames.CorrelationId));
            Assert.Equal(ActivityStatusCode.Ok, span.Status);
            var values = string.Join("|", span.TagObjects.Select(tag => Convert.ToString(tag.Value)));
            Assert.DoesNotContain(CountryId.ToString(), values, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Equal(LanguageId, labels.LastLanguageId);
        listener.Dispose();
    }

    [Fact]
    public async Task A_foreign_failure_keeps_its_identity_and_a_caller_token_is_propagated_verbatim()
    {
        var sentinel = new InvalidOperationException("foreign-sentinel-secret");
        var snapshots = new FailingSnapshots(sentinel);
        var labels = new ControlledLabels();
        var reader = new ShippingAuthoringDestinationFactsReader(snapshots, labels, new ModuleCallTracer());

        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.DisplayName.StartsWith("module.Shipping.Geography.", StringComparison.Ordinal))
                {
                    spans.Add(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reader.ReadAsync([new(ShippingAuthoringAreaKind.Country, CountryId)], LanguageId, CancellationToken.None));
        Assert.Same(sentinel, thrown);

        var failed = Assert.Single(spans);
        Assert.Equal(ActivityStatusCode.Error, failed.Status);
        Assert.Equal(typeof(InvalidOperationException).FullName, failed.GetTagItem(TracingTagNames.ExceptionType));
        var values = string.Join("|", failed.TagObjects.Select(tag => Convert.ToString(tag.Value)));
        Assert.DoesNotContain(sentinel.Message, values, StringComparison.Ordinal);
        Assert.DoesNotContain(CountryId.ToString(), values, StringComparison.OrdinalIgnoreCase);

        // The caller token reaches the foreign seam unchanged; cancellation is never converted to success.
        var cancelSource = new CancellationTokenSource();
        var cancelToken = cancelSource.Token;
        var cancelling = new FailingSnapshots(new OperationCanceledException(cancelToken));
        var cancellingReader = new ShippingAuthoringDestinationFactsReader(cancelling, labels, new ModuleCallTracer());
        var cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            cancellingReader.ReadAsync([new(ShippingAuthoringAreaKind.Country, CountryId)], LanguageId, cancelToken));
        Assert.Equal(cancelToken, cancelled.CancellationToken);
        Assert.Equal(cancelToken, cancelling.LastToken);
        listener.Dispose();
    }

    private static async Task SeedAsync(GeographyDbContext db)
    {
        var now = new DateTimeOffset(2026, 10, 23, 0, 0, 0, TimeSpan.Zero);

        // Seeded once as a separate setup step through the module's own domain factories.
        var country = Country.Create("IR", now, CountryId);
        var inactiveCountry = Country.Create("XX", now, CountryInactiveId);
        inactiveCountry.Deactivate(now);
        db.Countries.Add(country);
        db.Countries.Add(inactiveCountry);
        db.Countries.Add(Country.Create("ZZ", now, UnrelatedCountryId));

        db.AdministrativeAreas.Add(AdministrativeArea.Create(CountryId, "ROOT", null, now, RootAreaId));
        db.AdministrativeAreas.Add(AdministrativeArea.Create(CountryId, "DEEP", RootAreaId, now, DeepAreaId));

        db.Cities.Add(City.Create(CountryId, DeepAreaId, "CITY-A", now, CityWithAreaId));
        // A city with NO administrative parent is lawful and must stay that way.
        db.Cities.Add(City.Create(CountryId, null, "CITY-B", now, CityNoAreaId));

        db.ServiceAreas.Add(ServiceArea.Create(CityWithAreaId, "SA", now, ServiceAreaId));

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static ServiceProvider BuildProvider(string database, IGeographyLabelLookup? labels = null)
    {
        var services = new ServiceCollection();
        Compose(services, database, labels);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static void Compose(
        IServiceCollection services,
        string database = "geography-composition",
        IGeographyLabelLookup? labels = null)
    {
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
        services.AddToobaObservabilityFoundation();
        services.AddToobaCqrsFoundation(typeof(IShippingAuthoringDestinationFactsReader).Assembly);
        services.AddSingleton<ICurrentCommerceContext>(new StubCommerceContext());
        services.AddSingleton<IDatabaseConnectionResolver>(new StubConnectionResolver());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        // Controlled external fixture only: the Geography label adapter needs the Localization registry seam,
        // which this proof does not own. No module adapter is re-registered here.
        services.AddSingleton<Tooba.Localization.Contracts.Ports.ILanguageLookup>(new StubLanguageLookup());

        new GeographyModule().AddServices(services, new ConfigurationBuilder().Build(), new StubHostEnvironment());
        new ShippingModule().AddServices(services, new ConfigurationBuilder().Build(), new StubHostEnvironment());

        // ONLY the provider boundary is replaced: the module's own context registrations stay, but their
        // options resolve to in-memory storage instead of a real database.
        services.AddSingleton(new DbContextOptionsBuilder<GeographyDbContext>()
            .UseInMemoryDatabase(database)
            .Options);
        services.AddSingleton(new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"{database}-shipping")
            .Options);

        if (labels is not null)
        {
            // A controlled DATA boundary for the label fact only; the module's own adapters stay untouched.
            var existing = services
                .Where(descriptor => descriptor.ServiceType == typeof(IGeographyLabelLookup))
                .ToArray();
            foreach (var descriptor in existing)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton(labels);
        }
    }

    private sealed class ControlledLabels : IGeographyLabelLookup
    {
        public Guid? LastLanguageId { get; private set; }

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
            LastLanguageId = languageId;
            IReadOnlyDictionary<Guid, string> found = ids.Contains(CountryId)
                ? new Dictionary<Guid, string> { [CountryId] = "ایران" }
                : new Dictionary<Guid, string>();
            return Task.FromResult(found);
        }
    }

    private sealed class FailingSnapshots(Exception failure) : IGeographyPlaceSnapshotReader
    {
        public CancellationToken LastToken { get; private set; }

        public Task<IReadOnlyList<CountrySnapshot>> GetCountriesAsync(
            IReadOnlyCollection<Guid> countryIds,
            CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            throw failure;
        }

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> GetAdministrativeAreasAsync(
            IReadOnlyCollection<Guid> administrativeAreaIds,
            CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            throw failure;
        }

        public Task<IReadOnlyList<CitySnapshot>> GetCitiesAsync(
            IReadOnlyCollection<Guid> cityIds,
            CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            throw failure;
        }

        public Task<IReadOnlyList<ServiceAreaSnapshot>> GetServiceAreasAsync(
            IReadOnlyCollection<Guid> serviceAreaIds,
            CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            throw failure;
        }
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Tooba.Shipping.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class StubLanguageLookup : Tooba.Localization.Contracts.Ports.ILanguageLookup
    {
        public Task<IReadOnlyList<Tooba.Localization.Contracts.Ports.LanguageLookupSnapshot>> ListAsync(
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Tooba.Localization.Contracts.Ports.LanguageLookupSnapshot>>([]);
    }

    private sealed class StubCommerceContext : ICurrentCommerceContext
    {
        public CommerceContext? Current => null;
    }

    private sealed class StubConnectionResolver : IDatabaseConnectionResolver
    {
        public string Resolve(ConnectionReference reference) => "Host=127.0.0.1;Database=unused";
    }
}
