using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Tooba.BuildingBlocks.Observability.Correlation;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>HTTP correlation + ProblemDetails join for R2.</summary>
public sealed class CorrelationRuntimeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CorrelationRuntimeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Tooba:Messaging:Enabled"] = "false",
                    ["Tooba:Outbox:Enabled"] = "false",
                });
            });
        });
    }

    [Fact]
    public async Task Valid_incoming_correlation_is_preserved_on_response_and_problem_details()
    {
        var incoming = Guid.NewGuid();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(CorrelationIdConstants.HeaderName, incoming.ToString("D"));

        var response = await client.GetAsync("/__platform-error");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(CorrelationIdConstants.HeaderName, out var values));
        var header = Assert.Single(values);
        Assert.Equal(incoming.ToString("N"), header);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(header, json.GetProperty("correlationId").GetString());
        Assert.True(json.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
        var body = json.ToString();
        Assert.DoesNotContain("platform-bootstrap-test", body, StringComparison.Ordinal);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_incoming_correlation_is_replaced()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(CorrelationIdConstants.HeaderName, "not-a-guid");
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(CorrelationIdConstants.HeaderName, out var values));
        var header = Assert.Single(values);
        Assert.True(Guid.TryParse(header, out _));
        Assert.NotEqual("not-a-guid", header);
    }

    [Fact]
    public async Task Semantic_platform_conflict_joins_correlation_and_trace()
    {
        var incoming = Guid.NewGuid().ToString("N");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(CorrelationIdConstants.HeaderName, incoming);
        var response = await client.GetAsync("/__platform-conflict");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(incoming, json.GetProperty("correlationId").GetString());
        Assert.True(json.TryGetProperty("traceId", out _));
        Assert.Equal("platform.conflict", json.GetProperty("errorCode").GetString());
    }
}
