using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Logging;
using Tooba.Host;
using Tooba.Host.Observability;
using Tooba.Identity.Contracts;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1 — focused enrichment scope behavior.
/// </summary>
public sealed class RequestObservabilityEnrichmentMiddlewareTests
{
    [Fact]
    public async Task Enrichment_scope_includes_canonical_fields_without_client_ip_or_query()
    {
        var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var tenant = "store-tenant-1";
        var scopes = new List<IReadOnlyDictionary<string, object>>();
        var logger = new CapturingLogger(scopes);
        var correlation = new FixedCorrelation("corr-abc-001");
        var commerce = new StubCommerce(SingleStoreCommerce(tenant));
        var session = new CurrentAuthenticatedSession();
        session.Assign(new AuthenticatedIdentity(userId, Guid.NewGuid(), "SingleStore", tenant));

        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-req-9";
        context.Request.Method = "GET";
        context.Request.Path = "/v1/catalog/items";
        context.Request.QueryString = new QueryString("?secret=token&q=1");
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.50");

        Exception? nextError = null;
        var mw = new RequestObservabilityEnrichmentMiddleware(_ =>
        {
            Assert.Single(scopes);
            return Task.CompletedTask;
        }, logger);

        await mw.InvokeAsync(context, correlation, commerce, session);

        Assert.Null(nextError);
        Assert.Single(scopes);
        var scope = scopes[0];
        Assert.Equal("corr-abc-001", scope[ObservabilityLogScopeKeys.CorrelationId]);
        Assert.Equal("trace-req-9", scope[ObservabilityLogScopeKeys.RequestId]);
        Assert.Equal(tenant, scope[ObservabilityLogScopeKeys.TenantId]);
        Assert.Equal(tenant, scope[ObservabilityLogScopeKeys.StoreId]);
        Assert.Equal(userId.ToString("N"), scope[ObservabilityLogScopeKeys.ActorId]);
        Assert.Equal("GET", scope[ObservabilityLogScopeKeys.HttpMethod]);
        Assert.Equal("/v1/catalog/items", scope[ObservabilityLogScopeKeys.HttpPath]);
        Assert.False(scope.ContainsKey(ObservabilityLogScopeKeys.ClientIp));
        Assert.DoesNotContain("secret", string.Join('|', scope.Values.Select(v => v?.ToString() ?? string.Empty)));
        Assert.DoesNotContain("203.0.113.50", string.Join('|', scope.Values.Select(v => v?.ToString() ?? string.Empty)));
    }

    [Fact]
    public async Task Marketplace_null_tenant_omits_tenant_and_store()
    {
        var scopes = new List<IReadOnlyDictionary<string, object>>();
        var mw = new RequestObservabilityEnrichmentMiddleware(_ => Task.CompletedTask, new CapturingLogger(scopes));
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/v1/x";

        await mw.InvokeAsync(
            context,
            new FixedCorrelation("corr-mkt"),
            new StubCommerce(MarketplaceCommerce()),
            new CurrentAuthenticatedSession());

        Assert.Single(scopes);
        Assert.False(scopes[0].ContainsKey(ObservabilityLogScopeKeys.TenantId));
        Assert.False(scopes[0].ContainsKey(ObservabilityLogScopeKeys.StoreId));
        Assert.False(scopes[0].ContainsKey(ObservabilityLogScopeKeys.ActorId));
        Assert.False(scopes[0].ContainsKey(ObservabilityLogScopeKeys.ClientIp));
    }

    [Fact]
    public async Task Anonymous_session_omits_actor_authenticated_uses_guid_n()
    {
        var scopes = new List<IReadOnlyDictionary<string, object>>();
        var logger = new CapturingLogger(scopes);
        var correlation = new FixedCorrelation("corr-actor");
        var commerce = new StubCommerce(null);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/anon";

        await new RequestObservabilityEnrichmentMiddleware(_ => Task.CompletedTask, logger)
            .InvokeAsync(context, correlation, commerce, new CurrentAuthenticatedSession());
        Assert.False(scopes[0].ContainsKey(ObservabilityLogScopeKeys.ActorId));

        scopes.Clear();
        var userId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var session = new CurrentAuthenticatedSession();
        session.Assign(new AuthenticatedIdentity(userId, Guid.NewGuid(), "Marketplace", null));
        await new RequestObservabilityEnrichmentMiddleware(_ => Task.CompletedTask, logger)
            .InvokeAsync(context, correlation, commerce, session);
        Assert.Equal(userId.ToString("N"), scopes[0][ObservabilityLogScopeKeys.ActorId]);
    }

    [Fact]
    public async Task Exception_from_next_propagates_and_scope_disposes()
    {
        var scopes = new List<IReadOnlyDictionary<string, object>>();
        var logger = new CapturingLogger(scopes);
        var mw = new RequestObservabilityEnrichmentMiddleware(
            _ => throw new InvalidOperationException("boom"),
            logger);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/fail";

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mw.InvokeAsync(
                context,
                new FixedCorrelation("corr-ex"),
                new StubCommerce(null),
                new CurrentAuthenticatedSession()));

        Assert.Single(scopes);
        Assert.True(logger.LastScopeDisposed);
    }

    private static CommerceContext MarketplaceCommerce() =>
        new(
            new EditionContext(ToobaEdition.Marketplace, "dep-1"),
            Tenant: null,
            new ConnectionReference("marketplace"),
            TraceId: "t1");

    private static CommerceContext SingleStoreCommerce(string tenantId) =>
        new(
            new EditionContext(ToobaEdition.SingleStore, "dep-1"),
            new TenantContext(
                new TenantId(tenantId),
                TenantStatus.Active,
                new ConnectionReference("store-db"),
                DisplayName: null,
                ThemeReference: null,
                DefaultMarketReference: null,
                ResolvedHost: "store.local",
                PrimaryDomain: null),
            new ConnectionReference("store-db"),
            TraceId: "t1");

    private sealed class StubCommerce(CommerceContext? current) : ICurrentCommerceContext
    {
        public CommerceContext? Current { get; } = current;
    }

    private sealed class FixedCorrelation(string id) : ICorrelationIdProvider
    {
        public string? GetCorrelationId() => id;

        public Guid? GetCorrelationGuid() => null;

        public string EnsureCorrelationId(string? incoming = null) => id;

        public void SetCorrelationId(string correlationId)
        {
        }
    }

    private sealed class CapturingLogger(List<IReadOnlyDictionary<string, object>> scopes) : ILogger<RequestObservabilityEnrichmentMiddleware>
    {
        public bool LastScopeDisposed { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object>> pairs)
            {
                scopes.Add(pairs.ToDictionary(p => p.Key, p => p.Value));
            }
            else if (state is IReadOnlyDictionary<string, object> dict)
            {
                scopes.Add(dict);
            }

            return new ScopeHandle(() => LastScopeDisposed = true);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class ScopeHandle(Action onDispose) : IDisposable
        {
            public void Dispose() => onDispose();
        }
    }
}
