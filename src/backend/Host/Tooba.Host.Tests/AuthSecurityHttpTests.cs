using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Identity.Application;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure;
using Tooba.Identity.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// اثبات محدودسازی نرخ، هدرهای امنیتی، CORS و OTP Production fail-closed.
/// </summary>
[Collection("PostgresSerial")]
public sealed class AuthSecurityHttpTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;
    private string _alphaCs = "";
    private string _bravoCs = "";

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_auth_sec_a")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
            _alphaCs = _container.GetConnectionString();
            await using var admin = new NpgsqlConnection(_alphaCs);
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand("CREATE DATABASE tooba_auth_sec_b", admin);
            await create.ExecuteNonQueryAsync();
            _bravoCs = new NpgsqlConnectionStringBuilder(_alphaCs) { Database = "tooba_auth_sec_b" }.ConnectionString;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Login_rate_limit_returns_429_with_stable_error_code()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var factory = new AuthSecurityFactory(_alphaCs, _bravoCs, limit: 2);
        await EnsureIdentityAsync(factory, "store-alpha", "tenant-alpha");
        using var client = factory.CreateClient();
        var body = JsonContent.Create(new
        {
            identifierKind = "Email",
            identifier = "nobody@example.com",
            password = "wrong-password-1",
        });

        for (var i = 0; i < 2; i++)
        {
            var attempt = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/login", body);
            Assert.Equal(HttpStatusCode.Unauthorized, attempt.StatusCode);
        }

        var throttled = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/login", body);
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.Equal("identity.rate_limited", await ErrorCodeAsync(throttled));
    }

    [SkippableFact]
    public async Task Health_live_includes_security_headers()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var factory = new AuthSecurityFactory(_alphaCs, _bravoCs, limit: 30);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.EnsureSuccessStatusCode();
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("SAMEORIGIN", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [SkippableFact]
    public async Task Cors_allows_configured_origin_on_simple_request()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var factory = new AuthSecurityFactory(_alphaCs, _bravoCs, limit: 30, corsOrigin: "http://test-origin.local");
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", "http://test-origin.local");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        Assert.Equal("http://test-origin.local", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Production_otp_sender_is_fail_closed()
    {
        var sender = new ProductionOtpSender();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync(OtpPurpose.PasswordReset, "user@example.com", "123456", CancellationToken.None));
        Assert.Equal("identity.otp.delivery.unconfigured", ex.Message);
    }

    private static async Task EnsureIdentityAsync(WebApplicationFactory<Program> factory, string tenantId, string connectionReference)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ICommerceContextAssigner>()
            .Assign(OutboxTestContextFactory.SingleStore(tenantId, connectionReference));
        await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.EnsureCreatedAsync();
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        string host,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Host = host;
        return await client.SendAsync(request);
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        return document.RootElement.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }

    private sealed class AuthSecurityFactory : WebApplicationFactory<Program>
    {
        private readonly string _alpha;
        private readonly string _bravo;
        private readonly int _limit;
        private readonly string? _corsOrigin;

        public AuthSecurityFactory(string alpha, string bravo, int limit, string? corsOrigin = null)
        {
            _alpha = alpha;
            _bravo = bravo;
            _limit = limit;
            _corsOrigin = corsOrigin;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var entries = new Dictionary<string, string?>
                {
                    ["Tooba:Edition"] = "SingleStore",
                    ["Tooba:DeploymentId"] = "test-auth-security",
                    ["Tooba:Marketplace:ConnectionReference"] = "",
                    ["Tooba:SingleStore:Tenants:0:TenantId"] = "store-alpha",
                    ["Tooba:SingleStore:Tenants:0:Status"] = "Active",
                    ["Tooba:SingleStore:Tenants:0:ConnectionReference"] = "tenant-alpha",
                    ["Tooba:SingleStore:Tenants:0:Hosts:0"] = "alpha.localhost",
                    ["Tooba:SingleStore:Tenants:1:TenantId"] = "store-bravo",
                    ["Tooba:SingleStore:Tenants:1:Status"] = "Active",
                    ["Tooba:SingleStore:Tenants:1:ConnectionReference"] = "tenant-bravo",
                    ["Tooba:SingleStore:Tenants:1:Hosts:0"] = "bravo.localhost",
                    ["Tooba:PostgreSQL:ConnectionReferences:tenant-alpha"] = _alpha,
                    ["Tooba:PostgreSQL:ConnectionReferences:tenant-bravo"] = _bravo,
                    ["Tooba:Outbox:Enabled"] = "false",
                    ["Tooba:Messaging:Enabled"] = "false",
                    ["Tooba:AuthSecurity:AuthRateLimitPermitLimit"] = _limit.ToString(),
                    ["Tooba:AuthSecurity:AuthRateLimitWindowSeconds"] = "60",
                    ["Tooba:AuthSecurity:EnableSecurityHeaders"] = "true",
                    ["Identity:PasswordPolicy:MinLength"] = "10",
                };
                if (_corsOrigin is not null)
                {
                    entries["Tooba:AuthSecurity:CorsAllowedOrigins:0"] = _corsOrigin;
                }

                config.AddInMemoryCollection(entries);
            });
        }
    }
}
