using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Endpoints;
using Tooba.Shipping.Endpoints.Admin;
using Tooba.Shipping.Endpoints.Errors;
using Tooba.Shipping.Endpoints.Resources;
using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W1 — canonical Shipping error catalog and resources.
/// The declared code set is compared against the registered descriptor set by exact set equality
/// (not by a hard-coded count), the legacy HTTP contract of every code is pinned, and the composed
/// catalog is built through the real production registration path.
/// </summary>
public sealed class ShippingErrorCatalogAndResourcesTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo Persian = CultureInfo.GetCultureInfo("fa");
    private static readonly CultureInfo Uncovered = CultureInfo.GetCultureInfo("de-DE");

    [Fact]
    public void Declared_codes_and_registered_descriptors_are_set_equal()
    {
        var declared = DeclaredCodes();
        var registered = RegisteredCodes();

        Assert.Equal(declared.OrderBy(code => code, StringComparer.Ordinal),
            registered.OrderBy(code => code, StringComparer.Ordinal));
        Assert.Equal(registered.Length, registered.Distinct(StringComparer.Ordinal).Count());
        Assert.All(declared, code => Assert.True(code.StartsWith("shipping.", StringComparison.Ordinal), code));
    }

    [Fact]
    public void Every_registered_descriptor_keeps_the_legacy_http_contract()
    {
        // TB-188 added exactly one explicitly classified availability code, TB-189 the two
        // owner-scope gate codes and TB-191 the group/rate-row availability and currency codes; the
        // remaining legacy codes are NOT re-classified and must keep their 400/Business/Warning contract.
        var explicitCodes = new[]
        {
            ShippingErrorCodes.AuthoringConfigurationUnavailable,
            ShippingErrorCodes.AuthoringOwnerScopeDenied,
            ShippingErrorCodes.AuthoringOwnerScopeUnavailable,
            ShippingErrorCodes.AuthoringDraftNotFound,
            ShippingErrorCodes.AuthoringDraftRevisionConflict,
            ShippingErrorCodes.AuthoringDraftBindingDuplicate,
            ShippingErrorCodes.AuthoringRegionSelectionOverlap,
            ShippingErrorCodes.AuthoringRegionPayloadInvalid,
            ShippingErrorCodes.AuthoringRegionGroupNotFound,
            ShippingErrorCodes.AuthoringRateCurrencyMismatch,
        };
        var legacy = new ShippingErrorCatalogContributor()
            .Contribute()
            .Where(descriptor => !explicitCodes.Contains(descriptor.Code, StringComparer.Ordinal))
            .ToArray();
        // TB-193 adds two legacy-contract 400/Business/Warning codes for the weekly schedule week/day
        // shape, so the legacy set grows from 51 to 53 without changing any existing code.
        Assert.Equal(54, legacy.Length);

        foreach (var descriptor in legacy)
        {
            Assert.Equal(StatusCodes.Status400BadRequest, descriptor.HttpStatus);
            Assert.Equal(ErrorClassification.Business, descriptor.Classification);
            Assert.Equal(ErrorSeverity.Warning, descriptor.Severity);
            Assert.Equal(descriptor.Code, descriptor.LocalizationKey);
            Assert.False(string.IsNullOrWhiteSpace(descriptor.SafeTitleFallback), descriptor.Code);
        }
    }

    [Fact]
    public void The_authoring_context_failure_is_the_only_explicit_503_platform_code()
    {
        var descriptors = new ShippingErrorCatalogContributor().Contribute();

        var descriptor = Assert.Single(
            descriptors,
            candidate => candidate.Code == ShippingErrorCodes.AuthoringConfigurationUnavailable);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, descriptor.HttpStatus);
        Assert.Equal(ErrorClassification.Platform, descriptor.Classification);
        Assert.Equal(ErrorSeverity.Error, descriptor.Severity);
        Assert.Equal(descriptor.Code, descriptor.LocalizationKey);
        Assert.False(string.IsNullOrWhiteSpace(descriptor.SafeTitleFallback));

        // No other shipping.* code silently becomes unavailable: the explicit 503 classification is
        // bounded to the single TB-188 configuration code.
        Assert.Single(descriptors, candidate => candidate.HttpStatus == StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public void The_owner_scope_gate_uses_one_403_denial_and_one_404_capability_code()
    {
        var descriptors = new ShippingErrorCatalogContributor().Contribute();

        var denied = Assert.Single(
            descriptors,
            candidate => candidate.Code == ShippingErrorCodes.AuthoringOwnerScopeDenied);
        Assert.Equal(StatusCodes.Status403Forbidden, denied.HttpStatus);
        Assert.Equal(ErrorClassification.Forbidden, denied.Classification);
        Assert.Equal(ErrorSeverity.Warning, denied.Severity);
        Assert.Equal(denied.Code, denied.LocalizationKey);

        var unavailable = Assert.Single(
            descriptors,
            candidate => candidate.Code == ShippingErrorCodes.AuthoringOwnerScopeUnavailable);
        Assert.Equal(StatusCodes.Status404NotFound, unavailable.HttpStatus);
        Assert.Equal(ErrorClassification.NotFound, unavailable.Classification);
        Assert.Equal(ErrorSeverity.Warning, unavailable.Severity);
        Assert.Equal(unavailable.Code, unavailable.LocalizationKey);

        // Denial and capability-unavailable stay distinct: a SingleStore seller route is a 404
        // capability fact, never a 403 permission verdict about a seller that does not exist.
        Assert.NotEqual(denied.HttpStatus, unavailable.HttpStatus);
        Assert.Single(descriptors, candidate => candidate.HttpStatus == StatusCodes.Status403Forbidden);
        Assert.Equal(3, descriptors.Count(candidate => candidate.HttpStatus == StatusCodes.Status404NotFound));

        // Exactly the seven explicitly classified non-400 codes exist (503, 403, three 404s, two 409s);
        // nothing else drifted.
        Assert.Equal(
            new HashSet<string>(
                [
                    ShippingErrorCodes.AuthoringConfigurationUnavailable,
                    ShippingErrorCodes.AuthoringOwnerScopeDenied,
                    ShippingErrorCodes.AuthoringOwnerScopeUnavailable,
                    ShippingErrorCodes.AuthoringDraftNotFound,
                    ShippingErrorCodes.AuthoringDraftRevisionConflict,
                    ShippingErrorCodes.AuthoringRegionGroupNotFound,
                    ShippingErrorCodes.AuthoringRateCurrencyMismatch,
                ],
                StringComparer.Ordinal),
            descriptors
                .Where(candidate => candidate.HttpStatus != StatusCodes.Status400BadRequest)
                .Select(candidate => candidate.Code)
                .ToHashSet(StringComparer.Ordinal));
    }

    [Fact]
    public void Composed_catalog_has_one_owner_and_suppresses_nothing()
    {
        var forward = new ErrorDefinitionCatalog(
        [
            new FoundationErrorCatalogContributor(),
            new ShippingErrorCatalogContributor(),
        ]);
        // Contributor order must not change ownership or registrability.
        var reversed = new ErrorDefinitionCatalog(
        [
            new ShippingErrorCatalogContributor(),
            new FoundationErrorCatalogContributor(),
        ]);

        foreach (var code in DeclaredCodes())
        {
            Assert.True(forward.TryGet(code, out var descriptor), code);
            Assert.True(reversed.TryGet(code, out var reversedDescriptor), code);
            Assert.Equal(descriptor, reversedDescriptor);
            Assert.Equal(code, descriptor.LocalizationKey);
        }

        // A shipping code is never claimed by the Foundation keyspace.
        Assert.DoesNotContain(
            new FoundationErrorCatalogContributor().Contribute(),
            d => d.Code.StartsWith("shipping.", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(forward.RegisteredCodes.Count, reversed.RegisteredCodes.Count);
    }

    [Fact]
    public void Every_code_has_neutral_and_persian_resources_and_canonical_fallback()
    {
        var localizer = new ResourceErrorMessageLocalizer(
            [new FoundationErrorResourceSet(), new ShippingErrorResourceSet()],
            []);

        foreach (var code in DeclaredCodes())
        {
            var neutral = ShippingErrorResources.Manager.GetString(code, English);
            var persian = ShippingErrorResources.Manager.GetString(code, Persian);

            Assert.False(string.IsNullOrWhiteSpace(neutral), $"{code} neutral");
            Assert.False(string.IsNullOrWhiteSpace(persian), $"{code} fa");
            Assert.NotEqual(neutral, persian);

            // An additional configured locale without a translation falls back to the neutral
            // resource, never to the key itself.
            Assert.Equal(neutral, ShippingErrorResources.Manager.GetString(code, Uncovered));
            Assert.Equal(neutral, localizer.Localize(code, Uncovered, new Dictionary<string, string?>(), "fallback"));
            Assert.Equal(persian, localizer.Localize(code, Persian, new Dictionary<string, string?>(), "fallback"));
        }
    }

    [Fact]
    public void Foundation_keyspaces_are_not_claimed_by_the_shipping_resource_set()
    {
        var set = new ShippingErrorResourceSet();

        Assert.True(set.Owns(ShippingErrorCodes.RatePolicyNotFound));
        Assert.False(set.Owns(FoundationErrorCodes.AdminAuthorizationDenied));
        Assert.False(set.Owns("validation.failed"));
        Assert.False(set.Owns("platform.unexpected"));
        // shipping_service* belongs to Fulfillment, not Shipping.
        Assert.False(set.Owns("shipping_service.code.invalid"));
    }

    [Fact]
    public async Task Real_factory_returns_400_with_localized_title_and_trace_context()
    {
        using var provider = BuildProvider();
        var factory = provider.GetRequiredService<ApiResponseFactory>();
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        httpContext.Request.Path = "/v1/admin/shipping-services/x/rates";
        httpContext.Request.Headers.AcceptLanguage = "fa";
        httpContext.Response.Body = new MemoryStream();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        var result = factory.FromFailure(new SemanticError(ShippingErrorCodes.RatePolicyNotFound));
        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        httpContext.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(httpContext.Response.Body);
        var root = body.RootElement;

        Assert.Equal(StatusCodes.Status400BadRequest, root.GetProperty("status").GetInt32());
        Assert.Equal(ShippingErrorCodes.RatePolicyNotFound, root.GetProperty("errorCode").GetString());
        Assert.Equal(
            ShippingErrorResources.Manager.GetString(ShippingErrorCodes.RatePolicyNotFound, Persian),
            root.GetProperty("title").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("correlationId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("instance").GetString()));
    }

    [Fact]
    public async Task Uncovered_locale_falls_back_to_the_neutral_shipping_title()
    {
        using var provider = BuildProvider();
        var factory = provider.GetRequiredService<ApiResponseFactory>();
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        httpContext.Request.Path = "/v1/admin/shipping-services/x/rates";
        httpContext.Request.Headers.AcceptLanguage = "de-DE,en;q=0.5";
        httpContext.Response.Body = new MemoryStream();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        var result = factory.FromFailure(new SemanticError(ShippingErrorCodes.ScheduleWindowOverlap));
        await result.ExecuteAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(httpContext.Response.Body);
        Assert.Equal(
            ShippingErrorResources.Manager.GetString(ShippingErrorCodes.ScheduleWindowOverlap, English),
            body.RootElement.GetProperty("title").GetString());
        Assert.Equal(ShippingErrorCodes.ScheduleWindowOverlap, body.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Validation_failure_keeps_the_validation_envelope_while_shipping_codes_stay_400()
    {
        using var provider = BuildProvider();
        var factory = provider.GetRequiredService<ApiResponseFactory>();
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        httpContext.Request.Path = "/v1/admin/shipping-services/x/rates";
        httpContext.Request.Headers.AcceptLanguage = "en";
        httpContext.Response.Body = new MemoryStream();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        var validation = new FluentValidation.ValidationException(
        [
            new FluentValidation.Results.ValidationFailure("body", ShippingErrorCodes.ChargeAmountNegative)
            {
                ErrorCode = ShippingErrorCodes.ChargeAmountNegative,
            },
        ]);

        var result = factory.FromException(validation);
        await result.ExecuteAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(httpContext.Response.Body);
        var root = body.RootElement;

        // The transport validation envelope is untouched: validation.failed plus per-field codes.
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        Assert.Equal("validation.failed", root.GetProperty("errorCode").GetString());
        Assert.Equal(
            ShippingErrorCodes.ChargeAmountNegative,
            root.GetProperty("errors").GetProperty("body")[0].GetString());

        // The Shipping semantic code itself stays a 400 with its own stable code.
        Assert.Equal(
            StatusCodes.Status400BadRequest,
            new SafeErrorMapper(provider.GetRequiredService<IErrorDefinitionCatalog>())
                .Map(new SemanticException(new SemanticError(ShippingErrorCodes.ChargeAmountNegative)))
                .StatusCode);
    }

    [Fact]
    public void Production_composition_registers_the_catalog_and_resources_once()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
        services.AddToobaObservabilityFoundation();
        services.AddShippingEndpointPresentation();

        var contributors = Assert.Single(
            services,
            d => d.ServiceType == typeof(IErrorCatalogContributor)
                 && d.ImplementationType == typeof(ShippingErrorCatalogContributor));
        Assert.Equal(ServiceLifetime.Singleton, contributors.Lifetime);

        var resources = Assert.Single(
            services,
            d => d.ServiceType == typeof(IErrorResourceSet)
                 && d.ImplementationType == typeof(ShippingErrorResourceSet));
        Assert.Equal(ServiceLifetime.Singleton, resources.Lifetime);

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IErrorDefinitionCatalog>();
        foreach (var code in DeclaredCodes())
        {
            Assert.Contains(code, catalog.RegisteredCodes);
        }

        // Host composition path: exactly one call each, next to the existing module registrations.
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Equal(1, CountOccurrences(program, "builder.Services.AddShippingEndpointPresentation();"));
        Assert.Equal(1, CountOccurrences(program, "app.MapShippingEndpoints();"));

        // Nothing else in production code registers the Shipping contributor/resource set.
        var regressions = Directory
            .EnumerateFiles(Path.Combine(RepoRoot(), "src", "backend"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.Contains("Tests", StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path) is var text
                           && (text.Contains("IErrorCatalogContributor, ShippingErrorCatalogContributor", StringComparison.Ordinal)
                               || text.Contains("IErrorResourceSet, ShippingErrorResourceSet", StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(RepoRoot(), path).Replace('\\', '/'))
            .ToArray();
        Assert.Equal(
            new[] { "src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/ShippingEndpointModule.cs" },
            regressions);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
        services.AddToobaObservabilityFoundation();
        services.AddShippingEndpointPresentation();
        var provider = services.BuildServiceProvider();
        return provider;
    }

    private static string[] DeclaredCodes() =>
        typeof(ShippingErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

    private static string[] RegisteredCodes() =>
        new ShippingErrorCatalogContributor().Contribute().Select(d => d.Code).ToArray();

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tooba.Shipping.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
