using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts.Dtos;
using Tooba.StoreContext.Contracts.Current;

namespace Tooba.Host;

/// <summary>
/// بایندینگ پیکربندی بخش <c>Tooba</c>. این registry کنترل‌پلین تولید نیست؛ فقط bootstrap پیکربندی است.
/// کلیدهای dictionary اتصال نباید نویسهٔ <c>:</c> داشته باشند چون ASP.NET آن‌ها را تو در تو می‌کند.
/// </summary>
internal sealed class ToobaPlatformOptions
{
    /// <summary>
    /// نام بخش پیکربندی ریشه.
    /// </summary>
    public const string SectionName = "Tooba";

    /// <summary>
    /// Marketplace | SingleStore | Unset — یک فرآیند یک Edition.
    /// </summary>
    public string Edition { get; set; } = "Unset";

    /// <summary>
    /// برچسب استقرار برای تله‌متری؛ TenantId نیست.
    /// </summary>
    public string DeploymentId { get; set; } = "";

    /// <summary>
    /// IPهای proxy مورد اعتماد. خالی یعنی Forwarded Host اعمال نشود.
    /// </summary>
    public List<string> TrustedProxies { get; set; } = [];

    /// <summary>
    /// تنظیمات اختصاصی Marketplace.
    /// </summary>
    public MarketplaceOptions Marketplace { get; set; } = new();

    /// <summary>
    /// تنظیمات allowlist Single-Store.
    /// </summary>
    public SingleStoreOptions SingleStore { get; set; } = new();

    /// <summary>
    /// نقشهٔ ConnectionReference به رشتهٔ اتصال. مقدار رشته لاگ نشود.
    /// </summary>
    public PostgreSqlOptions PostgreSQL { get; set; } = new();

    /// <summary>
    /// زمینهٔ تجارت مؤثر فروشگاه در سطح deployment (مسیر Marketplace). در Single-Store از هر Tenant خوانده می‌شود.
    /// مالک این تنظیم کنترل‌پلین پلتفرم است؛ ماژول مصرف‌کننده صاحب پیش‌فرض تجاری نمی‌شود.
    /// </summary>
    public StoreCommerceOptions StoreCommerce { get; set; } = new();
}

/// <summary>
/// زمینهٔ تجارت مؤثر فروشگاه برای deploymentهایی که Tenant جدا ندارند (Marketplace).
/// این پیش‌فرض تجاری مالک کنترل‌پلین است، نه ماژول‌های دامنه‌ای.
/// </summary>
internal sealed class StoreCommerceOptions
{
    /// <summary>
    /// مرجع بازار مؤثر؛ تهی یعنی resolve نشده و مصرف‌کننده fail-closed می‌شود.
    /// </summary>
    public string? Market { get; set; }

    /// <summary>
    /// کد ارز مؤثر ISO؛ تهی یعنی resolve نشده.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// نام پایدار کانال فروش؛ تهی یعنی resolve نشده.
    /// </summary>
    public string? SalesChannel { get; set; }
}

/// <summary>
/// اتصال واحد marketplace؛ lookup فروشگاه از Host انجام نمی‌شود.
/// </summary>
internal sealed class MarketplaceOptions
{
    /// <summary>
    /// کلید ConnectionReference پایگاه marketplace.
    /// </summary>
    public string ConnectionReference { get; set; } = "";
}

/// <summary>
/// فهرست Tenantهای Single-Store در پیکربندی محلی.
/// </summary>
internal sealed class SingleStoreOptions
{
    /// <summary>
    /// رکوردهای Tenant؛ هر کدام حداقل یک Host نرمال‌شده نیاز دارند.
    /// </summary>
    public List<TenantRecordOptions> Tenants { get; set; } = [];
}

/// <summary>
/// شکل خام یک Tenant در پیکربندی قبل از نرمال‌سازی Host.
/// </summary>
internal sealed class TenantRecordOptions
{
    /// <summary>
    /// هویت پایدار؛ hostname نیست.
    /// </summary>
    public string TenantId { get; set; } = "";

    /// <summary>
    /// نام نمایشی اختیاری.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Active / Disabled / Suspended.
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// مرجع اتصال فروشگاه این Tenant.
    /// </summary>
    public string ConnectionReference { get; set; } = "";

    /// <summary>
    /// ارجاع تم اختیاری.
    /// </summary>
    public string? ThemeReference { get; set; }

    /// <summary>
    /// ارجاع بازار پیش‌فرض؛ با Locale یکی نیست.
    /// </summary>
    public string? DefaultMarketReference { get; set; }

    /// <summary>
    /// بازار می‌تواند از DefaultMarketReference ارث ببرد؛ ارز و کانال فروش نیازمند مقدار صریح StoreCommerce در همین Tenant هستند.
    /// </summary>
    public StoreCommerceOptions? StoreCommerce { get; set; }

    /// <summary>
    /// دامنهٔ اصلی در صورت وجود.
    /// </summary>
    public string? PrimaryDomain { get; set; }

    /// <summary>
    /// Hostهای مجاز برای routing به این Tenant.
    /// </summary>
    public List<string> Hosts { get; set; } = [];
}

/// <summary>
/// نقشهٔ مراجع اتصال به رشتهٔ Npgsql. ConnectionString ریشه فقط سازگاری قدیمی است و مرجع منطقی نیست.
/// </summary>
internal sealed class PostgreSqlOptions
{
    /// <summary>
    /// رشتهٔ تکی اختیاری؛ مسیر اصلی resolve از ConnectionReferences است.
    /// </summary>
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// کلید = ConnectionReference، مقدار = connection string. هرگز در ProblemDetails نیاید.
    /// </summary>
    public Dictionary<string, string> ConnectionReferences { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// رکورد immutable پس از اعتبارسنجی پیکربندی؛ مبنای resolve درخواست.
/// </summary>
internal sealed class TenantRecord
{
    /// <summary>
    /// هویت پایدار Tenant.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    /// نام نمایشی.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// وضعیت عملیاتی پس از parse.
    /// </summary>
    public required TenantStatus Status { get; init; }

    /// <summary>
    /// مرجع اتصال فروشگاه.
    /// </summary>
    public required ConnectionReference ConnectionReference { get; init; }

    /// <summary>
    /// ارجاع تم.
    /// </summary>
    public string? ThemeReference { get; init; }

    /// <summary>
    /// ارجاع بازار پیش‌فرض.
    /// </summary>
    public string? DefaultMarketReference { get; init; }

    /// <summary>
    /// زمینهٔ تجارت مؤثر همین فروشگاه. اگر تنظیم نشده باشد، DefaultMarketReference بازار را تأمین می‌کند و ارز/کانال تهی می‌مانند.
    /// </summary>
    public StoreCommerceContext StoreCommerce { get; init; } = new(null, null, null);

    /// <summary>
    /// دامنهٔ اصلی نرمال‌شده در صورت وجود.
    /// </summary>
    public string? PrimaryDomain { get; init; }

    /// <summary>
    /// Hostهای نرمال‌شدهٔ این Tenant.
    /// </summary>
    public required IReadOnlyList<string> Hosts { get; init; }
}

/// <summary>
/// تصویر فقط‌خواندنی control plane پیکربندی‌شده برای یک فرآیند. منبع تولید Tenant نیست.
/// </summary>
internal sealed class ControlPlaneRegistry
{
    /// <summary>
    /// Edition قفل‌شدهٔ فرآیند.
    /// </summary>
    public required ToobaEdition Edition { get; init; }

    /// <summary>
    /// برچسب استقرار.
    /// </summary>
    public required string DeploymentId { get; init; }

    /// <summary>
    /// مرجع اتصال marketplace؛ در Single-Store تهی است.
    /// </summary>
    public ConnectionReference? MarketplaceConnectionReference { get; init; }

    /// <summary>
    /// زمینهٔ تجارت مؤثر در سطح deployment (مسیر Marketplace). در Single-Store از Tenant خوانده می‌شود.
    /// </summary>
    public StoreCommerceContext DeploymentStoreCommerce { get; init; } = new(null, null, null);

    /// <summary>
    /// نگاشت Host نرمال‌شده → Tenant. کلید هویت نیست.
    /// </summary>
    public required IReadOnlyDictionary<string, TenantRecord> Hosts { get; init; }

    /// <summary>
    /// نگاشت TenantId → رکورد.
    /// </summary>
    public required IReadOnlyDictionary<string, TenantRecord> Tenants { get; init; }
}

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

        if (string.IsNullOrWhiteSpace(storeCommerce.Currency))
        {
            return $"Production {scope} requires effective StoreCommerce:Currency to be configured.";
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
                    StoreCommerce = ResolveStoreCommerce(raw.DefaultMarketReference, raw.StoreCommerce),
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
            Normalize(raw?.Currency),
            Normalize(raw?.SalesChannel));
    }

    /// <summary>
    /// مقدار پیکربندی را trim می‌کند؛ تهی/فاصله یعنی resolve نشده و مصرف‌کننده باید fail-closed شود.
    /// </summary>
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
