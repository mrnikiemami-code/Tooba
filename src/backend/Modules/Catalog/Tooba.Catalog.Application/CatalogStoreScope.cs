using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Application;

/// <summary>
/// Isolation key for Catalog store-scoped caches (parity with former Host store-appearance projector ScopeKey).
/// </summary>
public static class CatalogStoreScope
{
    /// <summary>کلید isolation بر اساس Tenant یا اتصال Marketplace.</summary>
    public static string ScopeKey(CommerceContext? context)
    {
        if (context?.Tenant is { } tenant)
        {
            return $"tenant:{tenant.TenantId.Value}";
        }

        var connection = context?.DatabaseConnectionReference.Value ?? "marketplace";
        return $"marketplace:{connection}";
    }
}
