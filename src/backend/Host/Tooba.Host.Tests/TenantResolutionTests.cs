using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Tooba.BuildingBlocks;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class HostNormalizerTests
{
    [Theory]
    [InlineData("ALPHA.LOCALHOST", "alpha.localhost")]
    [InlineData("alpha.localhost:443", "alpha.localhost")]
    [InlineData("alpha.localhost:5088", "alpha.localhost")]
    [InlineData("alpha.localhost.", "alpha.localhost")]
    public void Normalizes_host_case_port_and_trailing_dot(string input, string expected)
    {
        Assert.True(HostNormalizer.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void Rejects_empty_host()
    {
        Assert.False(HostNormalizer.TryNormalize(" ", out _));
    }
}

public sealed class PlatformOptionsValidatorTests
{
    [Fact]
    public void Duplicate_host_mapping_is_rejected()
    {
        var options = SampleSingleStore();
        options.SingleStore.Tenants.Add(new TenantRecordOptions
        {
            TenantId = "store-beta",
            ConnectionReference = "Tenant:beta",
            Hosts = ["alpha.localhost"],
        });

        var result = new PlatformOptionsValidator().Validate(null, options);
        Assert.False(result.Succeeded);
        Assert.Contains("Duplicate host", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Duplicate_tenant_id_is_rejected()
    {
        var options = SampleSingleStore();
        options.SingleStore.Tenants.Add(new TenantRecordOptions
        {
            TenantId = "store-alpha",
            ConnectionReference = "Tenant:other",
            Hosts = ["other.localhost"],
        });

        var result = new PlatformOptionsValidator().Validate(null, options);
        Assert.False(result.Succeeded);
        Assert.Contains("Duplicate TenantId", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Invalid_edition_is_rejected()
    {
        var options = new ToobaPlatformOptions { Edition = "Hybrid" };
        var result = new PlatformOptionsValidator().Validate(null, options);
        Assert.False(result.Succeeded);
    }

    private static ToobaPlatformOptions SampleSingleStore() => new()
    {
        Edition = "SingleStore",
        SingleStore = new SingleStoreOptions
        {
            Tenants =
            [
                new TenantRecordOptions
                {
                    TenantId = "store-alpha",
                    ConnectionReference = "Tenant:alpha",
                    Hosts = ["alpha.localhost"],
                },
            ],
        },
    };
}

public sealed class TenantResolutionTests
{
    [Fact]
    public async Task SingleStore_unknown_host_fails_closed()
    {
        await using var factory = new SingleStoreFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/__platform-commerce");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("platform.resolution.failed", json.GetProperty("errorCode").GetString());
        Assert.True(json.TryGetProperty("traceId", out _));
        var body = json.ToString();
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionString", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SingleStore_known_host_resolves_tenant_not_hostname()
    {
        await using var factory = new SingleStoreFactory();
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/__platform-commerce");
        request.Headers.Host = "ALPHA.LOCALHOST:5088";
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SingleStore", json.GetProperty("edition").GetString());
        Assert.Equal("store-alpha", json.GetProperty("tenantId").GetString());
        Assert.Equal("tenant-alpha", json.GetProperty("connectionReference").GetString());
        Assert.Equal("alpha.localhost", json.GetProperty("resolvedHost").GetString());
        Assert.NotEqual("ALPHA.LOCALHOST:5088", json.GetProperty("tenantId").GetString());
    }

    [Fact]
    public async Task SingleStore_disabled_tenant_fails_closed()
    {
        await using var factory = new SingleStoreFactory();
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/__platform-commerce");
        request.Headers.Host = "disabled.localhost";
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("platform.resolution.failed", json.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Tenant_a_never_receives_tenant_b_connection()
    {
        await using var factory = new SingleStoreFactory();
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/__platform-commerce");
        request.Headers.Host = "alpha.localhost";
        var json = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("tenant-alpha", json.GetProperty("connectionReference").GetString());
        Assert.NotEqual("tenant-bravo", json.GetProperty("connectionReference").GetString());
    }

    [Fact]
    public async Task Marketplace_does_not_lookup_host_for_database()
    {
        await using var factory = new MarketplaceFactory();
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/__platform-commerce");
        request.Headers.Host = "unknown.example";
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Marketplace", json.GetProperty("edition").GetString());
        Assert.Equal("marketplace", json.GetProperty("connectionReference").GetString());
        Assert.True(!json.TryGetProperty("tenantId", out var tenantId) || tenantId.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined);
    }

    [Fact]
    public async Task Health_and_ready_skip_tenant_resolution()
    {
        await using var factory = new SingleStoreFactory();
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ready")).StatusCode);
    }

    private sealed class SingleStoreFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Tooba:Edition"] = "SingleStore",
                    ["Tooba:DeploymentId"] = "test-singlestore",
                    ["Tooba:Marketplace:ConnectionReference"] = "",
                    ["Tooba:SingleStore:Tenants:0:TenantId"] = "store-alpha",
                    ["Tooba:SingleStore:Tenants:0:Status"] = "Active",
                    ["Tooba:SingleStore:Tenants:0:ConnectionReference"] = "tenant-alpha",
                    ["Tooba:SingleStore:Tenants:0:Hosts:0"] = "alpha.localhost",
                    ["Tooba:SingleStore:Tenants:1:TenantId"] = "store-bravo",
                    ["Tooba:SingleStore:Tenants:1:Status"] = "Active",
                    ["Tooba:SingleStore:Tenants:1:ConnectionReference"] = "tenant-bravo",
                    ["Tooba:SingleStore:Tenants:1:Hosts:0"] = "bravo.localhost",
                    ["Tooba:SingleStore:Tenants:2:TenantId"] = "store-disabled",
                    ["Tooba:SingleStore:Tenants:2:Status"] = "Disabled",
                    ["Tooba:SingleStore:Tenants:2:ConnectionReference"] = "tenant-disabled",
                    ["Tooba:SingleStore:Tenants:2:Hosts:0"] = "disabled.localhost",
                    ["Tooba:PostgreSQL:ConnectionReferences:tenant-alpha"] = "Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_alpha",
                    ["Tooba:PostgreSQL:ConnectionReferences:tenant-bravo"] = "Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_bravo",
                    ["Tooba:PostgreSQL:ConnectionReferences:tenant-disabled"] = "Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_disabled",
                });
            });
        }
    }

    private sealed class MarketplaceFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Tooba:Edition"] = "Marketplace",
                    ["Tooba:DeploymentId"] = "test-marketplace",
                    ["Tooba:Marketplace:ConnectionReference"] = "marketplace",
                    ["Tooba:PostgreSQL:ConnectionReferences:marketplace"] = "Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_marketplace",
                });
            });
        }
    }
}
