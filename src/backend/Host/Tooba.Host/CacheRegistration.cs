using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// ثبت abstraction کش. ماژول‌ها فقط ICache/ICacheKeyBuilder/ICacheInvalidator را می‌بینند و IMemoryCache در DI عمومی ثبت نمی‌شود.
/// </summary>
internal static class CacheRegistration
{
    /// <summary>
    /// کلیدساز canonical و ارائه‌دهندهٔ Memory یا None را ثبت می‌کند. بستهٔ Redis اضافه نمی‌شود.
    /// </summary>
    public static IServiceCollection AddToobaCache(this IServiceCollection services)
    {
        services.AddSingleton<ICacheKeyBuilder, CanonicalCacheKeyBuilder>();
        services.AddSingleton<CacheInstrumentation>();
        services.AddSingleton<ICache>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CacheHostOptions>>().Value;
            var provider = options.Provider.Trim();
            if (!options.Enabled || provider.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                return ActivatorUtilities.CreateInstance<DisabledToobaCache>(sp);
            }

            return ActivatorUtilities.CreateInstance<MemoryToobaCache>(sp);
        });
        services.AddSingleton<ICacheInvalidator>(sp => (ICacheInvalidator)sp.GetRequiredService<ICache>());
        return services;
    }
}
