using System.Globalization;

namespace Tooba.BuildingBlocks;

public enum ToobaEdition
{
    Unset = 0,
    Marketplace = 1,
    SingleStore = 2,
}

public enum TenantStatus
{
    Active = 0,
    Disabled = 1,
    Suspended = 2,
}

public readonly record struct TenantId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct ConnectionReference(string Value)
{
    public override string ToString() => Value;
}

public sealed record EditionContext(
    ToobaEdition Edition,
    string DeploymentId);

public sealed record TenantContext(
    TenantId TenantId,
    TenantStatus Status,
    ConnectionReference ConnectionReference,
    string? DisplayName,
    string? ThemeReference,
    string? DefaultMarketReference,
    string ResolvedHost,
    string? PrimaryDomain);

public sealed record CommerceContext(
    EditionContext Edition,
    TenantContext? Tenant,
    ConnectionReference DatabaseConnectionReference,
    string TraceId);

public interface ICurrentCommerceContext
{
    CommerceContext? Current { get; }
}

public interface ICurrentEdition
{
    EditionContext? Current { get; }
}

public interface ICurrentTenant
{
    TenantContext? Current { get; }
}

public static class HostNormalizer
{
    private static readonly IdnMapping Idn = new();

    public static bool TryNormalize(string? hostHeader, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(hostHeader))
        {
            return false;
        }

        var host = hostHeader.Trim();
        if (host.StartsWith('[') && host.Contains(']'))
        {
            var end = host.IndexOf(']');
            host = host[..(end + 1)];
        }
        else
        {
            var colon = host.LastIndexOf(':');
            if (colon > 0 && host.AsSpan(colon + 1).ToString().All(char.IsDigit))
            {
                host = host[..colon];
            }
        }

        host = host.Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(host) || host == "*")
        {
            return false;
        }

        try
        {
            normalized = Idn.GetAscii(host).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            return false;
        }

        return normalized.Length > 0;
    }
}
