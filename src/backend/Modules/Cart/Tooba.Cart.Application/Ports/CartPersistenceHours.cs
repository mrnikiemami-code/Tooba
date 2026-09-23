using Tooba.Cart.Application.Ports;

namespace Tooba.Cart.Application.Ports;

/// <summary>
/// Cart-owned persistence policy: platform value from <see cref="CartLifetimeOptions"/>,
/// optionally overridden by a store-scoped settings source. Cart owns the clamp/fallback rule.
/// The store override is async; this policy never blocks on the foreign read.
/// </summary>
public static class CartPersistenceHours
{
    /// <summary>Maximum persistence window in hours (90 days).</summary>
    public const int MaxHours = 24 * 90;

    /// <summary>Default persistence window when configuration is absent or non-positive.</summary>
    public const int DefaultHours = 168;

    /// <summary>
    /// Resolves persistence hours from platform options plus an optional store override.
    /// Null or non-positive override inherits the platform value; both are clamped.
    /// </summary>
    /// <param name="options">Cart platform lifetime options.</param>
    /// <param name="storeOverride">Optional store-scoped override seam.</param>
    /// <param name="cancellationToken">Cancellation propagated to the store override read.</param>
    public static async Task<int> ResolveAsync(
        CartLifetimeOptions? options,
        ICartPersistenceHoursResolver? storeOverride,
        CancellationToken cancellationToken)
    {
        var platform = Clamp(options?.PersistenceHours ?? DefaultHours);
        if (storeOverride is null)
        {
            return platform;
        }

        var overrideValue = await storeOverride.ResolveOverrideHoursAsync(cancellationToken).ConfigureAwait(false);
        return overrideValue is int configured ? Clamp(configured) : platform;
    }

    /// <summary>Clamps an hour count into the supported persistence window.</summary>
    public static int Clamp(int hours) =>
        Math.Clamp(hours <= 0 ? DefaultHours : hours, 1, MaxHours);
}
