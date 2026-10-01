using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;

namespace Tooba.Host.Caching;

/// <summary>
/// ارائه‌دهندهٔ درون‌فرآیندی. بین نمونه‌های Host مشترک نیست. IMemoryCache را به ماژول‌ها لو نمی‌دهد.
/// Single-flight با هماهنگی قفل‌دار per-key است تا acquire و retire به split-brain منجر نشود.
/// ناسازگاری نوع: ورود حذف می‌شود، تله‌متری type_mismatch ثبت می‌شود، و miss برمی‌گردد (نه hit-null خاموش).
/// </summary>
internal sealed class MemoryToobaCache : ICache, ICacheInvalidator, IDisposable
{
    internal const string ProviderName = "Memory";
    internal const string NamespaceTagPrefix = "ns:";

    private readonly MemoryCache _memory;
    private readonly CacheHostOptions _options;
    private readonly CacheInstrumentation _telemetry;
    private readonly ILogger<MemoryToobaCache> _logger;
    private readonly CacheInflightCoordinator _inflight = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tagToKeys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string[]> _keyToTags = new(StringComparer.Ordinal);

    /// <summary>
    /// درز تست برای اثبات race بازنشستگی؛ در تولید null می‌ماند.
    /// </summary>
    internal Action? AfterRefCountZeroBeforeRecheckRemove
    {
        get => _inflight.AfterRefCountZeroBeforeRecheckRemove;
        set => _inflight.AfterRefCountZeroBeforeRecheckRemove = value;
    }

    /// <summary>
    /// دسترسی تست به هماهنگی inflight.
    /// </summary>
    internal CacheInflightCoordinator InflightForTests => _inflight;

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

        var slot = _inflight.Acquire(key.Value);
        try
        {
            var entered = await slot.Gate.WaitAsync(0, cancellationToken).ConfigureAwait(false);
            if (!entered)
            {
                _telemetry.StampedeWait(ProviderName, key);
                await slot.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
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
                slot.Gate.Release();
            }
        }
        finally
        {
            _inflight.Release(key.Value, slot);
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
            _telemetry.FactoryFailure(ProviderName, key);
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

    /// <summary>
    /// خواندن typed. اگر payload غیر null با نوع ناسازگار باشد، ورود حذف و miss برمی‌گردد (نه hit با null).
    /// نشان منفی از type mismatch جدا می‌ماند.
    /// </summary>
    private bool TryRead<T>(CacheKey key, out T? value, out bool hit)
        where T : class
    {
        value = null;
        hit = false;
        if (!_memory.TryGetValue(key.Value, out var boxed) || boxed is not CacheBox box)
        {
            return false;
        }

        if (ReferenceEquals(box.Payload, NullSentinel.Instance))
        {
            hit = true;
            return true;
        }

        if (box.Payload is T typed)
        {
            value = typed;
            hit = true;
            return true;
        }

        _memory.Remove(key.Value);
        DropTagIndex(key.Value);
        _telemetry.TypeMismatch(ProviderName, key);
        _logger.LogWarning(
            "Cache type mismatch entry removed. Namespace={Namespace} Edition={Edition}",
            key.Namespace,
            key.EditionLabel);
        return false;
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
/// هماهنگی single-flight per-key با قفل slot و بازنشستگی فقط وقتی RefCount هنوز صفر است.
/// Gate هرگز Dispose نمی‌شود. Acquire نمی‌تواند به slot در حال retire که از dictionary حذف شده بچسبد.
/// </summary>
internal sealed class CacheInflightCoordinator
{
    private readonly ConcurrentDictionary<string, InflightSlot> _slots = new(StringComparer.Ordinal);

    /// <summary>
    /// درز تست: بین صفر شدن RefCount و بازبینی/حذف صدا زده می‌شود تا attach همزمان شبیه‌سازی شود.
    /// </summary>
    internal Action? AfterRefCountZeroBeforeRecheckRemove { get; set; }

    /// <summary>
    /// اتصال به slot فعال برای کلید؛ slot بازنشسته‌شده رد می‌شود.
    /// </summary>
    internal InflightSlot Acquire(string cacheKey)
    {
        while (true)
        {
            var slot = _slots.GetOrAdd(cacheKey, static _ => new InflightSlot());
            lock (slot.Sync)
            {
                if (slot.Retired)
                {
                    _slots.TryRemove(KeyValuePair.Create(cacheKey, slot));
                    continue;
                }

                if (!_slots.TryGetValue(cacheKey, out var current) || !ReferenceEquals(current, slot))
                {
                    continue;
                }

                slot.RefCount++;
                return slot;
            }
        }
    }

    /// <summary>
    /// آزادسازی اتصال؛ حذف dictionary فقط وقتی هیچ attach جدیدی پذیرفته نشده باشد.
    /// </summary>
    internal void Release(string cacheKey, InflightSlot slot)
    {
        lock (slot.Sync)
        {
            slot.RefCount--;
            if (slot.RefCount > 0)
            {
                return;
            }
        }

        // پنجرهٔ تعمدی برای تست race؛ در تولید معمولاً فوری است.
        AfterRefCountZeroBeforeRecheckRemove?.Invoke();

        lock (slot.Sync)
        {
            // attach جدید در پنجرهٔ بالا RefCount را بالا برده؛ slot فعال حذف نمی‌شود.
            if (slot.RefCount != 0 || slot.Retired)
            {
                return;
            }

            slot.Retired = true;
            _slots.TryRemove(KeyValuePair.Create(cacheKey, slot));
        }
    }

    /// <summary>
    /// آیا کلید هنوز در dictionary با همین نمونه است (برای تست).
    /// </summary>
    internal bool TryGet(string cacheKey, out InflightSlot? slot) => _slots.TryGetValue(cacheKey, out slot);

    /// <summary>
    /// تعداد ورودی‌های هماهنگی باقی‌مانده (برای تست نشتی).
    /// </summary>
    internal int Count => _slots.Count;
}

/// <summary>
/// شیء هماهنگی per-key؛ Gate هرگز Dispose نمی‌شود.
/// </summary>
internal sealed class InflightSlot
{
    /// <summary>قفل چرخهٔ عمر RefCount/Retire.</summary>
    public readonly object Sync = new();

    /// <summary>تعداد callers متصل.</summary>
    public int RefCount;

    /// <summary>پس از بازنشستگی دیگر attach نمی‌پذیرد.</summary>
    public bool Retired;

    /// <summary>قفل اجرای کارخانه؛ Dispose نمی‌شود.</summary>
    public readonly SemaphoreSlim Gate = new(1, 1);
}

/// <summary>
/// وقتی کش غیرفعال است همیشه miss می‌دهد. کارخانه اجرا می‌شود و چیزی ذخیره نمی‌شود تا منبع حقیقت تنها مرجع بماند.
/// قرارداد اعتبارسنجی با Memory یکسان است؛ پس از تأیید، invalidation/storage no-op است.
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
        cancellationToken.ThrowIfCancellationRequested();
        _telemetry.Miss("None", key);
        var clock = Stopwatch.StartNew();
        try
        {
            return await factory(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _telemetry.FactoryFailure("None", key);
            throw;
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
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new InvalidOperationException("Cache tag is required for invalidation.");
        }

        _logger.LogDebug("Disabled cache ignored tag invalidation.");
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

        return Task.CompletedTask;
    }
}
