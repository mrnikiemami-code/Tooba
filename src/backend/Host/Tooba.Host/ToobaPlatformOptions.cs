using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

internal sealed class ToobaPlatformOptions
{
    public const string SectionName = "Tooba";

    public string Edition { get; set; } = "Unset";
    public string DeploymentId { get; set; } = "";
    public List<string> TrustedProxies { get; set; } = [];
    public MarketplaceOptions Marketplace { get; set; } = new();
    public SingleStoreOptions SingleStore { get; set; } = new();
    public PostgreSqlOptions PostgreSQL { get; set; } = new();
}

internal sealed class MarketplaceOptions
{
    public string ConnectionReference { get; set; } = "";
}

internal sealed class SingleStoreOptions
{
    public List<TenantRecordOptions> Tenants { get; set; } = [];
}

internal sealed class TenantRecordOptions
{
    public string TenantId { get; set; } = "";
    public string? DisplayName { get; set; }
    public string Status { get; set; } = "Active";
    public string ConnectionReference { get; set; } = "";
    public string? ThemeReference { get; set; }
    public string? DefaultMarketReference { get; set; }
    public string? PrimaryDomain { get; set; }
    public List<string> Hosts { get; set; } = [];
}

internal sealed class PostgreSqlOptions
{
    public string ConnectionString { get; set; } = "";
    public Dictionary<string, string> ConnectionReferences { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class TenantRecord
{
    public required TenantId TenantId { get; init; }
    public string? DisplayName { get; init; }
    public required TenantStatus Status { get; init; }
    public required ConnectionReference ConnectionReference { get; init; }
    public string? ThemeReference { get; init; }
    public string? DefaultMarketReference { get; init; }
    public string? PrimaryDomain { get; init; }
    public required IReadOnlyList<string> Hosts { get; init; }
}

internal sealed class ControlPlaneRegistry
{
    public required ToobaEdition Edition { get; init; }
    public required string DeploymentId { get; init; }
    public ConnectionReference? MarketplaceConnectionReference { get; init; }
    public required IReadOnlyDictionary<string, TenantRecord> Hosts { get; init; }
    public required IReadOnlyDictionary<string, TenantRecord> Tenants { get; init; }
}

internal sealed class PlatformOptionsValidator : IValidateOptions<ToobaPlatformOptions>
{
    public ValidateOptionsResult Validate(string? name, ToobaPlatformOptions options)
    {
        if (!TryParseEdition(options.Edition, out _))
        {
            return ValidateOptionsResult.Fail($"Unsupported Tooba:Edition '{options.Edition}'.");
        }

        try
        {
            _ = BuildRegistry(options);
        }
        catch (InvalidOperationException ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }

        return ValidateOptionsResult.Success;
    }

    public static bool TryParseEdition(string? value, out ToobaEdition edition)
    {
        edition = ToobaEdition.Unset;
        if (string.IsNullOrWhiteSpace(value) || value.Equals("Unset", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.Equals("Marketplace", StringComparison.OrdinalIgnoreCase))
        {
            edition = ToobaEdition.Marketplace;
            return true;
        }

        if (value.Equals("SingleStore", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Single-Store", StringComparison.OrdinalIgnoreCase))
        {
            edition = ToobaEdition.SingleStore;
            return true;
        }

        return false;
    }

    public static TenantStatus ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            return TenantStatus.Active;
        }

        if (value.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return TenantStatus.Disabled;
        }

        if (value.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
        {
            return TenantStatus.Suspended;
        }

        throw new InvalidOperationException($"Unsupported tenant status '{value}'.");
    }

    public static ControlPlaneRegistry BuildRegistry(ToobaPlatformOptions options)
    {
        if (!TryParseEdition(options.Edition, out var edition))
        {
            throw new InvalidOperationException($"Unsupported Tooba:Edition '{options.Edition}'.");
        }

        var deploymentId = string.IsNullOrWhiteSpace(options.DeploymentId)
            ? edition.ToString()
            : options.DeploymentId.Trim();

        var hosts = new Dictionary<string, TenantRecord>(StringComparer.Ordinal);
        var tenants = new Dictionary<string, TenantRecord>(StringComparer.Ordinal);

        if (edition == ToobaEdition.Marketplace)
        {
            if (string.IsNullOrWhiteSpace(options.Marketplace.ConnectionReference))
            {
                throw new InvalidOperationException("Marketplace edition requires Tooba:Marketplace:ConnectionReference.");
            }
        }

        if (edition == ToobaEdition.SingleStore)
        {
            foreach (var raw in options.SingleStore.Tenants)
            {
                if (string.IsNullOrWhiteSpace(raw.TenantId))
                {
                    throw new InvalidOperationException("Single-Store tenant is missing TenantId.");
                }

                if (string.IsNullOrWhiteSpace(raw.ConnectionReference))
                {
                    throw new InvalidOperationException($"Tenant '{raw.TenantId}' is missing ConnectionReference.");
                }

                if (tenants.ContainsKey(raw.TenantId))
                {
                    throw new InvalidOperationException($"Duplicate TenantId '{raw.TenantId}'.");
                }

                var hostList = new List<string>();
                foreach (var host in raw.Hosts)
                {
                    if (!HostNormalizer.TryNormalize(host, out var normalized))
                    {
                        throw new InvalidOperationException($"Tenant '{raw.TenantId}' has invalid host '{host}'.");
                    }

                    if (hosts.ContainsKey(normalized))
                    {
                        throw new InvalidOperationException($"Duplicate host mapping '{normalized}'.");
                    }

                    hostList.Add(normalized);
                }

                if (raw.PrimaryDomain is not null
                    && HostNormalizer.TryNormalize(raw.PrimaryDomain, out var primary)
                    && !hostList.Contains(primary))
                {
                    if (hosts.ContainsKey(primary))
                    {
                        throw new InvalidOperationException($"Duplicate host mapping '{primary}'.");
                    }

                    hostList.Add(primary);
                }

                if (hostList.Count == 0)
                {
                    throw new InvalidOperationException($"Tenant '{raw.TenantId}' has no host mappings.");
                }

                var record = new TenantRecord
                {
                    TenantId = new TenantId(raw.TenantId),
                    DisplayName = raw.DisplayName,
                    Status = ParseStatus(raw.Status),
                    ConnectionReference = new ConnectionReference(raw.ConnectionReference),
                    ThemeReference = raw.ThemeReference,
                    DefaultMarketReference = raw.DefaultMarketReference,
                    PrimaryDomain = raw.PrimaryDomain is not null
                        && HostNormalizer.TryNormalize(raw.PrimaryDomain, out var pd)
                        ? pd
                        : hostList[0],
                    Hosts = hostList,
                };

                tenants[raw.TenantId] = record;
                foreach (var h in hostList)
                {
                    hosts[h] = record;
                }
            }
        }

        return new ControlPlaneRegistry
        {
            Edition = edition,
            DeploymentId = deploymentId,
            MarketplaceConnectionReference = edition == ToobaEdition.Marketplace
                ? new ConnectionReference(options.Marketplace.ConnectionReference)
                : null,
            Hosts = hosts,
            Tenants = tenants,
        };
    }
}
