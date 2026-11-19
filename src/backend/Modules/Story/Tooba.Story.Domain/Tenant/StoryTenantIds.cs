using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Tenancy;
using Tooba.Story.Contracts.Errors;

namespace Tooba.Story.Domain.Tenant;

/// <summary>شناسهٔ پایدار Tenant برای ماژول Story.</summary>
public static class StoryTenantIds
{
    /// <summary>Tenant توسعهٔ store-alpha.</summary>
    public static readonly Guid StoreAlpha = StableStoryTenantGuid.StoreAlpha;

    /// <summary>Tenant توسعهٔ store-beta برای تست جداسازی.</summary>
    public static readonly Guid StoreBeta = StableStoryTenantGuid.StoreBeta;

    /// <summary>کلید Tenant پیکربندی‌شده را به Guid پایدار نگاشت می‌کند.</summary>
    public static Guid FromTenantKey(string tenantKey)
    {
        if (string.IsNullOrWhiteSpace(tenantKey))
            throw new SemanticException(new SemanticError(StoryErrorCodes.TenantMissing));
        return StableStoryTenantGuid.FromTenantKey(tenantKey);
    }
}
