using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Host;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// یکپارچگی واقعی SpiceDB با تصویر قفل‌شده. PASS از InMemory به‌عنوان یکپarچگی گزارش نمی‌شود.
/// </summary>
[Collection("PostgresSerial")]
public sealed class SpiceDbIntegrationTests : IAsyncLifetime
{
    private const string TestPresharedKey = "tooba-spicedb-test-key";
    private IContainer? _container;
    private bool _dockerAvailable;

    /// <summary>
    /// SpiceDB v1.56.0 را در صورت دسترسی Docker بالا می‌آورد.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _container = new ContainerBuilder()
                .WithImage("authzed/spicedb:v1.56.0")
                .WithCommand("serve", "--grpc-preshared-key", TestPresharedKey, "--grpc-addr", ":50051")
                .WithPortBinding(50051, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("grpc server started serving"))
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <summary>
    /// کانتینر را آزاد می‌کند.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Real_spicedb_allows_member_denies_other_tenant_and_fails_closed_when_stopped()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers SpiceDB is not available.");

        var endpoint = $"127.0.0.1:{_container.GetMappedPublicPort(50051)}";
        using var adapter = CreateAdapter(endpoint);
        var schema = new FoundationAuthorizationSchemaProvider();
        await WaitForSchemaAsync(adapter, schema.SchemaText);

        var user = AuthorizationSubject.ForUser(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var tenantA = new AuthorizationResource { Type = AuthorizationObjectTypes.Tenant, Id = "tenant-a" };
        var tenantB = new AuthorizationResource { Type = AuthorizationObjectTypes.Tenant, Id = "tenant-b" };
        var ctxA = new AuthorizationCallContext { TenantId = "tenant-a", Edition = ToobaEdition.SingleStore };
        var ctxB = ctxA with { TenantId = "tenant-b" };

        await adapter.WriteAsync(
            new AuthorizationRelationshipWrite { Subject = user, Resource = tenantA, Relation = AuthorizationRelations.Member },
            CancellationToken.None);

        var allow = await adapter.CanAsync(
            new AuthorizationCheck { Subject = user, Resource = tenantA, Permission = AuthorizationRelations.View, CallContext = ctxA },
            CancellationToken.None);
        var denyOtherTenant = await adapter.CanAsync(
            new AuthorizationCheck { Subject = user, Resource = tenantB, Permission = AuthorizationRelations.View, CallContext = ctxB },
            CancellationToken.None);

        Assert.Equal(AuthorizationDecisionKind.Allow, allow.Kind);
        Assert.Equal(AuthorizationDecisionKind.Deny, denyOtherTenant.Kind);

        var party = new AuthorizationResource
        {
            Type = AuthorizationObjectTypes.Party,
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222").ToString("D"),
        };
        await adapter.WriteAsync(
            new AuthorizationRelationshipWrite { Subject = user, Resource = party, Relation = AuthorizationRelations.Member },
            CancellationToken.None);
        var partyAllow = await adapter.CanAsync(
            new AuthorizationCheck { Subject = user, Resource = party, Permission = AuthorizationRelations.View, CallContext = ctxA },
            CancellationToken.None);
        var stranger = AuthorizationSubject.ForUser(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var partyDeny = await adapter.CanAsync(
            new AuthorizationCheck { Subject = stranger, Resource = party, Permission = AuthorizationRelations.View, CallContext = ctxA },
            CancellationToken.None);
        Assert.Equal(AuthorizationDecisionKind.Allow, partyAllow.Kind);
        Assert.Equal(AuthorizationDecisionKind.Deny, partyDeny.Kind);

        using var deadAdapter = CreateAdapter("127.0.0.1:1");
        var unavailable = await deadAdapter.CanAsync(
            new AuthorizationCheck { Subject = user, Resource = tenantA, Permission = AuthorizationRelations.View, CallContext = ctxA },
            CancellationToken.None);
        Assert.Equal(AuthorizationDecisionKind.Unavailable, unavailable.Kind);
        Assert.False(unavailable.IsAllow);
    }

    [SkippableFact]
    public async Task Revoke_removes_access_and_duplicate_touch_is_idempotent()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers SpiceDB is not available.");

        var endpoint = $"127.0.0.1:{_container.GetMappedPublicPort(50051)}";
        using var adapter = CreateAdapter(endpoint);
        await WaitForSchemaAsync(adapter, new FoundationAuthorizationSchemaProvider().SchemaText);

        var user = AuthorizationSubject.ForUser(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        var tenant = new AuthorizationResource { Type = AuthorizationObjectTypes.Tenant, Id = "tenant-revoke" };
        var ctx = new AuthorizationCallContext { TenantId = "tenant-revoke", Edition = ToobaEdition.SingleStore };
        var write = new AuthorizationRelationshipWrite { Subject = user, Resource = tenant, Relation = AuthorizationRelations.Member };

        await adapter.WriteAsync(write, CancellationToken.None);
        await adapter.WriteAsync(write, CancellationToken.None);
        var allowed = await adapter.CanAsync(
            new AuthorizationCheck { Subject = user, Resource = tenant, Permission = AuthorizationRelations.View, CallContext = ctx },
            CancellationToken.None);
        Assert.Equal(AuthorizationDecisionKind.Allow, allowed.Kind);

        await adapter.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = write.Subject,
                Resource = write.Resource,
                Relation = write.Relation,
                Operation = AuthorizationRelationshipOperation.Delete,
            },
            CancellationToken.None);
        var denied = await adapter.CanAsync(
            new AuthorizationCheck { Subject = user, Resource = tenant, Permission = AuthorizationRelations.View, CallContext = ctx },
            CancellationToken.None);
        Assert.Equal(AuthorizationDecisionKind.Deny, denied.Kind);
    }

    [SkippableFact]
    public async Task Readiness_probe_succeeds_when_spicedb_is_up()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers SpiceDB is not available.");

        var endpoint = $"127.0.0.1:{_container.GetMappedPublicPort(50051)}";
        using var adapter = CreateAdapter(endpoint);
        await WaitForSchemaAsync(adapter, new FoundationAuthorizationSchemaProvider().SchemaText);
        using var probe = new SpiceDbHealthProbe(Options.Create(new AuthorizationHostOptions
        {
            Mode = "SpiceDb",
            SpiceDb = new SpiceDbHostOptions
            {
                Endpoint = endpoint,
                Token = TestPresharedKey,
                UseTls = false,
                TimeoutSeconds = 5,
                ReadinessProbeEnabled = true,
            },
        }));

        var ok = await probe.CheckAsync(CancellationToken.None);
        Assert.True(ok);
    }

    [SkippableFact]
    public async Task Readiness_probe_fails_when_spicedb_endpoint_is_unreachable()
    {
        Skip.If(!_dockerAvailable, "Docker/Testcontainers SpiceDB is not available.");

        using var probe = new SpiceDbHealthProbe(Options.Create(new AuthorizationHostOptions
        {
            Mode = "SpiceDb",
            SpiceDb = new SpiceDbHostOptions
            {
                Endpoint = "127.0.0.1:59999",
                Token = TestPresharedKey,
                UseTls = false,
                TimeoutSeconds = 2,
                ReadinessProbeEnabled = true,
            },
        }));

        var ok = await probe.CheckAsync(CancellationToken.None);
        Assert.False(ok);
    }

    [Fact]
    public void Production_rejects_insecure_tls_and_inmemory_mode()
    {
        var validator = new AuthorizationOptionsValidator(new FakeHostEnvironment { EnvironmentName = Environments.Production });
        var inMemory = validator.Validate(null, new AuthorizationHostOptions { Mode = "InMemory" });
        var noTls = validator.Validate(null, new AuthorizationHostOptions
        {
            Mode = "SpiceDb",
            SpiceDb = new SpiceDbHostOptions
            {
                Endpoint = "spicedb.prod.example:443",
                Token = "secret",
                UseTls = false,
            },
        });

        Assert.True(inMemory.Failed);
        Assert.True(noTls.Failed);
    }

    private static SpiceDbAuthorizationAdapter CreateAdapter(string endpoint) =>
        new(
            Options.Create(new AuthorizationHostOptions
            {
                Mode = "SpiceDb",
                ApplySchemaOnStartup = true,
                SpiceDb = new SpiceDbHostOptions
                {
                    Endpoint = endpoint,
                    Token = TestPresharedKey,
                    UseTls = false,
                    TimeoutSeconds = 8,
                    RetryMaxAttempts = 3,
                    RetryBaseDelayMilliseconds = 50,
                },
            }),
            new AuthorizationInstrumentation(),
            new InMemoryAuthorizationSecurityEventSink(),
            NullLogger<SpiceDbAuthorizationAdapter>.Instance);

    private static async Task WaitForSchemaAsync(SpiceDbAuthorizationAdapter adapter, string schemaText)
    {
        Exception? last = null;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                await adapter.WriteSchemaAsync(schemaText, CancellationToken.None);
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(200);
            }
        }

        throw new InvalidOperationException("SpiceDB schema write did not become ready.", last);
    }
}
