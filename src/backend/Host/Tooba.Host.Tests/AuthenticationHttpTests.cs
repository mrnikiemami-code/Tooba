using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Identity.Application;
using Tooba.Identity.Infrastructure;
using Tooba.Identity.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش HTTP احراز: نشست مات، enumeration-safe، چرخش Refresh، isolation Tenant، و عدم نشت راز.
/// </summary>
[Collection("PostgresSerial")]
public sealed class AuthenticationHttpTests : IAsyncLifetime
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
                .WithDatabase("tooba_auth_http_a")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
            _alphaCs = _container.GetConnectionString();
            await using var admin = new NpgsqlConnection(_alphaCs);
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand("CREATE DATABASE tooba_auth_http_b", admin);
            await create.ExecuteNonQueryAsync();
            _bravoCs = new NpgsqlConnectionStringBuilder(_alphaCs) { Database = "tooba_auth_http_b" }.ConnectionString;
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
    public async Task Authentication_http_boundary_covers_session_reset_tenant_and_secrets()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var logs = new ListLoggerProvider();
        await using var factory = new AuthHttpFactory(_alphaCs, _bravoCs, logs);
        await EnsureIdentityAsync(factory, "store-alpha", "tenant-alpha");
        await EnsureIdentityAsync(factory, "store-bravo", "tenant-bravo");
        var client = factory.CreateClient();
        const string password = "correct-horse";

        var register = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/register", new
        {
            identifierKind = "Email",
            identifier = "auth-http@example.com",
            password,
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var registered = await ReadAsync(register);
        var userId = registered.GetProperty("userId").GetGuid();

        var duplicate = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/register", new
        {
            identifierKind = "Email",
            identifier = "AUTH-HTTP@example.com",
            password,
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var duplicateBody = await duplicate.Content.ReadAsStringAsync();
        Assert.Equal("identity.identifier.conflict", ErrorCode(duplicateBody));
        Assert.DoesNotContain(password, duplicateBody, StringComparison.Ordinal);

        var unknownLogin = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/login", new
        {
            identifierKind = "Email",
            identifier = "missing@example.com",
            password,
        });
        var badPassword = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/login", new
        {
            identifierKind = "Email",
            identifier = "auth-http@example.com",
            password = "wrong-password-value",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, unknownLogin.StatusCode);
        Assert.Equal(unknownLogin.StatusCode, badPassword.StatusCode);
        var unknownBody = await unknownLogin.Content.ReadAsStringAsync();
        var badPasswordBody = await badPassword.Content.ReadAsStringAsync();
        Assert.Equal("identity.authentication.failed", ErrorCode(unknownBody));
        Assert.Equal(ErrorCode(unknownBody), ErrorCode(badPasswordBody));
        Assert.DoesNotContain("not found", unknownBody, StringComparison.OrdinalIgnoreCase);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICommerceContextAssigner>()
                .Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
            await scope.ServiceProvider.GetRequiredService<IIdentityAuthenticationService>()
                .DisableAsync(userId, CancellationToken.None);
        }

        var disabledLogin = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/login", new
        {
            identifierKind = "Email",
            identifier = "auth-http@example.com",
            password,
        });
        Assert.Equal(HttpStatusCode.Unauthorized, disabledLogin.StatusCode);
        Assert.Equal("identity.authentication.failed", ErrorCode(await disabledLogin.Content.ReadAsStringAsync()));

        var liveRegister = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/register", new
        {
            identifierKind = "Email",
            identifier = "rotate@example.com",
            password,
        });
        liveRegister.EnsureSuccessStatusCode();
        var login1 = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/login", new
        {
            identifierKind = "Email",
            identifier = "rotate@example.com",
            password,
        });
        var session1 = await ReadAsync(login1);
        var access1 = session1.GetProperty("accessToken").GetString()!;
        var refresh1 = session1.GetProperty("refreshToken").GetString()!;
        var sessionId1 = session1.GetProperty("sessionId").GetGuid();
        Assert.Equal(sessionId1.ToString("D"), access1);

        var me = await SendAsync(client, "alpha.localhost", HttpMethod.Get, "/v1/auth/me", bearer: access1);
        me.EnsureSuccessStatusCode();
        var meJson = await ReadAsync(me);
        Assert.Equal(sessionId1, meJson.GetProperty("sessionId").GetGuid());
        Assert.False(meJson.TryGetProperty("refreshToken", out _));
        Assert.False(meJson.TryGetProperty("securityStamp", out _));

        var rotated = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/refresh", new
        {
            sessionId = sessionId1,
            refreshToken = refresh1,
        });
        rotated.EnsureSuccessStatusCode();
        var session2 = await ReadAsync(rotated);
        var refresh2 = session2.GetProperty("refreshToken").GetString()!;
        Assert.NotEqual(refresh1, refresh2);
        var replay = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/refresh", new
        {
            sessionId = sessionId1,
            refreshToken = refresh1,
        });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        var replayBody = await replay.Content.ReadAsStringAsync();
        Assert.Equal("identity.session.invalid", ErrorCode(replayBody));
        Assert.DoesNotContain("reuse", replayBody, StringComparison.OrdinalIgnoreCase);

        var login2 = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/login", new
        {
            identifierKind = "Email",
            identifier = "rotate@example.com",
            password,
        });
        var access2 = (await ReadAsync(login2)).GetProperty("accessToken").GetString()!;
        var logout = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/logout", bearer: access1);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/logout", bearer: access1)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(client, "alpha.localhost", HttpMethod.Get, "/v1/auth/me", bearer: access1)).StatusCode);
        (await SendAsync(client, "alpha.localhost", HttpMethod.Get, "/v1/auth/me", bearer: access2)).EnsureSuccessStatusCode();
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/logout-all", bearer: access2)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(client, "alpha.localhost", HttpMethod.Get, "/v1/auth/me", bearer: access2)).StatusCode);

        var unknownReset = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/password-reset/request", new
        {
            identifierKind = "Email",
            identifier = "nobody@example.com",
        });
        var knownReset = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/password-reset/request", new
        {
            identifierKind = "Email",
            identifier = "rotate@example.com",
        });
        Assert.Equal(HttpStatusCode.OK, unknownReset.StatusCode);
        Assert.Equal(unknownReset.StatusCode, knownReset.StatusCode);
        var unknownResetJson = await ReadAsync(unknownReset);
        var knownResetJson = await ReadAsync(knownReset);
        Assert.True(unknownResetJson.GetProperty("accepted").GetBoolean());
        Assert.True(knownResetJson.GetProperty("accepted").GetBoolean());
        Assert.False(unknownResetJson.TryGetProperty("challengeId", out _));
        Assert.False(knownResetJson.TryGetProperty("challengeId", out _));

        var sender = factory.Services.GetRequiredService<CapturingOtpDeliveryProvider>();
        var resetSecret = sender.LastCode!;
        Guid resetChallengeId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICommerceContextAssigner>()
                .Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            resetChallengeId = await db.Challenges.AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.ChallengeId)
                .FirstAsync();
        }

        var completeReset = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/password-reset/complete", new
        {
            challengeId = resetChallengeId,
            secret = resetSecret,
            newPassword = "reset-horse-1",
        });
        Assert.Equal(HttpStatusCode.NoContent, completeReset.StatusCode);
        var reuseReset = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/password-reset/complete", new
        {
            challengeId = resetChallengeId,
            secret = resetSecret,
            newPassword = "reset-horse-2",
        });
        Assert.Equal(HttpStatusCode.BadRequest, reuseReset.StatusCode);
        Assert.Equal("identity.challenge.invalid", ErrorCode(await reuseReset.Content.ReadAsStringAsync()));

        var afterReset = await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/login", new
        {
            identifierKind = "Email",
            identifier = "rotate@example.com",
            password = "reset-horse-1",
        });
        afterReset.EnsureSuccessStatusCode();
        var verifyAccess = (await ReadAsync(afterReset)).GetProperty("accessToken").GetString()!;

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/password-change", new
            {
                currentPassword = "reset-horse-1",
                newPassword = "changed-horse",
            })).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/password-change", new
            {
                currentPassword = "not-the-current",
                newPassword = "changed-horse",
            }, bearer: verifyAccess)).StatusCode);

        (await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/identifier-verification/request", new
        {
            identifierKind = "Email",
            identifier = "rotate@example.com",
        }, bearer: verifyAccess)).EnsureSuccessStatusCode();
        var verifySecret = sender.LastCode!;
        Guid verifyChallengeId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICommerceContextAssigner>()
                .Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            verifyChallengeId = await db.Challenges.AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.ChallengeId)
                .FirstAsync();
        }

        Assert.Equal(
            "identity.challenge.invalid",
            ErrorCode(await (await SendAsync(
                client,
                "alpha.localhost",
                HttpMethod.Post,
                "/v1/auth/identifier-verification/complete",
                new { challengeId = verifyChallengeId, secret = "000000" })).Content.ReadAsStringAsync()));
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/identifier-verification/complete", new
            {
                challengeId = verifyChallengeId,
                secret = verifySecret,
            })).StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await SendAsync(client, "alpha.localhost", HttpMethod.Post, "/v1/auth/password-change", new
            {
                currentPassword = "reset-horse-1",
                newPassword = "changed-horse",
            }, bearer: verifyAccess)).StatusCode);

        (await SendAsync(client, "bravo.localhost", HttpMethod.Post, "/v1/auth/register", new
        {
            identifierKind = "Email",
            identifier = "bravo@example.com",
            password,
        })).EnsureSuccessStatusCode();
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await SendAsync(client, "bravo.localhost", HttpMethod.Get, "/v1/auth/me", bearer: verifyAccess)).StatusCode);

        Assert.Equal(
            "identity.tenant.untrusted",
            ErrorCode(await (await SendAsync(
                client,
                "alpha.localhost",
                HttpMethod.Post,
                "/v1/auth/login",
                new { identifierKind = "Email", identifier = "x@example.com", password },
                extraHeaders: [("X-Tenant-Id", "store-bravo")])).Content.ReadAsStringAsync()));
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(
            client,
            "alpha.localhost",
            HttpMethod.Post,
            "/v1/auth/login?tenantId=store-bravo",
            new { identifierKind = "Email", identifier = "x@example.com", password })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(
            client,
            "alpha.localhost",
            HttpMethod.Post,
            "/v1/auth/login",
            new { identifierKind = "Email", identifier = "x@example.com", password, tenantId = "store-bravo" })).StatusCode);
        var cookie = new HttpRequestMessage(HttpMethod.Post, "/v1/auth/login")
        {
            Content = JsonContent.Create(new { identifierKind = "Email", identifier = "x@example.com", password }),
        };
        cookie.Headers.Host = "alpha.localhost";
        cookie.Headers.Add("Cookie", "tenantId=store-bravo");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(cookie)).StatusCode);

        var joinedLogs = string.Join('\n', logs.Messages);
        Assert.DoesNotContain(password, joinedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain("reset-horse-1", joinedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain(refresh1, joinedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain(refresh2, joinedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain(resetSecret, joinedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain(verifySecret, joinedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer " + access1, joinedLogs, StringComparison.Ordinal);
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
        object? body = null,
        string? bearer = null,
        (string Name, string Value)[]? extraHeaders = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Host = host;
        if (bearer is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        if (extraHeaders is not null)
        {
            foreach (var header in extraHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Name, header.Value);
            }
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text).RootElement.Clone();
    }

    private static string? ErrorCode(string body)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        var json = document.RootElement;
        if (json.TryGetProperty("errorCode", out var code) || json.TryGetProperty("errorCode", out code))
        {
            return code.GetString();
        }

        return null;
    }

    /// <summary>
    /// Host تست Single-Store با دو پایگاه و ضبط لاگ.
    /// </summary>
    private sealed class AuthHttpFactory : WebApplicationFactory<Program>
    {
        private readonly string _alpha;
        private readonly string _bravo;
        private readonly ListLoggerProvider _logs;

        public AuthHttpFactory(string alpha, string bravo, ListLoggerProvider logs)
        {
            _alpha = alpha;
            _bravo = bravo;
            _logs = logs;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(_logs);
            });
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Tooba:Edition"] = "SingleStore",
                    ["Tooba:DeploymentId"] = "test-auth-http",
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
                    ["Identity:PasswordPolicy:MinLength"] = "10",
                });
            });
        }
    }

    /// <summary>
    /// جمع‌آوری متن لاگ برای اثبات نبود راز.
    /// </summary>
    private sealed class ListLoggerProvider : ILoggerProvider
    {
        public ConcurrentBag<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new ListLogger(Messages);

        public void Dispose()
        {
        }

        private sealed class ListLogger(ConcurrentBag<string> messages) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                messages.Add(formatter(state, exception));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
