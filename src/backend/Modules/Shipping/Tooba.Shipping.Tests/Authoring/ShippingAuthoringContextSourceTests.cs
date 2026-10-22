using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Localization.Contracts.Ports;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.StoreContext.Contracts.Current;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188 — the Shipping → Localization Contracts read.
/// </summary>
/// <remarks>
/// The adapter is exercised with controlled fakes for the two Contracts seams and the platform
/// edition context; the module-call span assertions use the real <see cref="ModuleCallTracer"/> over
/// the real <see cref="ToobaTelemetry.ActivitySource"/> and an <see cref="ActivityListener"/>, so the
/// canonical child span is observed, not spied. Fakes are labelled as fakes: no real store database
/// or running server is claimed.
/// </remarks>
[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingAuthoringContextSourceTests
{
    private const string Sentinel = "sentinel-secret-language-token-4b7e";
    private static readonly Guid Fa = Guid.Parse("01900000-0000-7000-8000-00000000aa01");
    private static readonly Guid En = Guid.Parse("01900000-0000-7000-8000-00000000aa02");

    [Fact]
    public async Task Every_projected_fact_comes_from_the_source_row_unchanged()
    {
        var languages = new FakeLanguages([
            new LanguageLookupSnapshot(Fa, "fa", "fa-IR", "fa", true)
            {
                IsActive = true,
                Direction = "rtl",
                DisplayName = "Persian",
                NativeName = "فارسی",
                SortOrder = 2,
            },
            new LanguageLookupSnapshot(En, "en", "en-US", "en", false)
            {
                IsActive = false,
                Direction = "ltr",
                DisplayName = "English",
                NativeName = "English",
                SortOrder = 1,
            },
        ]);
        var source = new ShippingAuthoringContextSource(
            languages,
            new FakeStoreCommerce("IRR"),
            new FakeEdition(ToobaEdition.SingleStore),
            new ModuleCallTracer());

        var snapshot = await source.ReadAsync(CancellationToken.None);

        Assert.Equal(ToobaEdition.SingleStore, snapshot.Edition);
        Assert.Equal("IRR", snapshot.DefaultCurrency);
        Assert.Equal(1, languages.Calls);

        Assert.Collection(
            snapshot.Languages,
            first =>
            {
                Assert.Equal(Fa, first.LanguageId);
                Assert.Equal("fa", first.Code);
                Assert.Equal("fa-IR", first.Culture);
                Assert.Equal("fa", first.UrlPrefix);
                Assert.True(first.IsActive);
                Assert.True(first.IsDefault);
                Assert.Equal("rtl", first.Direction);
                Assert.Equal("Persian", first.DisplayName);
                Assert.Equal("فارسی", first.NativeName);
                Assert.Equal(2, first.SortOrder);
            },
            second =>
            {
                Assert.Equal(En, second.LanguageId);
                Assert.False(second.IsActive);
                Assert.False(second.IsDefault);
                Assert.Equal("ltr", second.Direction);
                Assert.Equal(1, second.SortOrder);
            });
    }

    [Fact]
    public async Task A_legacy_snapshot_without_activation_stays_absent_and_is_never_defaulted_active()
    {
        var source = new ShippingAuthoringContextSource(
            new FakeLanguages([new LanguageLookupSnapshot(Fa, "fa", "fa-IR", "fa", true)]),
            new FakeStoreCommerce("IRR"),
            new FakeEdition(ToobaEdition.SingleStore),
            new ModuleCallTracer());

        var snapshot = await source.ReadAsync(CancellationToken.None);

        var language = Assert.Single(snapshot.Languages);
        Assert.Null(language.IsActive);
        Assert.Null(language.Direction);
        Assert.Null(language.DisplayName);
        Assert.Null(language.NativeName);
        Assert.Null(language.SortOrder);
    }

    [Theory]
    [InlineData(ToobaEdition.Unset, "IRR")]
    [InlineData((ToobaEdition)99, "IRR")]
    [InlineData(ToobaEdition.SingleStore, null)]
    [InlineData(ToobaEdition.SingleStore, "  ")]
    public async Task An_unresolved_trusted_context_skips_the_foreign_read_and_stays_unavailable(
        ToobaEdition edition,
        string? currency)
    {
        var languages = new FakeLanguages([
            new LanguageLookupSnapshot(Fa, "fa", "fa-IR", "fa", true) { IsActive = true },
        ]);
        var source = new ShippingAuthoringContextSource(
            languages,
            new FakeStoreCommerce(currency),
            new FakeEdition(edition),
            new ModuleCallTracer());

        var snapshot = await source.ReadAsync(CancellationToken.None);

        // No foreign read, no invented language list: readiness is decided upstream by the handler.
        Assert.Equal(0, languages.Calls);
        Assert.Empty(snapshot.Languages);
        Assert.Equal(currency, snapshot.DefaultCurrency);
        Assert.Equal(edition, snapshot.Edition);
    }

    [Fact]
    public async Task The_foreign_read_emits_one_canonical_child_span_with_correlation_and_parent()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);
        var correlationId = Guid.NewGuid().ToString("N");
        using var correlationScope = CorrelationIdContext.BeginScope(correlationId);
        using var parent = ToobaTelemetry.ActivitySource.StartActivity("test.parent");
        Assert.NotNull(parent);

        var source = new ShippingAuthoringContextSource(
            new FakeLanguages([new LanguageLookupSnapshot(Fa, "fa", "fa-IR", "fa", true) { IsActive = true }]),
            new FakeStoreCommerce("IRR"),
            new FakeEdition(ToobaEdition.SingleStore),
            new ModuleCallTracer());

        await source.ReadAsync(CancellationToken.None);

        var span = Assert.Single(spans);
        Assert.Equal("module.Shipping.Localization.ListConfiguredLanguages", span.OperationName);
        Assert.Equal("Shipping", Tag(span, TracingTagNames.ModuleSource));
        Assert.Equal("Localization", Tag(span, TracingTagNames.ModuleTarget));
        Assert.Equal("Localization", Tag(span, TracingTagNames.Module));
        Assert.Equal("ListConfiguredLanguages", Tag(span, TracingTagNames.Operation));
        Assert.Equal(TracingRequestKinds.ModuleCall, Tag(span, TracingTagNames.RequestKind));
        Assert.Equal(correlationId, Tag(span, TracingTagNames.CorrelationId));
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
        Assert.Equal(parent!.SpanId, span.ParentSpanId);
        Assert.Equal(parent.TraceId, span.TraceId);
        Assert.Same(parent, Activity.Current);

        // No language identity, code, culture, name or request-scoped data is exported as a tag.
        var tagValues = span.TagObjects.Select(tag => tag.Value?.ToString() ?? string.Empty).ToArray();
        Assert.DoesNotContain(tagValues, value => value.Contains(Fa.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(tagValues, value => value.Contains("fa-IR", StringComparison.Ordinal));
        Assert.DoesNotContain(tagValues, value => value.Contains("Persian", StringComparison.Ordinal));
        Assert.DoesNotContain(tagValues, value => value.Contains("IRR", StringComparison.Ordinal));
        Assert.Empty(span.Events);
    }

    [Fact]
    public async Task A_resolved_context_with_an_empty_registry_still_uses_one_traced_call()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);

        var source = new ShippingAuthoringContextSource(
            new FakeLanguages([]),
            new FakeStoreCommerce("IRR"),
            new FakeEdition(ToobaEdition.Marketplace),
            new ModuleCallTracer());

        var snapshot = await source.ReadAsync(CancellationToken.None);

        Assert.Equal(ToobaEdition.Marketplace, snapshot.Edition);
        Assert.Empty(snapshot.Languages);
        Assert.Single(spans);
    }

    [Fact]
    public async Task A_dependency_failure_records_type_only_and_rethrows_the_same_exception()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);
        var failure = new InvalidOperationException(Sentinel);
        var languages = new FakeLanguages([]) { ThrowOnRead = failure };

        var source = new ShippingAuthoringContextSource(
            languages,
            new FakeStoreCommerce("IRR"),
            new FakeEdition(ToobaEdition.SingleStore),
            new ModuleCallTracer());

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => source.ReadAsync(CancellationToken.None));

        Assert.Same(failure, thrown);
        var span = Assert.Single(spans);
        Assert.Equal(typeof(InvalidOperationException).FullName, Tag(span, TracingTagNames.ExceptionType));
        var tagValues = span.TagObjects.Select(tag => tag.Value?.ToString() ?? string.Empty).ToArray();
        Assert.DoesNotContain(tagValues, value => value.Contains(Sentinel, StringComparison.Ordinal));
        Assert.Empty(span.Events);
    }

    [Fact]
    public async Task Caller_cancellation_is_preserved_through_the_traced_read()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var languages = new FakeLanguages([]) { ThrowOnRead = new OperationCanceledException(cancellation.Token) };

        var source = new ShippingAuthoringContextSource(
            languages,
            new FakeStoreCommerce("IRR"),
            new FakeEdition(ToobaEdition.SingleStore),
            new ModuleCallTracer());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.ReadAsync(cancellation.Token));
        Assert.Equal(1, languages.Calls);
    }

    [Fact]
    public async Task The_real_module_di_resolves_the_source_and_its_contracts_seams()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
        services.AddToobaObservabilityFoundation();
        services.AddSingleton<ILanguageLookup>(new FakeLanguages([
            new LanguageLookupSnapshot(Fa, "fa", "fa-IR", "fa", true)
            {
                IsActive = true,
                Direction = "rtl",
                DisplayName = "Persian",
                NativeName = "فارسی",
                SortOrder = 0,
            },
        ]));
        services.AddSingleton<ICurrentStoreCommerceContext>(new FakeStoreCommerce("IRR"));
        services.AddSingleton<ICurrentEdition>(new FakeEdition(ToobaEdition.SingleStore));

        // The real module composition, not a hand-built object graph: the source must be reachable
        // through the module's own AddServices with the existing foundation tracer.
        new Tooba.Shipping.Infrastructure.DependencyInjection.ShippingModule()
            .AddServices(services, new ConfigurationBuilder().Build(), new StubHostEnvironment());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        var source = scope.ServiceProvider
            .GetRequiredService<Tooba.Shipping.Application.Authoring.Ports.IShippingAuthoringContextSource>();
        Assert.IsType<ShippingAuthoringContextSource>(source);
        Assert.Same(
            typeof(ModuleCallTracer),
            provider.GetRequiredService<IModuleCallTracer>().GetType());

        var snapshot = await source.ReadAsync(CancellationToken.None);
        var language = Assert.Single(snapshot.Languages);
        Assert.True(language.IsActive);
        Assert.Equal("IRR", snapshot.DefaultCurrency);
    }

    private static string? Tag(Activity activity, string key) => activity.GetTagItem(key)?.ToString();

    private static ActivityListener CreateListener(List<Activity> sink)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ToobaTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.OperationName.StartsWith("module.Shipping.Localization.", StringComparison.Ordinal))
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

    private sealed class FakeLanguages(IReadOnlyList<LanguageLookupSnapshot> rows) : ILanguageLookup
    {
        public int Calls { get; private set; }

        public Exception? ThrowOnRead { get; init; }

        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken)
        {
            Calls++;
            if (ThrowOnRead is not null)
            {
                return Task.FromException<IReadOnlyList<LanguageLookupSnapshot>>(ThrowOnRead);
            }

            return Task.FromResult(rows);
        }
    }

    private sealed class FakeStoreCommerce(string? defaultCurrency) : ICurrentStoreCommerceContext
    {
        public StoreCommerceContext? Current => new("fa-IR", defaultCurrency, "web");
    }

    private sealed class FakeEdition(ToobaEdition edition) : ICurrentEdition
    {
        public EditionContext? Current =>
            edition == ToobaEdition.Unset ? null : new EditionContext(edition, "test-deployment");
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tooba.Shipping.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
