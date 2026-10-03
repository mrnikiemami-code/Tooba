using System.Security.Cryptography;
using System.Text;

using Tooba.BuildingBlocks;
using Tooba.PageComposition.Contracts.Errors;

namespace Tooba.PageComposition.Domain.Constants;

/// <summary>شناسهٔ پایدار Tenant برای Page Composition.</summary>
public static class PageCompositionTenantIds
{
    /// <summary>Tenant توسعهٔ store-alpha.</summary>
    public static readonly Guid StoreAlpha = Guid.Parse("a0000000-0001-4000-8000-000000000001");

    /// <summary>Tenant توسعهٔ store-beta برای تست جداسازی.</summary>
    public static readonly Guid StoreBeta = Guid.Parse("a0000000-0002-4000-8000-000000000002");

    /// <summary>کلید Tenant پیکربندی‌شده را به Guid پایدار نگاشت می‌کند.</summary>
    public static Guid FromTenantKey(string tenantKey)
    {
        if (string.IsNullOrWhiteSpace(tenantKey))
            throw new SemanticException(new SemanticError(PageCompositionErrorCodes.TenantMissing));

        if (string.Equals(tenantKey, "store-alpha", StringComparison.Ordinal))
            return StoreAlpha;
        if (string.Equals(tenantKey, "store-beta", StringComparison.Ordinal))
            return StoreBeta;

        var payload = Encoding.UTF8.GetBytes($"tooba:page-composition:{tenantKey.Trim()}");
        var hash = SHA256.HashData(payload);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x40);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }
}
