using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قرارداد ProblemDetails پلتفرم: traceId هست، جزئیات پیاده‌سازی و credential نیست.
/// </summary>
public sealed class ErrorContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ErrorContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
    }

    [Fact]
    public async Task Unexpected_exception_returns_problem_details_with_trace_id()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/__platform-error");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(500, json.GetProperty("status").GetInt32());
        Assert.True(json.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));

        var body = json.ToString();
        Assert.DoesNotContain("ConnectionString", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at Tooba.Host", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Mapped_platform_exception_returns_expected_status()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/__platform-conflict");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, json.GetProperty("status").GetInt32());
        Assert.True(json.TryGetProperty("traceId", out _));
        Assert.Equal("platform.conflict", json.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Health_and_ready_remain_available()
    {
        var client = _factory.CreateClient();
        var health = await client.GetAsync("/health");
        var ready = await client.GetAsync("/ready");
        var live = await client.GetAsync("/health/live");
        var readyProbe = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyProbe.StatusCode);
    }
}

/// <summary>
/// نگاشت استثنا باید در حالت غیر Development فیلد Detail را خالی بگذارد.
/// </summary>
public sealed class PlatformExceptionMapperTests
{
    [Fact]
    public void Production_problem_details_omit_implementation_detail()
    {
        var mapped = PlatformExceptionMapper.Map(new InvalidOperationException("secret-path C:\\internal\\sql"));
        var problem = PlatformExceptionMapper.ToProblemDetails(mapped, "abc", developmentDetail: null);
        Assert.Equal(500, problem.Status);
        Assert.Null(problem.Detail);
        Assert.Equal("abc", problem.Extensions["traceId"]?.ToString());
        Assert.False(problem.Extensions.ContainsKey("errorCode"));
    }
}
