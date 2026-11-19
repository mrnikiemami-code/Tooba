using System.Security.Cryptography;
using System.Text;

namespace Tooba.BuildingBlocks.Tenancy;

/// <summary>
/// Canonical tenant-key → Guid mapping used by Story-owned tenant ids and compatible Media seams.
/// </summary>
public static class StableStoryTenantGuid
{
    /// <summary>store-alpha stable id.</summary>
    public static readonly Guid StoreAlpha = Guid.Parse("a0000000-0001-4000-8000-000000000001");

    /// <summary>store-beta stable id.</summary>
    public static readonly Guid StoreBeta = Guid.Parse("a0000000-0002-4000-8000-000000000002");

    /// <summary>Maps a configured tenant key to the Story-compatible Guid.</summary>
    public static Guid FromTenantKey(string tenantKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantKey);

        if (string.Equals(tenantKey, "store-alpha", StringComparison.Ordinal))
            return StoreAlpha;
        if (string.Equals(tenantKey, "store-beta", StringComparison.Ordinal))
            return StoreBeta;

        var payload = Encoding.UTF8.GetBytes($"tooba:story:{tenantKey.Trim()}");
        var hash = SHA256.HashData(payload);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x40);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }
}
