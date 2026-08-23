using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// ارائه‌دهندهٔ درون‌فرآیندی. بین نمونه‌های Host مشترک نیست. IMemoryCache را به ماژول‌ها لو نمی‌دهد.
/// </summary>
internal sealed class MemoryToobaCache : ICache, ICacheInvalidator, IDisposable
{
    internal const string ProviderName = "Memory";
    internal const string NamespaceTagPrefix = "ns:";

    private readonly MemoryCache _memory;
    private readonly CacheHostOptions _options;
    private readonly CacheInstrumentation _telemetry;
    private readonly ILogger<MemoryToobaCache> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tagToKeys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string[]> _keyToTags = new(StringComparer.Ordinal);

    /// <summary>
    /// حافظه، تله‌متری و سیاست stampede را می‌گیرد. کلید از Host ساخته نمی‌شود.
    /// </summary>
    /// <summary>
    /// حافظهٔ اختصاصی فرآیند را می‌سازد تا IMemoryCache به ماژول‌های کسب‌وکار تزریق نشود.
    /// </summary>
    public MemoryToobaCache(
        IOptions<CacheHostOptions> options,
        CacheInstrumentation telemetry,
        ILogger<MemoryToobaCache> logger)
    {
        _options = options.Value;
        _memory = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = _options.EntryCountLimit,
            CompactionPercentage = 0.25,
        });
        _telemetry = telemetry;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(CacheKey key, CancellationToken cancellationToken)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (TryRead(key, out T? value, out var hit) && hit)
        {
            _telemetry.Hit(ProviderName, key);
            return Task.FromResult(value);
        }

        _telemetry.Miss(ProviderName, key);
        return Task.FromResult<T?>(null);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(CacheKey key, T? value, CachePolicy policy, CancellationToken cancellationToken)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        policy.EnsureBounded();
        Store(key, value, policy);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<T?> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CancellationToken, Task<T?>> factory,
        CachePolicy policy,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        policy.EnsureBounded();
        cancellationToken.ThrowIfCancellationRequested();

        if (TryRead(key, out T? existing, out var hit) && hit)
        {
            _telemetry.Hit(ProviderName, key);
            return existing;
        }

        if (!_options.StampedeProtection)
        {
            _telemetry.Miss(ProviderName, key);
            return await RunFactoryAndStore(key, factory, policy, cancellationToken).ConfigureAwait(false);
        }

        var gate = _gates.GetOrAdd(key.Value, static _ => new SemaphoreSlim(1, 1));
        var entered = await gate.WaitAsync(0, cancellationToken).ConfigureAwait(false);
        if (!entered)
        {
            _telemetry.StampedeWait(ProviderName, key);
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            if (TryRead(key, out existing, out hit) && hit)
            {
                _telemetry.Hit(ProviderName, key);
                return existing;
            }

            _telemetry.Miss(ProviderName, key);
            return await RunFactoryAndStore(key, factory, policy, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
            if (gate.CurrentCount == 1)
            {
                _gates.TryRemove(key.Value, out _);
            }
        }
    }

    /// <inheritdoc />
    public Task RemoveAsync(CacheKey key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _memory.Remove(key.Value);
        _telemetry.Remove(ProviderName, key);
        _logger.LogInformation("Cache key removed. Namespace={Namespace} Edition={Edition}", key.Namespace, key.EditionLabel);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new InvalidOperationException("Cache tag is required for invalidation.");
        }

        RemoveTagged(tag.Trim());
        _telemetry.Invalidation(ProviderName, "tag", "n/a");
        _logger.LogInformation("Cache tag invalidation. Category={Category}", "tag");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task InvalidateByNamespaceAsync(string ns, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(ns))
        {
            throw new InvalidOperationException("Cache namespace is required for invalidation.");
        }

        var normalized = ns.Trim().ToLowerInvariant();
        RemoveTagged(NamespaceTagPrefix + normalized);
        _telemetry.Invalidation(ProviderName, normalized, "n/a");
        _logger.LogInformation("Cache namespace invalidation. Namespace={Namespace}", normalized);
        return Task.CompletedTask;
    }

    private async Task<T?> RunFactoryAndStore<T>(
        CacheKey key,
        Func<CancellationToken, Task<T?>> factory,
        CachePolicy policy,
        CancellationToken cancellationToken)
        where T : class
    {
        var clock = Stopwatch.StartNew();
        try
        {
            var created = await factory(cancellationToken).ConfigureAwait(false);
            Store(key, created, policy);
            return created;
        }
        catch
        {
            _logger.LogWarning("Cache factory failed; result not stored. Namespace={Namespace} Edition={Edition}", key.Namespace, key.EditionLabel);
            throw;
        }
        finally
        {
            _telemetry.FactoryDuration(ProviderName, key, clock.Elapsed.TotalMilliseconds);
        }
    }

    private void Store<T>(CacheKey key, T? value, CachePolicy policy)
        where T : class
    {
        if (value is null && !policy.CacheNull)
        {
            return;
        }

        var tags = new List<string> { NamespaceTagPrefix + key.Namespace };
        foreach (var tag in policy.Tags)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                tags.Add(tag.Trim());
            }
        }

        var distinctTags = tags.Distinct(StringComparer.Ordinal).ToArray();
        var box = new CacheBox(key, value is null ? NullSentinel.Instance : value);
        var entry = new MemoryCacheEntryOptions
        {
            Size = 1,
        };

        if (value is null)
        {
            entry.AbsoluteExpirationRelativeToNow = policy.NullAbsoluteExpiration;
        }
        else
        {
            if (policy.AbsoluteExpiration is { } absolute)
            {
                entry.AbsoluteExpirationRelativeToNow = absolute;
            }

            if (policy.SlidingExpiration is { } sliding)
            {
                entry.SlidingExpiration = sliding;
            }
        }

        entry.RegisterPostEvictionCallback(OnEvicted, box.Key);
        _keyToTags[key.Value] = distinctTags;
        foreach (var tag in distinctTags)
        {
            var keys = _tagToKeys.GetOrAdd(tag, static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
            keys[key.Value] = 0;
        }

        _memory.Set(key.Value, box, entry);
        _telemetry.Set(ProviderName, key);
        _logger.LogDebug("Cache set. Namespace={Namespace} Edition={Edition}", key.Namespace, key.EditionLabel);
    }

    private bool TryRead<T>(CacheKey key, out T? value, out bool hit)
        where T : class
    {
        value = null;
        hit = false;
        if (!_memory.TryGetValue(key.Value, out var boxed) || boxed is not CacheBox box)
        {
            return false;
        }

        hit = true;
        if (ReferenceEquals(box.Payload, NullSentinel.Instance))
        {
            return true;
        }

        value = box.Payload as T;
        return true;
    }

    private void OnEvicted(object key, object? _, EvictionReason reason, object? state)
    {
        var cacheKey = key as string ?? "";
        DropTagIndex(cacheKey);
        if (state is CacheKey typed)
        {
            _telemetry.Eviction(ProviderName, typed);
        }

        if (reason is EvictionReason.Capacity or EvictionReason.Expired or EvictionReason.TokenExpired)
        {
            _logger.LogDebug("Cache eviction. Namespace={Namespace} Reason={Reason}", typedNs(state), reason);
        }
    }

    private static string typedNs(object? state) => state is CacheKey key ? key.Namespace : "unknown";

    private void RemoveTagged(string tag)
    {
        if (!_tagToKeys.TryRemove(tag, out var keys))
        {
            return;
        }

        foreach (var cacheKey in keys.Keys)
        {
            _memory.Remove(cacheKey);
        }
    }

    /// <summary>
    /// حافظهٔ فرآیند را در خاموشی Host آزاد می‌کند.
    /// </summary>
    public void Dispose() => _memory.Dispose();

    private void DropTagIndex(string cacheKey)
    {
        if (!_keyToTags.TryRemove(cacheKey, out var tags))
        {
            return;
        }

        foreach (var tag in tags)
        {
            if (_tagToKeys.TryGetValue(tag, out var keys))
            {
                keys.TryRemove(cacheKey, out _);
                if (keys.IsEmpty)
                {
                    _tagToKeys.TryRemove(tag, out _);
                }
            }
        }
    }

    /// <summary>
    /// جعبهٔ ورود حافظه تا payload از فرادادهٔ کلید جدا بماند و موجودیت EF در قرارداد عمومی نباشد.
    /// </summary>
    private sealed record CacheBox(CacheKey Key, object Payload);

    /// <summary>
    /// نشان ورود منفی صریح؛ با miss واقعی یکی نیست و بدون سیاست منفی ساخته نمی‌شود.
    /// </summary>
    private sealed class NullSentinel
    {
        /// <summary>
        /// تنها نمونهٔ نشان منفی.
        /// </summary>
        public static readonly NullSentinel Instance = new();

        private NullSentinel()
        {
        }
    }
}

/// <summary>
/// وقتی کش غیرفعال است همیشه miss می‌دهد. کارخانه اجرا می‌شود و چیزی ذخیره نمی‌شود تا منبع حقیقت تنها مرجع بماند.
/// </summary>
internal sealed class DisabledToobaCache : ICache, ICacheInvalidator
{
    private readonly CacheInstrumentation _telemetry;
    private readonly ILogger<DisabledToobaCache> _logger;

    /// <summary>
    /// ارائه‌دهندهٔ خنثی برای Provider=None یا Enabled=false.
    /// </summary>
    public DisabledToobaCache(CacheInstrumentation telemetry, ILogger<DisabledToobaCache> logger)
    {
        _telemetry = telemetry;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(CacheKey key, CancellationToken cancellationToken)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        _telemetry.Miss("None", key);
        return Task.FromResult<T?>(null);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(CacheKey key, T? value, CachePolicy policy, CancellationToken cancellationToken)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        policy.EnsureBounded();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<T?> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CancellationToken, Task<T?>> factory,
        CachePolicy policy,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        policy.EnsureBounded();
        _telemetry.Miss("None", key);
        var clock = Stopwatch.StartNew();
        try
        {
            return await factory(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _telemetry.FactoryDuration("None", key, clock.Elapsed.TotalMilliseconds);
        }
    }

    /// <inheritdoc />
    public Task RemoveAsync(CacheKey key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Disabled cache ignored tag invalidation.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task InvalidateByNamespaceAsync(string ns, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
