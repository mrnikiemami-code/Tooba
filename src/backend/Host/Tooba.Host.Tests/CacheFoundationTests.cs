using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Host.Caching;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش ایزولاسیون Tenant/Edition و ابعاد Market/Locale/Currency و single-flight بدون Redis.
/// </summary>
public sealed class CacheFoundationTests
{
    private static readonly CanonicalCacheKeyBuilder Keys = new();

    [Fact]
    public async Task Same_key_and_context_hits_after_set()
    {
        using var fixture = CreateFixture();
        var cache = fixture.Cache;
        var key = TenantKey("a", "sku-1");
        var policy = CachePolicy.Expiring(TimeSpan.FromMinutes(1));
        await cache.SetAsync(key, new CatalogProjection("sku-1", "A"), policy, CancellationToken.None);
        var first = await cache.GetAsync<CatalogProjection>(key, CancellationToken.None);
        var second = await cache.GetAsync<CatalogProjection>(key, CancellationToken.None);
        Assert.Equal("A", first?.Title);
        Assert.Equal("A", second?.Title);
    }

    [Fact]
    public async Task Same_resource_id_is_isolated_across_tenants()
    {
        using var fixture = CreateFixture();
        var cache = fixture.Cache;
        var policy = CachePolicy.Expiring(TimeSpan.FromMinutes(1));
        var tenantA = TenantKey("tenant-a", "sku-1");
        var tenantB = TenantKey("tenant-b", "sku-1");
        await cache.SetAsync(tenantA, new CatalogProjection("sku-1", "A"), policy, CancellationToken.None);
        await cache.SetAsync(tenantB, new CatalogProjection("sku-1", "B"), policy, CancellationToken.None);
        Assert.Equal("A", (await cache.GetAsync<CatalogProjection>(tenantA, CancellationToken.None))?.Title);
        Assert.Equal("B", (await cache.GetAsync<CatalogProjection>(tenantB, CancellationToken.None))?.Title);
        Assert.NotEqual(tenantA.Value, tenantB.Value);
    }

    [Fact]
    public void Marketplace_and_single_store_keys_do_not_collide()
    {
        var market = Keys.Build(new CacheKeyParts
        {
            Namespace = "catalog",
            ResourceType = "product",
            ResourceId = "sku-1",
            Edition = ToobaEdition.Marketplace,
            DeploymentId = "dep-1",
        });
        var store = TenantKey("tenant-a", "sku-1");
        Assert.NotEqual(market.Value, store.Value);
        Assert.Contains("marketplace", market.Value, StringComparison.Ordinal);
        Assert.Contains("singlestore", store.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Marketplace_rejects_fake_tenant_id()
    {
        Assert.Throws<InvalidOperationException>(() => Keys.Build(new CacheKeyParts
        {
            Namespace = "catalog",
            ResourceType = "product",
            ResourceId = "sku-1",
            Edition = ToobaEdition.Marketplace,
            DeploymentId = "dep-1",
            TenantId = "should-not-exist",
        }));
    }

    [Fact]
    public void Unset_edition_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => Keys.Build(new CacheKeyParts
        {
            Namespace = "catalog",
            ResourceType = "product",
            ResourceId = "sku-1",
            Edition = ToobaEdition.Unset,
            DeploymentId = "dep-1",
        }));
    }

    [Fact]
    public void Tenant_scoped_requires_singlestore_and_tenant_id()
    {
        Assert.Throws<InvalidOperationException>(() => Keys.Build(new CacheKeyParts
        {
            Namespace = "catalog",
            ResourceType = "product",
            ResourceId = "sku-1",
            Edition = ToobaEdition.Marketplace,
            DeploymentId = "dep-1",
            TenantScoped = true,
        }));

        Assert.Throws<InvalidOperationException>(() => Keys.Build(new CacheKeyParts
        {
            Namespace = "catalog",
            ResourceType = "product",
            ResourceId = "sku-1",
            Edition = ToobaEdition.SingleStore,
            DeploymentId = "dep-1",
            TenantScoped = true,
        }));
    }

    [Fact]
    public void Market_locale_and_currency_are_independent_dimensions()
    {
        var baseline = DimensionKey(market: "ir", locale: "fa-ir", currency: "irr");
        var market = DimensionKey(market: "uk", locale: "fa-ir", currency: "irr");
        var locale = DimensionKey(market: "ir", locale: "en-gb", currency: "irr");
        var currency = DimensionKey(market: "ir", locale: "fa-ir", currency: "gbp");
        Assert.NotEqual(baseline.Value, market.Value);
        Assert.NotEqual(baseline.Value, locale.Value);
        Assert.NotEqual(baseline.Value, currency.Value);
        Assert.NotEqual(market.Value, locale.Value);
        Assert.NotEqual(locale.Value, currency.Value);
    }

    [Fact]
    public async Task GetOrCreate_single_flight_runs_factory_once()
    {
        using var fixture = CreateFixture();
        var cache = fixture.Cache;
        var key = TenantKey("tenant-a", "sku-stampede");
        var policy = CachePolicy.Expiring(TimeSpan.FromMinutes(1));
        var runs = 0;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<CatalogProjection?> Factory(CancellationToken ct)
        {
            Interlocked.Increment(ref runs);
            started.TrySetResult();
            await Task.Delay(200, ct);
            return new CatalogProjection("sku-stampede", "once");
        }

        var first = cache.GetOrCreateAsync(key, Factory, policy, CancellationToken.None);
        await started.Task;
        var rest = Enumerable.Range(0, 15)
            .Select(_ => cache.GetOrCreateAsync(key, Factory, policy, CancellationToken.None));
        var results = await Task.WhenAll(new[] { first }.Concat(rest));
        Assert.Equal(1, runs);
        Assert.All(results, item => Assert.Equal("once", item?.Title));
    }

    [Fact]
    public async Task Failed_factory_is_not_cached()
    {
        using var fixture = CreateFixture();
        var cache = fixture.Cache;
        var key = TenantKey("tenant-a", "sku-fail");
        var policy = CachePolicy.Expiring(TimeSpan.FromMinutes(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cache.GetOrCreateAsync<CatalogProjection>(
                key,
                _ => throw new InvalidOperationException("source-failed"),
                policy,
                CancellationToken.None));
        Assert.Null(await cache.GetAsync<CatalogProjection>(key, CancellationToken.None));
        var recovered = await cache.GetOrCreateAsync(
            key,
            _ => Task.FromResult<CatalogProjection?>(new CatalogProjection("sku-fail", "ok")),
            policy,
            CancellationToken.None);
        Assert.Equal("ok", recovered?.Title);
    }

    [Fact]
    public async Task GetOrCreate_respects_cancellation()
    {
        using var fixture = CreateFixture();
        var cache = fixture.Cache;
        var key = TenantKey("tenant-a", "sku-cancel");
        var policy = CachePolicy.Expiring(TimeSpan.FromMinutes(1));
        using var cts = new CancellationTokenSource();
        var blocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiter = cache.GetOrCreateAsync<CatalogProjection>(
            key,
            async token =>
            {
                blocked.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return new CatalogProjection("sku-cancel", "nope");
            },
            policy,
            cts.Token);
        await blocked.Task;
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);
        Assert.Null(await cache.GetAsync<CatalogProjection>(key, CancellationToken.None));
    }

    [Fact]
    public async Task Cancelled_waiter_does_not_corrupt_inflight_for_other_callers()
    {
        using var fixture = CreateFixture();
        var cache = fixture.Cache;
        var key = TenantKey("tenant-a", "sku-cancel-waiter");
        var policy = CachePolicy.Expiring(TimeSpan.FromMinutes(1));
        using var leaderCts = new CancellationTokenSource();
        using var waiterCts = new CancellationTokenSource();
        var factoryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factoryRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runs = 0;

        var leader = cache.GetOrCreateAsync(
            key,
            async ct =>
            {
                Interlocked.Increment(ref runs);
                factoryStarted.TrySetResult();
                await factoryRelease.Task.WaitAsync(ct);
                return new CatalogProjection("sku-cancel-waiter", "leader");
            },
            policy,
            leaderCts.Token);

        await factoryStarted.Task;

        var waiter = cache.GetOrCreateAsync(
            key,
            _ => Task.FromResult<CatalogProjection?>(new CatalogProjection("sku-cancel-waiter", "waiter")),
            policy,
            waiterCts.Token);

        await Task.Delay(50);
        waiterCts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);

        factoryRelease.TrySetResult();
        var result = await leader;
        Assert.Equal("leader", result?.Title);
        Assert.Equal(1, runs);
        Assert.Equal("leader", (await cache.GetAsync<CatalogProjection>(key, CancellationToken.None))?.Title);
    }

    [Fact]
    public async Task Type_mismatch_removes_entry_and_returns_miss_not_hit_null()
    {
        using var fixture = CreateFixture();
        var cache = fixture.Cache;
        var key = TenantKey("tenant-a", "sku-type");
        var policy = CachePolicy.Expiring(TimeSpan.FromMinutes(1));
        await cache.SetAsync(key, new CatalogProjection("sku-type", "A"), policy, CancellationToken.None);

        var wrong = await cache.GetAsync<OtherProjection>(key, CancellationToken.None);
        Assert.Null(wrong);

        var recovered = await cache.GetAsync<CatalogProjection>(key, CancellationToken.None);
        Assert.Null(recovered);

        await cache.SetAsync(key, new CatalogProjection("sku-type", "B"), policy, CancellationToken.None);
        Assert.Equal("B", (await cache.GetAsync<CatalogProjection>(key, CancellationToken.None))?.Title);
    }

    [Fact]
    public void Zero_and_negative_ttl_are_rejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new CachePolicy(TimeSpan.Zero, null, Array.Empty<string>(), false, null).EnsureBounded());
        Assert.Throws<InvalidOperationException>(() =>
            new CachePolicy(TimeSpan.FromSeconds(-1), null, Array.Empty<string>(), false, null).EnsureBounded());
        Assert.Throws<InvalidOperationException>(() =>
            new CachePolicy(null, TimeSpan.Zero, Array.Empty<string>(), false, null).EnsureBounded());
        Assert.Throws<InvalidOperationException>(() =>
            new CachePolicy(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(-1), Array.Empty<string>(), false, null).EnsureBounded());
        Assert.Throws<InvalidOperationException>(() =>
            new CachePolicy(TimeSpan.FromMinutes(1), null, Array.Empty<string>(), true, null).EnsureBounded());
        Assert.Throws<InvalidOperationException>(() =>
            new CachePolicy(TimeSpan.FromMinutes(1), null, Array.Empty<string>(), true, TimeSpan.Zero).EnsureBounded());
        Assert.Throws<InvalidOperationException>(() =>
            new CachePolicy(null, null, Array.Empty<string>(), false, null).EnsureBounded());

        new CachePolicy(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30), Array.Empty<string>(), false, null)
            .EnsureBounded();
    }

    [Fact]
    public async Task Memory_and_none_reject_blank_tag_and_namespace_consistently()
    {
        using var memory = CreateFixture();
        using var none = CreateFixture(provider: "None");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            memory.Invalidator.InvalidateByTagAsync(" ", CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            none.Invalidator.InvalidateByTagAsync(" ", CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            memory.Invalidator.InvalidateByNamespaceAsync("", CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            none.Invalidator.InvalidateByNamespaceAsync("", CancellationToken.None));

        var key = TenantKey("tenant-a", "sku-policy");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            memory.Cache.SetAsync(
                key,
                new CatalogProjection("sku-policy", "x"),
                new CachePolicy(TimeSpan.Zero, null, Array.Empty<string>(), false, null),
                CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            none.Cache.SetAsync(
                key,
                new CatalogProjection("sku-policy", "x"),
                new CachePolicy(TimeSpan.Zero, null, Array.Empty<string>(), false, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Remove_invalidates_key()
    {
        using var fixture = CreateFixture();
        var cache = fixture.Cache;
        var key = TenantKey("tenant-a", "sku-remove");
        var policy = CachePolicy.Expiring(TimeSpan.FromMinutes(1));
        await cache.SetAsync(key, new CatalogProjection("sku-remove", "x"), policy, CancellationToken.None);
        await cache.RemoveAsync(key, CancellationToken.None);
        Assert.Null(await cache.GetAsync<CatalogProjection>(key, CancellationToken.None));
    }

    [Fact]
    public async Task Tag_invalidation_removes_tagged_keys_only()
    {
        using var fixture = CreateFixture();
        var cache = fixture.Cache;
        var tagged = TenantKey("tenant-a", "sku-tag");
        var other = TenantKey("tenant-a", "sku-other");
        var taggedPolicy = CachePolicy.Expiring(TimeSpan.FromMinutes(1), tags: new[] { "catalog:product:sku-tag" });
        var otherPolicy = CachePolicy.Expiring(TimeSpan.FromMinutes(1), tags: new[] { "catalog:product:sku-other" });
        await cache.SetAsync(tagged, new CatalogProjection("sku-tag", "t"), taggedPolicy, CancellationToken.None);
        await cache.SetAsync(other, new CatalogProjection("sku-other", "o"), otherPolicy, CancellationToken.None);
        await fixture.Invalidator.InvalidateByTagAsync("catalog:product:sku-tag", CancellationToken.None);
        Assert.Null(await cache.GetAsync<CatalogProjection>(tagged, CancellationToken.None));
        Assert.Equal("o", (await cache.GetAsync<CatalogProjection>(other, CancellationToken.None))?.Title);
    }

    [Fact]
    public async Task Expired_entries_do_not_leave_broken_tag_invalidation()
    {
        using var fixture = CreateFixture();
        var cache = fixture.Cache;
        var expired = TenantKey("tenant-a", "sku-exp");
        var kept = TenantKey("tenant-a", "sku-kept");
        await cache.SetAsync(
            expired,
            new CatalogProjection("sku-exp", "old"),
            CachePolicy.Expiring(TimeSpan.FromMilliseconds(50), tags: new[] { "catalog:product:sku-exp" }),
            CancellationToken.None);
        await cache.SetAsync(
            kept,
            new CatalogProjection("sku-kept", "keep"),
            CachePolicy.Expiring(TimeSpan.FromMinutes(1), tags: new[] { "unrelated" }),
            CancellationToken.None);
        await Task.Delay(150);
        Assert.Null(await cache.GetAsync<CatalogProjection>(expired, CancellationToken.None));
        await fixture.Invalidator.InvalidateByTagAsync("catalog:product:sku-exp", CancellationToken.None);
        Assert.Equal("keep", (await cache.GetAsync<CatalogProjection>(kept, CancellationToken.None))?.Title);
    }

    [Fact]
    public void Public_cache_contract_is_dto_not_ef_entity()
    {
        var cacheSource = File.ReadAllText(
            Path.Combine(FindRepoRoot(), "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks", "Cache.cs"));
        Assert.Contains("موجودیت tracked مربوط به EF", cacheSource, StringComparison.Ordinal);
        Assert.Contains("DbContext", cacheSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", cacheSource, StringComparison.Ordinal);
        Assert.True(typeof(CatalogProjection).IsClass);
        Assert.False(typeof(CatalogProjection).IsSubclassOf(typeof(Microsoft.EntityFrameworkCore.DbContext)));
    }

    [Fact]
    public void Redis_packages_are_absent()
    {
        var root = FindRepoRoot();
        foreach (var csproj in Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(csproj);
            Assert.DoesNotContain("StackExchange.Redis", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Microsoft.Extensions.Caching.StackExchangeRedis", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Host_does_not_register_memory_cache_for_modules()
    {
        using var provider = CreateProvider();
        Assert.Null(provider.GetService<IMemoryCache>());
        Assert.NotNull(provider.GetService<ICache>());
        Assert.NotNull(provider.GetService<ICacheInvalidator>());
        Assert.NotNull(provider.GetService<ICacheKeyBuilder>());
    }

    private static CacheFixture CreateFixture(string provider = "Memory")
    {
        return new CacheFixture(CreateProvider(provider));
    }

    private static ServiceProvider CreateProvider(string provider = "Memory")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<CacheHostOptions>()
            .Configure(options =>
            {
                options.Enabled = true;
                options.Provider = provider;
                options.EntryCountLimit = 1000;
                options.StampedeProtection = true;
            });
        services.AddSingleton<IValidateOptions<CacheHostOptions>, CacheOptionsValidator>();
        services.AddToobaCache();
        return services.BuildServiceProvider();
    }

    private static CacheKey TenantKey(string tenantId, string resourceId) =>
        Keys.Build(new CacheKeyParts
        {
            Namespace = "catalog",
            ResourceType = "product",
            ResourceId = resourceId,
            Edition = ToobaEdition.SingleStore,
            DeploymentId = "dep-1",
            TenantId = tenantId,
            TenantScoped = true,
        });

    private static CacheKey DimensionKey(string market, string locale, string currency) =>
        Keys.Build(new CacheKeyParts
        {
            Namespace = "pricing",
            ResourceType = "offer",
            ResourceId = "offer-1",
            Edition = ToobaEdition.SingleStore,
            DeploymentId = "dep-1",
            TenantId = "tenant-a",
            TenantScoped = true,
            Market = market,
            Locale = locale,
            Currency = currency,
        });

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    /// <summary>
    /// ظرف تست برای آزادسازی ارائه‌دهنده همراه با حافظهٔ فرآیند.
    /// </summary>
    private sealed class CacheFixture : IDisposable
    {
        public CacheFixture(ServiceProvider provider)
        {
            Provider = provider;
            Cache = provider.GetRequiredService<ICache>();
            Invalidator = provider.GetRequiredService<ICacheInvalidator>();
        }

        public ServiceProvider Provider { get; }

        public ICache Cache { get; }

        public ICacheInvalidator Invalidator { get; }

        public void Dispose() => Provider.Dispose();
    }

    /// <summary>
    /// نمونهٔ projection خواندنی برای تست؛ موجودیت tracked نیست و الگوی مجاز کش است.
    /// </summary>
    private sealed record CatalogProjection(string Id, string Title);

    /// <summary>
    /// نوع ناسازگار برای آزمون type mismatch.
    /// </summary>
    private sealed record OtherProjection(string Id, string Title);
}
