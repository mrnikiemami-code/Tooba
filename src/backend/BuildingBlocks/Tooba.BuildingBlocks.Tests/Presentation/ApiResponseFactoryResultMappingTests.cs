using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Results;
using Xunit;

namespace Tooba.BuildingBlocks.Tests.Presentation;

public sealed class ApiResponseFactoryResultMappingTests
{
    [Fact]
    public async Task From_result_success_returns_raw_dto_json()
    {
        using var sp = BuildServices();
        var factory = sp.GetRequiredService<ApiResponseFactory>();
        var result = factory.From(Result.Success(new DemoDto("abc")));
        var http = CreateHttp(sp);
        await result.ExecuteAsync(http);
        Assert.Equal(StatusCodes.Status200OK, http.Response.StatusCode);
        http.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(http.Response.Body);
        Assert.Equal("abc", doc.RootElement.GetProperty("id").GetString());
        Assert.False(doc.RootElement.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task From_result_failure_maps_problem_details_without_throwing()
    {
        using var sp = BuildServices(withCatalog: true);
        var factory = sp.GetRequiredService<ApiResponseFactory>();
        var correlation = sp.GetRequiredService<ICorrelationIdProvider>().EnsureCorrelationId("corr-result-1");
        var accessor = sp.GetRequiredService<IHttpContextAccessor>();
        var http = CreateHttp(sp);
        accessor.HttpContext = http;
        var result = factory.From(Result.Failure<DemoDto>(new SemanticError("demo.not_found")));
        await result.ExecuteAsync(http);
        Assert.Equal(StatusCodes.Status404NotFound, http.Response.StatusCode);
        http.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(http.Response.Body);
        Assert.Equal("demo.not_found", doc.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal(correlation, doc.RootElement.GetProperty("correlationId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Created_success_sets_201_and_location()
    {
        using var sp = BuildServices();
        var factory = sp.GetRequiredService<ApiResponseFactory>();
        var result = factory.Created("/v1/seller/offers/1", Result.Success(new DemoDto("1")));
        var http = CreateHttp(sp);
        await result.ExecuteAsync(http);
        Assert.Equal(StatusCodes.Status201Created, http.Response.StatusCode);
        Assert.Equal("/v1/seller/offers/1", http.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Created_failure_maps_problem_details_without_location()
    {
        using var sp = BuildServices(withCatalog: true);
        var factory = sp.GetRequiredService<ApiResponseFactory>();
        var accessor = sp.GetRequiredService<IHttpContextAccessor>();
        var http = CreateHttp(sp);
        accessor.HttpContext = http;
        var result = factory.Created("/v1/seller/offers/1", Result.Failure<DemoDto>(new SemanticError("demo.conflict")));
        await result.ExecuteAsync(http);
        Assert.Equal(StatusCodes.Status409Conflict, http.Response.StatusCode);
        Assert.True(string.IsNullOrEmpty(http.Response.Headers.Location.ToString()));
    }

    [Fact]
    public async Task Void_result_failure_maps_forbidden()
    {
        using var sp = BuildServices(withCatalog: true);
        var factory = sp.GetRequiredService<ApiResponseFactory>();
        var accessor = sp.GetRequiredService<IHttpContextAccessor>();
        var http = CreateHttp(sp);
        accessor.HttpContext = http;
        var result = factory.From(Result.Failure(new SemanticError("demo.forbidden")));
        await result.ExecuteAsync(http);
        Assert.Equal(StatusCodes.Status403Forbidden, http.Response.StatusCode);
    }

    private static DefaultHttpContext CreateHttp(IServiceProvider sp)
    {
        var http = new DefaultHttpContext
        {
            RequestServices = sp,
        };
        http.Response.Body = new MemoryStream();
        return http;
    }

    private static ServiceProvider BuildServices(bool withCatalog = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new StubHostEnvironment(Environments.Production));
        services.AddHttpContextAccessor();
        services.AddToobaObservabilityFoundation();
        if (withCatalog)
        {
            services.AddSingleton<IErrorCatalogContributor, DemoCatalogContributor>();
            services.AddSingleton<IErrorDefinitionCatalog>(sp =>
                new ErrorDefinitionCatalog(sp.GetServices<IErrorCatalogContributor>()));
        }

        return services.BuildServiceProvider();
    }

    private sealed record DemoDto(string Id);

    private sealed class DemoCatalogContributor : IErrorCatalogContributor
    {
        public IReadOnlyList<ErrorDescriptor> Contribute() =>
        [
            new("demo.not_found", ErrorClassification.NotFound, StatusCodes.Status404NotFound, "demo.not_found", ErrorSeverity.Warning, "Missing."),
            new("demo.conflict", ErrorClassification.Conflict, StatusCodes.Status409Conflict, "demo.conflict", ErrorSeverity.Warning, "Conflict."),
            new("demo.forbidden", ErrorClassification.Forbidden, StatusCodes.Status403Forbidden, "demo.forbidden", ErrorSeverity.Warning, "Forbidden."),
            new("demo.bad", ErrorClassification.Validation, StatusCodes.Status400BadRequest, "demo.bad", ErrorSeverity.Warning, "Bad."),
        ];
    }

    private sealed class StubHostEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
