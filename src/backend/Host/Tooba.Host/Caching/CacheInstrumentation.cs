using System.Diagnostics;
using System.Diagnostics.Metrics;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// متریک کش با برچسب کران‌دار. TenantId و کلید کامل و شناسهٔ کاربر بعد متریک نیستند.
/// </summary>
internal sealed class CacheInstrumentation
{
    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;
    private readonly Counter<long> _sets;
    private readonly Counter<long> _removes;
    private readonly Counter<long> _invalidations;
    private readonly Counter<long> _evictions;
    private readonly Counter<long> _stampedeWaits;
    private readonly Histogram<double> _factoryDurationMs;

    /// <summary>
    /// شمارنده‌ها را روی Meter پایدار Tooba ثبت می‌کند.
    /// </summary>
    public CacheInstrumentation()
    {
        var meter = ToobaTelemetry.Meter;
        _hits = meter.CreateCounter<long>("tooba.cache.hit");
        _misses = meter.CreateCounter<long>("tooba.cache.miss");
        _sets = meter.CreateCounter<long>("tooba.cache.set");
        _removes = meter.CreateCounter<long>("tooba.cache.remove");
        _invalidations = meter.CreateCounter<long>("tooba.cache.invalidation");
        _evictions = meter.CreateCounter<long>("tooba.cache.eviction");
        _stampedeWaits = meter.CreateCounter<long>("tooba.cache.stampede.wait");
        _factoryDurationMs = meter.CreateHistogram<double>("tooba.cache.factory.duration", "ms");
    }

    /// <summary>
    /// برچسب‌های مجاز برای این رویداد.
    /// </summary>
    /// <param name="provider">Memory یا None.</param>
    /// <param name="key">فقط Namespace و Edition از کلید خوانده می‌شود.</param>
    public TagList Tags(string provider, CacheKey key) =>
        new()
        {
            { "cache.provider", provider },
            { "cache.namespace", key.Namespace },
            { "cache.edition", key.EditionLabel },
        };

    /// <summary>hit داخل فرآیند.</summary>
    public void Hit(string provider, CacheKey key) => _hits.Add(1, Tags(provider, key));

    /// <summary>miss؛ منبع حقیقت باید خوانده شود.</summary>
    public void Miss(string provider, CacheKey key) => _misses.Add(1, Tags(provider, key));

    /// <summary>ذخیرهٔ موفق.</summary>
    public void Set(string provider, CacheKey key) => _sets.Add(1, Tags(provider, key));

    /// <summary>حذف تک‌کلید.</summary>
    public void Remove(string provider, CacheKey key) => _removes.Add(1, Tags(provider, key));

    /// <summary>ابطال برچسب یا namespace.</summary>
    public void Invalidation(string provider, string ns, string edition) =>
        _invalidations.Add(1, new TagList
        {
            { "cache.provider", provider },
            { "cache.namespace", ns },
            { "cache.edition", edition },
        });

    /// <summary>اخراج حافظه یا انقضا.</summary>
    public void Eviction(string provider, CacheKey key) => _evictions.Add(1, Tags(provider, key));

    /// <summary>منتظر ماندن پشت single-flight همان کلید.</summary>
    public void StampedeWait(string provider, CacheKey key) => _stampedeWaits.Add(1, Tags(provider, key));

    /// <summary>مدت کارخانهٔ منبع حقیقت.</summary>
    public void FactoryDuration(string provider, CacheKey key, double milliseconds) =>
        _factoryDurationMs.Record(milliseconds, Tags(provider, key));
}
