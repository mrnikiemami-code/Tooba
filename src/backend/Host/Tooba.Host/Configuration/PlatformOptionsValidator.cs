using System.Net;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts.Dtos;
using Tooba.StoreContext.Contracts.Current;

namespace Tooba.Host.Configuration;

/// <summary>
/// اعتبارسنجی پیکربندی در شروع فرآیند تا Edition نامعتبر یا Host تکراری fail-fast شود.
/// </summary>
internal sealed class PlatformOptionsValidator : IValidateOptions<ToobaPlatformOptions>
{
    private readonly IHostEnvironment? _environment;

    /// <summary>
    /// اعتبارسنج بدون محیط (تست واحد) یا با محیط Host (DI).
    /// </summary>
    public PlatformOptionsValidator()
    {
    }

    /// <summary>
    /// اعتبارسنج production-aware را با محیط Host می‌سازد.
    /// </summary>
    public PlatformOptionsValidator(IHostEnvironment environment) => _environment = environment;

    /// <summary>
    /// پیکربندی را parse و registry می‌سازد؛ شکست یعنی فرآیند بالا نیاید.
    /// </summary>
    public ValidateOptionsResult Validate(string? name, ToobaPlatformOptions options)
    {
        if (!TryParseEdition(options.Edition, out var edition))
        {
            return ValidateOptionsResult.Fail($"Unsupported Tooba:Edition '{options.Edition}'.");
        }

        var trustedProxiesFailure = ValidateTrustedProxies(options);
        if (trustedProxiesFailure is not null)
        {
            return ValidateOptionsResult.Fail(trustedProxiesFailure);
        }

        try
        {
            var registry = BuildRegistry(options);
            var productionFailure = ValidateProductionRequirements(options, edition, registry);
            if (productionFailure is not null)
            {
                return ValidateOptionsResult.Fail(productionFailure);
            }
        }
        catch (InvalidOperationException ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }

        return ValidateOptionsResult.Success;
    }

    /// <summary>
    /// هر عنصر غیرخالی TrustedProxies باید IP معتبر باشد؛ عنصر خالی/نامعتبر fail-fast است.
    /// </summary>
    private static string? ValidateTrustedProxies(ToobaPlatformOptions options)
    {
        var proxies = options.TrustedProxies;
        if (proxies is null || proxies.Count == 0)
        {
            return null;
        }

        for (var i = 0; i < proxies.Count; i++)
        {
            var entry = proxies[i];
            if (string.IsNullOrWhiteSpace(entry) || !IPAddress.TryParse(entry.Trim(), out _))
            {
                return $"Tooba:TrustedProxies contains an invalid or blank entry at index {i}.";
            }
        }

        return null;
    }

    /// <summary>
    /// در Production edition و مراجع اتصال باید صریحاً پیکربندی شده باشند.
    /// </summary>
    private string? ValidateProductionRequirements(
        ToobaPlatformOptions options,
        ToobaEdition edition,
        ControlPlaneRegistry registry)
    {
        if (_environment is null || !_environment.IsProduction())
        {
            return null;
        }

        if (edition == ToobaEdition.Unset)
        {
            return "Production requires Tooba:Edition to be Marketplace or SingleStore.";
        }

        if (edition == ToobaEdition.SingleStore && registry.Tenants.Count == 0)
        {
            return "Production Single-Store requires at least one configured tenant.";
        }

        foreach (var reference in CollectConfiguredConnectionReferences(options, registry))
        {
            if (!options.PostgreSQL.ConnectionReferences.TryGetValue(reference, out var connection)
                || string.IsNullOrWhiteSpace(connection))
            {
                return $"Production requires PostgreSQL connection reference '{reference}' to be configured.";
            }
        }

        return ValidateStoreCommerce(registry);
    }

    /// <summary>
    /// زمینهٔ تجارت مؤثر فروشگاه باید در Production کامل باشد؛ ناقص بودن یعنی فرآیند بالا نیاید نه شکست دیرهنگام در زمان مصرف.
    /// Marketplace از StoreCommerce سطح deployment، Single-Store از رکورد هر Tenant فعال استفاده می‌کند.
    /// </summary>
    private static string? ValidateStoreCommerce(ControlPlaneRegistry registry)
    {
        if (registry.Edition == ToobaEdition.Marketplace)
        {
            return ValidateStoreCommerceRecord("Marketplace", registry.DeploymentStoreCommerce);
        }

        if (registry.Edition == ToobaEdition.SingleStore)
        {
            foreach (var tenant in registry.Tenants.Values)
            {
                if (tenant.Status != TenantStatus.Active)
                {
                    continue;
                }

                var failure = ValidateStoreCommerceRecord(
                    $"tenant '{tenant.TenantId.Value}'",
                    tenant.StoreCommerce);
                if (failure is not null)
                {
                    return failure;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// یک زمینهٔ تجارت را اعتبارسنجی می‌کند: بازار/ارز خالی نباشند و کانال هم نام canonical باشد.
    /// </summary>
    private static string? ValidateStoreCommerceRecord(string scope, StoreCommerceContext storeCommerce)
    {
        if (string.IsNullOrWhiteSpace(storeCommerce.Market))
        {
            return $"Production {scope} requires effective StoreCommerce:Market to be configured.";
        }

        if (string.IsNullOrWhiteSpace(storeCommerce.DefaultCurrency))
        {
            return $"Production {scope} requires effective StoreCommerce:DefaultCurrency to be configured.";
        }

        return ValidateSalesChannel(scope, storeCommerce.SalesChannel);
    }

    /// <summary>
    /// کانال فروش باید با enum canonical فروشگاه (<see cref="SalesChannel"/>) بخواند؛ مقدار نامعتبر باید در Startup رد شود.
    /// </summary>
    private static string? ValidateSalesChannel(string scope, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)
            || !Enum.TryParse<SalesChannel>(raw.Trim(), ignoreCase: true, out _))
        {
            return $"Production {scope} requires StoreCommerce:SalesChannel to be a valid SalesChannel.";
        }

        return null;
    }

    /// <summary>
    /// مراجع اتصال مورد انتظار edition را برای fail-fast تولید جمع می‌کند.
    /// </summary>
    private static IEnumerable<string> CollectConfiguredConnectionReferences(
        ToobaPlatformOptions options,
        ControlPlaneRegistry registry)
    {
        if (registry.Edition == ToobaEdition.Marketplace
            && registry.MarketplaceConnectionReference is { } marketplaceReference)
        {
            yield return marketplaceReference.Value;
        }

        if (registry.Edition == ToobaEdition.SingleStore)
        {
            foreach (var tenant in registry.Tenants.Values)
            {
                yield return tenant.ConnectionReference.Value;
            }
        }
    }

    /// <summary>
    /// مقدار متنی Edition را به enum تبدیل می‌کند. مقدار ناشناخته false است نه Unset خاموش.
    /// </summary>
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

    /// <summary>
    /// وضعیت Tenant را parse می‌کند. مقدار ناشناخته استثنا است تا پیکربندی غلط silent نشود.
    /// </summary>
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

    /// <summary>
    /// registry immutable را از options می‌سازد. Host تکراری یا Tenant بدون Host رد می‌شود.
    /// </summary>
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

                string? normalizedPrimaryDomain = null;
                if (!string.IsNullOrWhiteSpace(raw.PrimaryDomain))
                {
                    if (!HostNormalizer.TryNormalize(raw.PrimaryDomain, out var primary))
                    {
                        throw new InvalidOperationException($"Tenant '{raw.TenantId}' has invalid PrimaryDomain.");
                    }

                    normalizedPrimaryDomain = primary;
                    if (!hostList.Contains(primary))
                    {
                        if (hosts.ContainsKey(primary))
                        {
                            throw new InvalidOperationException($"Duplicate host mapping '{primary}'.");
                        }

                        hostList.Add(primary);
                    }
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
                    StoreCommerce = ResolveStoreCommerce(raw.DefaultMarketReference, raw.StoreCommerce),
                    PrimaryDomain = normalizedPrimaryDomain ?? hostList[0],
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
            DeploymentStoreCommerce = ResolveStoreCommerce(
                marketFallback: null,
                options.StoreCommerce),
            Hosts = hosts,
            Tenants = tenants,
        };
    }

    /// <summary>
    /// زمینهٔ تجارت مؤثر را از پیکربندی کنترل‌پلین می‌سازد. ارز/کانال فقط از پیکربندی صریح می‌آیند؛
    /// بازار در نبود تنظیم صریح از مرجع بازار پیش‌فرض ارث می‌برد. مقدار نامعلوم در کد اختراع نمی‌شود.
    /// </summary>
    private static StoreCommerceContext ResolveStoreCommerce(string? marketFallback, StoreCommerceOptions? raw)
    {
        var market = Normalize(raw?.Market) ?? Normalize(marketFallback);
        return new StoreCommerceContext(
            market,
            Normalize(raw?.DefaultCurrency),
            Normalize(raw?.SalesChannel));
    }

    /// <summary>
    /// مقدار پیکربندی را trim می‌کند؛ تهی/فاصله یعنی resolve نشده و مصرف‌کننده باید fail-closed شود.
    /// </summary>
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
