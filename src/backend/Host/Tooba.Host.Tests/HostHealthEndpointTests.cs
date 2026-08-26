using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// liveness/readiness endpoints باید بدون tenant resolution در دسترس باشند.
/// </summary>
public sealed class HostHealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HostHealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Tooba:Edition"] = "SingleStore",
                    ["Tooba:Messaging:Enabled"] = "false",
                    ["Tooba:SingleStore:Tenants:0:TenantId"] = "store-alpha",
                    ["Tooba:SingleStore:Tenants:0:ConnectionReference"] = "tenant-alpha",
                    ["Tooba:SingleStore:Tenants:0:Hosts:0"] = "localhost",
                    ["Tooba:PostgreSQL:ConnectionReferences:tenant-alpha"] = "Host=127.0.0.1;Database=tooba_alpha",
                });
            });
        });
    }

    [Fact]
    public async Task Live_and_legacy_health_return_ok()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task Ready_and_legacy_ready_return_ok_in_development()
    {
        var client = _factory.CreateClient();
        var ready = await client.GetAsync("/health/ready");
        var legacy = await client.GetAsync("/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
    }
}
