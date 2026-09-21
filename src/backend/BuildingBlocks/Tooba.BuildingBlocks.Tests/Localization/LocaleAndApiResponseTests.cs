using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.BuildingBlocks.Tests.Localization;

public sealed class RequestLocaleResolverTests
{
    [Fact]
    public void Selects_culture_from_accept_language_quality()
    {
        var resolver = new RequestLocaleResolver(new RequestLocaleOptions
        {
            DefaultCulture = "en",
            FallbackCultures = ["en"],
        });
        var culture = resolver.Resolve("fr-FR,fr;q=0.9,en;q=0.8");
        Assert.Equal("fr-FR", culture.Name);
    }

    [Fact]
    public void Falls_back_to_configured_chain_not_fa_first()
    {
        var resolver = new RequestLocaleResolver(new RequestLocaleOptions
        {
            DefaultCulture = "en",
            FallbackCultures = ["en", "de"],
            SupportedCultures = ["en", "de"],
        });
        var culture = resolver.Resolve("fa-IR");
        Assert.Equal("en", culture.TwoLetterISOLanguageName);
    }

    [Fact]
    public void Supports_non_fa_en_locale()
    {
        var resolver = new RequestLocaleResolver(new RequestLocaleOptions());
        Assert.Equal("zh-CN", resolver.Resolve("zh-CN").Name);
    }

    [Fact]
    public void Malformed_header_is_safe()
    {
        var resolver = new RequestLocaleResolver(new RequestLocaleOptions { DefaultCulture = "en" });
        Assert.Equal("en", resolver.Resolve("@@@;;;").TwoLetterISOLanguageName);
        Assert.Equal("en", resolver.Resolve(null).TwoLetterISOLanguageName);
    }
}

public sealed class ApiResponseFactoryTests
{
    [Fact]
    public void Problem_body_includes_status_error_code_correlation_and_trace()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new StubHostEnvironment(Environments.Production));
        services.AddToobaObservabilityFoundation();
        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<ApiResponseFactory>();
        var correlation = sp.GetRequiredService<ICorrelationIdProvider>();
        var id = correlation.EnsureCorrelationId(Guid.NewGuid().ToString("N"));

        var problem = factory.CreateProblemDetails(
            new SemanticException(new SemanticError("demo.not_found")));

        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("demo.not_found", problem.Extensions["errorCode"]?.ToString());
        Assert.Equal(id, problem.Extensions["correlationId"]?.ToString());
        Assert.False(string.IsNullOrWhiteSpace(problem.Extensions["traceId"]?.ToString()));
        Assert.Null(problem.Detail);
        Assert.DoesNotContain("SemanticException", problem.Title ?? string.Empty, StringComparison.Ordinal);
    }

    private sealed class StubHostEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public sealed class TracingPolicyTests
{
    [Fact]
    public void Health_and_ready_are_excluded()
    {
        Assert.True(ToobaTracingPolicy.IsExcludedPath("/health"));
        Assert.True(ToobaTracingPolicy.IsExcludedPath("/ready"));
        Assert.False(ToobaTracingPolicy.ShouldCollectHttpRequest("/health"));
    }

    [Fact]
    public void Enrich_existing_when_activity_present_and_no_duplicate_fallback()
    {
        using var activity = new System.Diagnostics.Activity("test").Start();
        var decision = ToobaTracingPolicy.ResolveHttpSpan("/v1/seller/offers", activity);
        Assert.Equal(HttpSpanDecision.EnrichExisting, decision);

        var http = new DefaultHttpContext();
        http.Request.Path = "/v1/seller/offers";
        http.Request.Method = "GET";
        ToobaTraceEnricher.EnrichHttpRequest(http, "abc");
        Assert.Equal("abc", activity.GetTagItem(TracingTagNames.CorrelationId));
    }

    [Fact]
    public void CreateFallback_does_not_start_activity_in_enricher()
    {
        System.Diagnostics.Activity.Current = null;
        var decision = ToobaTracingPolicy.ResolveHttpSpan("/v1/x", current: null);
        Assert.Equal(HttpSpanDecision.CreateFallback, decision);
        var http = new DefaultHttpContext();
        http.Request.Path = "/v1/x";
        ToobaTraceEnricher.EnrichHttpRequest(http, "cid");
        Assert.Null(System.Diagnostics.Activity.Current);
    }
}
