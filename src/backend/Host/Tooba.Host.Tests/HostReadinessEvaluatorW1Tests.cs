using System.Reflection;
using MassTransit;
using Tooba.AccessControl.Contracts.Readiness;
using Tooba.BuildingBlocks;
using Tooba.Host.Configuration;
using Tooba.Host.Health;
using Tooba.Host.Messaging;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-TMAR-HOST-HEALTH-AMC-001-W1 — focused readiness evaluator behavior (hygiene, not CERT).
/// </summary>
public sealed class HostReadinessEvaluatorW1Tests
{
    private const string SecretReference = "tenant-secret-ref-token";

    [Fact]
    public async Task Missing_connection_reference_is_generic_and_excludes_actual_token()
    {
        var evaluation = await HostReadinessEvaluator.EvaluateAsync(
            CreateRegistry(includeTenantReference: true),
            CreatePlatform(includeConnection: false),
            new MessagingHostOptions { Enabled = false },
            new StubAuth(ready: true, "disabled"),
            Array.Empty<IBusControl>());

        Assert.False(evaluation.Ready);
        Assert.Equal("missing-reference", evaluation.Checks["postgresql"]);
        Assert.DoesNotContain(SecretReference, string.Join('|', evaluation.Checks.Values), StringComparison.Ordinal);
        Assert.DoesNotContain("missing-reference:", string.Join('|', evaluation.Checks.Values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Messaging_disabled_does_not_require_bus()
    {
        var evaluation = await HostReadinessEvaluator.EvaluateAsync(
            CreateRegistry(includeTenantReference: true),
            CreatePlatform(includeConnection: true),
            new MessagingHostOptions { Enabled = false },
            new StubAuth(ready: true, "disabled"),
            Array.Empty<IBusControl>());

        Assert.True(evaluation.Ready);
        Assert.Equal("disabled", evaluation.Checks["messaging"]);
        Assert.Equal("n/a", evaluation.Checks["messaging-transport"]);
        Assert.False(evaluation.Checks.ContainsKey("messaging-schema"));
    }

    [Fact]
    public async Task Messaging_enabled_with_empty_bus_collection_is_bus_unavailable()
    {
        var evaluation = await HostReadinessEvaluator.EvaluateAsync(
            CreateRegistry(includeTenantReference: true),
            CreatePlatform(includeConnection: true),
            new MessagingHostOptions { Enabled = true, ConnectionReference = SecretReference, Schema = "transport-secret" },
            new StubAuth(ready: true, "disabled"),
            Array.Empty<IBusControl>());

        Assert.False(evaluation.Ready);
        Assert.Equal("bus-unavailable", evaluation.Checks["messaging"]);
        Assert.False(evaluation.Checks.ContainsKey("messaging-schema"));
        Assert.DoesNotContain("transport-secret", string.Join('|', evaluation.Checks.Values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Messaging_enabled_unhealthy_bus_reports_unhealthy()
    {
        var bus = CreateBus(BusHealthResult.Unhealthy("down", new Exception("x"), new Dictionary<string, EndpointHealthResult>()));
        var evaluation = await HostReadinessEvaluator.EvaluateAsync(
            CreateRegistry(includeTenantReference: true),
            CreatePlatform(includeConnection: true),
            new MessagingHostOptions { Enabled = true, ConnectionReference = SecretReference, Schema = "transport-secret" },
            new StubAuth(ready: true, "disabled"),
            new[] { bus });

        Assert.False(evaluation.Ready);
        Assert.Equal("unhealthy", evaluation.Checks["messaging"]);
        Assert.False(evaluation.Checks.ContainsKey("messaging-schema"));
    }

    [Fact]
    public async Task Messaging_ready_omits_schema_and_keeps_safe_transport()
    {
        var bus = CreateBus(BusHealthResult.Healthy("up", new Dictionary<string, EndpointHealthResult>()));
        var evaluation = await HostReadinessEvaluator.EvaluateAsync(
            CreateRegistry(includeTenantReference: true),
            CreatePlatform(includeConnection: true),
            new MessagingHostOptions { Enabled = true, ConnectionReference = SecretReference, Schema = "transport-secret" },
            new StubAuth(ready: true, "disabled"),
            new[] { bus });

        Assert.True(evaluation.Ready);
        Assert.Equal("Healthy", evaluation.Checks["messaging"]);
        Assert.Equal("postgresql-sql", evaluation.Checks["messaging-transport"]);
        Assert.False(evaluation.Checks.ContainsKey("messaging-schema"));
        Assert.DoesNotContain("transport-secret", string.Join('|', evaluation.Checks.Values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authorization_not_ready_preserves_safe_label()
    {
        var evaluation = await HostReadinessEvaluator.EvaluateAsync(
            CreateRegistry(includeTenantReference: true),
            CreatePlatform(includeConnection: true),
            new MessagingHostOptions { Enabled = false },
            new StubAuth(ready: false, "spicedb-endpoint-missing"),
            Array.Empty<IBusControl>());

        Assert.False(evaluation.Ready);
        Assert.Equal("spicedb-endpoint-missing", evaluation.Checks["authorization"]);
    }

    [Fact]
    public async Task Multiple_buses_fail_deterministically_as_bus_unavailable()
    {
        var a = CreateBus(BusHealthResult.Healthy("up", new Dictionary<string, EndpointHealthResult>()));
        var b = CreateBus(BusHealthResult.Healthy("up", new Dictionary<string, EndpointHealthResult>()));
        var evaluation = await HostReadinessEvaluator.EvaluateAsync(
            CreateRegistry(includeTenantReference: true),
            CreatePlatform(includeConnection: true),
            new MessagingHostOptions { Enabled = true, ConnectionReference = SecretReference },
            new StubAuth(ready: true, "disabled"),
            new[] { a, b });

        Assert.False(evaluation.Ready);
        Assert.Equal("bus-unavailable", evaluation.Checks["messaging"]);
    }

    private static ControlPlaneRegistry CreateRegistry(bool includeTenantReference)
    {
        var tenants = new Dictionary<string, TenantRecord>(StringComparer.Ordinal);
        var hosts = new Dictionary<string, TenantRecord>(StringComparer.Ordinal);
        if (includeTenantReference)
        {
            var record = new TenantRecord
            {
                TenantId = new TenantId("store-alpha"),
                Status = TenantStatus.Active,
                ConnectionReference = new ConnectionReference(SecretReference),
                Hosts = ["localhost"],
            };
            tenants["store-alpha"] = record;
            hosts["localhost"] = record;
        }

        return new ControlPlaneRegistry
        {
            Edition = ToobaEdition.SingleStore,
            DeploymentId = "test",
            Hosts = hosts,
            Tenants = tenants,
        };
    }

    private static ToobaPlatformOptions CreatePlatform(bool includeConnection)
    {
        var options = new ToobaPlatformOptions
        {
            Edition = "SingleStore",
            DeploymentId = "test",
            PostgreSQL = new PostgreSqlOptions(),
        };
        if (includeConnection)
            options.PostgreSQL.ConnectionReferences[SecretReference] = "Host=127.0.0.1;Database=tooba_alpha";
        return options;
    }

    private static IBusControl CreateBus(BusHealthResult health)
    {
        var proxy = DispatchProxy.Create<IBusControl, BusHealthProxy>()!;
        ((BusHealthProxy)(object)proxy).Health = health;
        return proxy;
    }

    private sealed class StubAuth(bool ready, string label) : IAuthorizationReadinessProbe
    {
        public Task<AuthorizationReadiness> EvaluateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuthorizationReadiness(ready, label));
    }

    private class BusHealthProxy : DispatchProxy
    {
        public BusHealthResult Health { get; set; } =
            BusHealthResult.Healthy("default", new Dictionary<string, EndpointHealthResult>());

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null)
                throw new InvalidOperationException("missing method");
            if (targetMethod.Name == nameof(IBusControl.CheckHealth))
                return Health;
            throw new NotSupportedException(targetMethod.Name);
        }
    }
}
