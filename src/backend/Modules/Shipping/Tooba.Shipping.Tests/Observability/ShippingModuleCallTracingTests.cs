using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Infrastructure.DependencyInjection;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Observability;

/// <summary>
/// TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1 — real (listener-observed) canonical module-call spans on
/// the two Shipping outbound Contracts read adapters.
/// </summary>
/// <remarks>
/// These tests use the real <see cref="ModuleCallTracer"/> over the real
/// <see cref="ToobaTelemetry.ActivitySource"/> with an <see cref="ActivityListener"/>, not a spy: they
/// assert the exported span name, source/target/operation tags, correlation tag, parent/trace identity,
/// single disposal, parent restoration, and that no identifier or exception message is exported.
/// Collection parallelization is disabled and the sink is filtered to <c>module.Shipping.*</c> so other
/// tests sharing the process-wide source cannot falsify the counts.
/// </remarks>
[CollectionDefinition(nameof(ShippingModuleCallTracingCollection), DisableParallelization = true)]
public sealed class ShippingModuleCallTracingCollection;

[Collection(nameof(ShippingModuleCallTracingCollection))]
public sealed class ShippingModuleCallTracingTests
{
    private const string Sentinel = "sentinel-secret-exception-message-9f3c";
    private const string ServiceCode = "tooba-post";
    private const string OptionCode = "express";
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AreaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CityId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ServiceAreaId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public async Task Geography_read_emits_one_child_span_with_canonical_identity_and_restores_the_parent()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);
        var correlationId = Guid.NewGuid().ToString("N");
        using var correlationScope = CorrelationIdContext.BeginScope(correlationId);
        using var parent = ToobaTelemetry.ActivitySource.StartActivity("test.parent");
        Assert.NotNull(parent);

        var geography = new RecordingGeography(accept: true);
        var validator = new GeographyDestinationScopeValidator(geography, new ModuleCallTracer());

        var result = await validator.ValidateAsync(Scope(), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var span = Assert.Single(spans);
        Assert.Equal("module.Shipping.Geography.ValidateDestinationIdentifiers", span.OperationName);
        Assert.Equal("Shipping", Tag(span, TracingTagNames.ModuleSource));
        Assert.Equal("Geography", Tag(span, TracingTagNames.ModuleTarget));
        Assert.Equal("Geography", Tag(span, TracingTagNames.Module));
        Assert.Equal("ValidateDestinationIdentifiers", Tag(span, TracingTagNames.Operation));
        Assert.Equal(TracingRequestKinds.ModuleCall, Tag(span, TracingTagNames.RequestKind));
        Assert.Equal(correlationId, Tag(span, TracingTagNames.CorrelationId));
        Assert.Equal(ActivityStatusCode.Ok, span.Status);

        // One child of the incoming span, sharing its trace; the ambient parent is restored afterwards.
        Assert.Equal(parent!.SpanId, span.ParentSpanId);
        Assert.Equal(parent.TraceId, span.TraceId);
        Assert.Same(parent, Activity.Current);

        // The exact submitted identifiers are forwarded unchanged (single Geography read).
        Assert.Equal(1, geography.Calls);
        Assert.Equal(new DestinationIdentifiers(CountryId, AreaId, CityId, ServiceAreaId), geography.Received);
    }

    [Fact]
    public async Task Geography_rejection_is_one_completed_span_marked_business_failure()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        var validator = new GeographyDestinationScopeValidator(new RecordingGeography(accept: false), new ModuleCallTracer());

        var result = await validator.ValidateAsync(Scope(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrorCodes.DestinationScopeInvalid, result.FirstError.Code);

        var span = Assert.Single(spans);
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
        Assert.Equal("business_failure", Tag(span, TracingTagNames.ResultStatus));
        Assert.Equal(ShippingErrorCodes.DestinationScopeInvalid, Tag(span, TracingTagNames.ErrorCode));
    }

    [Fact]
    public async Task Geography_dependency_failure_records_type_only_and_rethrows_the_same_exception()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        var failure = new InvalidOperationException(Sentinel);
        var validator = new GeographyDestinationScopeValidator(
            new RecordingGeography(accept: true) { ThrowOnRead = failure },
            new ModuleCallTracer());

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.ValidateAsync(Scope(), CancellationToken.None));

        Assert.Same(failure, thrown);
        AssertNoLeak(Assert.Single(spans), typeof(InvalidOperationException));
    }

    [Fact]
    public async Task Geography_cancellation_stays_cancellation_and_never_becomes_a_business_failure()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        using var cts = new CancellationTokenSource();
        var geography = new RecordingGeography(accept: true) { ThrowOnRead = new OperationCanceledException(cts.Token) };
        var validator = new GeographyDestinationScopeValidator(geography, new ModuleCallTracer());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => validator.ValidateAsync(Scope(), cts.Token));

        Assert.Equal(cts.Token, geography.ReceivedToken);
        var span = Assert.Single(spans);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Equal(typeof(OperationCanceledException).FullName, Tag(span, TracingTagNames.ExceptionType));
        Assert.NotEqual("business_failure", Tag(span, TracingTagNames.ResultStatus));
    }

    [Fact]
    public async Task Service_level_success_emits_exactly_one_catalog_span_and_skips_the_canonical_branch()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        var catalog = new RecordingCatalog();
        var canonical = new RecordingCanonicalReader(binding: true);
        var validator = new FulfillmentShippingMethodOptionValidator(catalog, new ModuleCallTracer(), canonical);

        var result = await validator.ValidateAsync(ServiceCode, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var span = Assert.Single(spans);
        Assert.Equal("module.Shipping.Fulfillment.ListShippingCatalog", span.OperationName);
        Assert.Equal("Fulfillment", Tag(span, TracingTagNames.ModuleTarget));
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
        Assert.Equal(0, canonical.BindingCalls);
        AssertNoLeak(span, exceptionType: null);
    }

    [Fact]
    public async Task Legacy_option_success_emits_one_catalog_span_and_no_canonical_span()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        var canonical = new RecordingCanonicalReader(binding: true);
        var validator = new FulfillmentShippingMethodOptionValidator(new RecordingCatalog(), new ModuleCallTracer(), canonical);

        var result = await validator.ValidateAsync(ServiceCode, OptionCode, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var span = Assert.Single(spans);
        Assert.Equal("module.Shipping.Fulfillment.ListShippingCatalog", span.OperationName);
        Assert.Equal(0, canonical.BindingCalls);
    }

    [Fact]
    public async Task Canonical_fallback_emits_two_distinct_fulfillment_spans()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        var catalog = new RecordingCatalog();
        var canonical = new RecordingCanonicalReader(binding: true);
        var validator = new FulfillmentShippingMethodOptionValidator(catalog, new ModuleCallTracer(), canonical);

        var result = await validator.ValidateAsync(ServiceCode, "linked-option", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, spans.Count);
        Assert.Equal(1, spans.Count(s => s.OperationName == "module.Shipping.Fulfillment.ListShippingCatalog"));
        Assert.Equal(1, spans.Count(s => s.OperationName == "module.Shipping.Fulfillment.ResolveCanonicalMethodBinding"));
        Assert.All(spans, span => Assert.Equal(ActivityStatusCode.Ok, span.Status));
        Assert.Equal(1, canonical.BindingCalls);
        Assert.Equal(ServiceCode, canonical.ServiceCode);
        Assert.Equal("linked-option", canonical.OptionCode);
    }

    [Fact]
    public async Task Canonical_false_is_a_completed_lookup_and_the_shipping_rejection_is_unchanged()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        var validator = new FulfillmentShippingMethodOptionValidator(
            new RecordingCatalog(),
            new ModuleCallTracer(),
            new RecordingCanonicalReader(binding: false));

        var result = await validator.ValidateAsync(ServiceCode, "linked-option", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrorCodes.ServiceOptionUnknown, result.FirstError.Code);
        Assert.Equal(2, spans.Count);
        Assert.All(spans, span => Assert.Equal(ActivityStatusCode.Ok, span.Status));
        Assert.All(spans, span => Assert.Null(Tag(span, TracingTagNames.ExceptionType)));
    }

    [Fact]
    public async Task Absent_canonical_reader_emits_one_span_and_no_fabricated_call()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        var validator = new FulfillmentShippingMethodOptionValidator(new RecordingCatalog(), new ModuleCallTracer());

        var result = await validator.ValidateAsync(ServiceCode, "linked-option", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrorCodes.ServiceOptionUnknown, result.FirstError.Code);
        Assert.Single(spans);
    }

    [Fact]
    public async Task Unknown_service_and_unknown_option_are_completed_lookups_without_error_spans()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        var validator = new FulfillmentShippingMethodOptionValidator(
            new RecordingCatalog(),
            new ModuleCallTracer(),
            new RecordingCanonicalReader(binding: false));

        var unknownService = await validator.ValidateAsync("missing-service", OptionCode, CancellationToken.None);

        Assert.Equal(ShippingErrorCodes.ServiceCodeUnknown, unknownService.FirstError.Code);
        var single = Assert.Single(spans);
        Assert.Equal(ActivityStatusCode.Ok, single.Status);
    }

    [Fact]
    public async Task Local_rejection_emits_zero_foreign_spans()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        var catalog = new RecordingCatalog();
        var validator = new FulfillmentShippingMethodOptionValidator(catalog, new ModuleCallTracer());

        var result = await validator.ValidateAsync("   ", OptionCode, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrorCodes.ServiceCodeRequired, result.FirstError.Code);
        Assert.Empty(spans);
        Assert.Equal(0, catalog.Calls);
    }

    [Fact]
    public async Task Fulfillment_dependency_failure_records_type_only_and_rethrows_the_same_exception()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        var failure = new TimeoutException(Sentinel);
        var catalog = new RecordingCatalog { ThrowOnRead = failure };
        var validator = new FulfillmentShippingMethodOptionValidator(catalog, new ModuleCallTracer());

        var thrown = await Assert.ThrowsAsync<TimeoutException>(
            () => validator.ValidateAsync(ServiceCode, OptionCode, CancellationToken.None));

        Assert.Same(failure, thrown);
        var span = Assert.Single(spans);
        AssertNoLeak(span, typeof(TimeoutException));
    }

    [Fact]
    public async Task Submitted_identifiers_and_exception_messages_are_never_exported_as_tags()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        var validator = new FulfillmentShippingMethodOptionValidator(new RecordingCatalog(), new ModuleCallTracer());
        await validator.ValidateAsync(ServiceCode, OptionCode, CancellationToken.None);
        await new GeographyDestinationScopeValidator(new RecordingGeography(accept: true), new ModuleCallTracer())
            .ValidateAsync(Scope(), CancellationToken.None);

        Assert.Equal(2, spans.Count);
        foreach (var span in spans)
        {
            AssertNoLeak(span, exceptionType: null);
            var tagValues = span.TagObjects.Select(tag => tag.Value?.ToString() ?? string.Empty).ToArray();
            Assert.DoesNotContain(tagValues, value => value.Contains(ServiceCode, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(tagValues, value => value.Contains(OptionCode, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(tagValues, value => value.Contains(CountryId.ToString(), StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(tagValues, value => value.Contains(CityId.ToString(), StringComparison.OrdinalIgnoreCase));
            Assert.Empty(span.Events);
        }
    }

    [Fact]
    public void Both_adapters_resolve_from_the_real_module_registration_with_the_foundation_tracer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaObservabilityFoundation();
        services.AddSingleton<IGeographyLookup>(new RecordingGeography(accept: true));
        services.AddSingleton<IShippingCatalogReader>(new RecordingCatalog());
        services.AddSingleton<IShippingOneLevelCanonicalMethodReader>(new RecordingCanonicalReader(binding: true));
        new ShippingModule().AddServices(services, new ConfigurationBuilder().Build(), new StubHostEnvironment());

        var tracer = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IModuleCallTracer));
        Assert.Equal(typeof(ModuleCallTracer), tracer.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, tracer.Lifetime);

        var destination = Assert.Single(services, d => d.ServiceType == typeof(IShippingDestinationScopeValidator));
        Assert.Equal(typeof(GeographyDestinationScopeValidator), destination.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, destination.Lifetime);

        var methodOption = Assert.Single(services, d => d.ServiceType == typeof(IShippingMethodOptionValidator));
        Assert.Equal(typeof(FulfillmentShippingMethodOptionValidator), methodOption.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, methodOption.Lifetime);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IShippingDestinationScopeValidator>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IShippingMethodOptionValidator>());
    }

    [Fact]
    public void Both_adapters_require_the_canonical_tracer_as_a_mandatory_constructor_argument()
    {
        // A silent regression could re-add a nullable/optional tracer "for old tests". Both adapters
        // must demand the canonical abstraction with no default value, so forgetting to wire it is a
        // composition/compile error rather than untraced production traffic.
        foreach (var adapter in new[]
                 {
                     typeof(GeographyDestinationScopeValidator),
                     typeof(FulfillmentShippingMethodOptionValidator),
                 })
        {
            var constructor = Assert.Single(adapter.GetConstructors());
            var tracer = Assert.Single(
                constructor.GetParameters(),
                parameter => parameter.ParameterType == typeof(IModuleCallTracer));
            Assert.False(tracer.IsOptional, $"{adapter.Name} must not default the module-call tracer");
            Assert.False(tracer.HasDefaultValue, $"{adapter.Name} must not carry a default tracer value");
        }
    }

    [Fact]
    public async Task Adapter_resolved_from_module_DI_actually_emits_the_module_call_span()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaObservabilityFoundation();
        services.AddSingleton<IGeographyLookup>(new RecordingGeography(accept: true));
        new ShippingModule().AddServices(services, new ConfigurationBuilder().Build(), new StubHostEnvironment());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IShippingDestinationScopeValidator>();

        var result = await validator.ValidateAsync(Scope(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "module.Shipping.Geography.ValidateDestinationIdentifiers",
            Assert.Single(spans).OperationName);
    }

    private static void AssertNoLeak(Activity span, Type? exceptionType)
    {
        Assert.Equal(exceptionType?.FullName, Tag(span, TracingTagNames.ExceptionType));
        var tagValues = span.TagObjects.Select(tag => tag.Value?.ToString() ?? string.Empty).ToArray();
        Assert.DoesNotContain(tagValues, value => value.Contains(Sentinel, StringComparison.Ordinal));
        Assert.Empty(span.Events);
    }

    private static string? Tag(Activity activity, string key) => activity.GetTagItem(key)?.ToString();

    private static ShippingDestinationScopeWrite Scope() => new(CountryId, AreaId, CityId, ServiceAreaId);

    private static ActivityListener CreateListener(List<Activity> sink)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ToobaTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                // Isolate from other tests sharing the process-wide source: only Shipping module calls.
                if (activity.OperationName.StartsWith("module.Shipping.", StringComparison.Ordinal))
                {
                    lock (sink)
                    {
                        sink.Add(activity);
                    }
                }
            },
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private sealed class RecordingGeography(bool accept) : IGeographyLookup
    {
        public int Calls { get; private set; }

        public DestinationIdentifiers? Received { get; private set; }

        public CancellationToken ReceivedToken { get; private set; }

        public Exception? ThrowOnRead { get; init; }

        public Task<IReadOnlyList<CountrySnapshot>> ListCountriesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CountrySnapshot>>([]);

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> ListAdministrativeAreasAsync(
            Guid countryId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AdministrativeAreaSnapshot>>([]);

        public Task<IReadOnlyList<CitySnapshot>> ListCitiesAsync(
            Guid countryId,
            Guid? administrativeAreaId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CitySnapshot>>([]);

        public Task<IReadOnlyList<ServiceAreaSnapshot>> ListServiceAreasAsync(
            Guid cityId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ServiceAreaSnapshot>>([]);

        public Task<DestinationIdentifiers?> GetByIdsAsync(
            DestinationIdentifiers identifiers,
            CancellationToken cancellationToken)
        {
            Calls++;
            Received = identifiers;
            ReceivedToken = cancellationToken;
            if (ThrowOnRead is not null)
            {
                return Task.FromException<DestinationIdentifiers?>(ThrowOnRead);
            }

            return Task.FromResult(accept ? identifiers : null);
        }

        public Task<DestinationResolution> ResolveLegacyAsync(
            string? countryCode,
            string? administrativeAreaName,
            string? cityName,
            string? serviceAreaName,
            CancellationToken cancellationToken) =>
            Task.FromResult(DestinationResolution.Unresolved());
    }

    private sealed class RecordingCatalog : IShippingCatalogReader
    {
        public int Calls { get; private set; }

        public Exception? ThrowOnRead { get; init; }

        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken)
        {
            Calls++;
            if (ThrowOnRead is not null)
            {
                return Task.FromException<IReadOnlyList<ShippingCatalogServiceSnapshot>>(ThrowOnRead);
            }

            return Task.FromResult<IReadOnlyList<ShippingCatalogServiceSnapshot>>(
            [
                new ShippingCatalogServiceSnapshot(
                    Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    ServiceCode,
                    "internal",
                    "icon",
                    "color",
                    true,
                    1,
                    [],
                    [
                        new ShippingCatalogOptionSnapshot(
                            Guid.Parse("66666666-6666-6666-6666-666666666666"),
                            Guid.Parse("55555555-5555-5555-5555-555555555555"),
                            OptionCode,
                            true,
                            1,
                            []),
                    ]),
            ]);
        }

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken) =>
            Task.FromResult<ShippingCatalogServiceSnapshot?>(null);
    }

    private sealed class RecordingCanonicalReader(bool binding) : IShippingOneLevelCanonicalMethodReader
    {
        public int BindingCalls { get; private set; }

        public string? ServiceCode { get; private set; }

        public string? OptionCode { get; private set; }

        public Task<IReadOnlyList<ShippingOneLevelCanonicalMethodSnapshot>> ListActiveUnboundAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingOneLevelCanonicalMethodSnapshot>>([]);

        public Task<bool> IsCanonicalMethodBindingAsync(
            string serviceCode,
            string? optionCode,
            CancellationToken cancellationToken)
        {
            BindingCalls++;
            ServiceCode = serviceCode;
            OptionCode = optionCode;
            return Task.FromResult(binding);
        }
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "Tooba.Host";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
