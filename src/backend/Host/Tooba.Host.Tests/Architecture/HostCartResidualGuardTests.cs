using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-CART-HOST-RESIDUAL-REVERIFY-001 — durable Host → Cart reverse-audit guard.
/// Prevents Cart business authority from re-entering production Host.
/// Explicit allowlists only; no broad wildcards.
/// </summary>
public sealed class HostCartResidualGuardTests
{
    /// <summary>
    /// Production Host files allowed to name Cart at all, with the reason each is thin.
    /// Every entry is a composition seam, execution shell, or dev/migration surface.
    /// </summary>
    private static readonly Dictionary<string, string> CartNamingAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Program.cs"] = "endpoint/MediatR/module composition only (no Cart implementation)",
        ["Composition/ToobaModuleComposition.cs"] = "explicit module list composition",
        ["CommerceHoldPolicy.cs"] = "Payment/Order hold adapter; holds only (no Cart persistence value)",
        ["CheckoutReservationHoldPolicy.cs"] = "Order hold port adapter (Payment options only)",
        ["Admin/HoldPolicySettingsEndpoints.cs"] = "settings admin UX; reads Cart-owned persistence hours via Cart-owned port",
        ["Admin/ProductWorkspaceDevelopmentBootstrap.cs"] = "Development-only schema migration list",
        ["Storefront/StorefrontModels.cs"] = "storefront wire DTOs that carry CartId",
        ["Storefront/StorefrontComposer.cs"] = "storefront read composition flag",
        ["Storefront/StorefrontEndpoints.cs"] = "checkout identity policy flag name (no Cart authority)",
        ["Order/HostOrderStorefrontActor.cs"] = "thin session → CartAccess adapter",
        ["GlobalUsings.SettlementApp.cs"] = "Settlement-only global usings (no Cart import)",
        ["GlobalUsings.SettlementDomain.cs"] = "Settlement-only global usings (no Cart import)",
        ["UnpaidOrderExpiryHostedService.cs"] = "Order worker shell (no Cart authority)",
        ["UnpaidOrderExpiryHostOptions.cs"] = "Order worker scheduling knobs",
    };

    [Fact]
    public void Host_Cart_naming_files_are_explicitly_allowlisted()
    {
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var offenders = new List<string>();
        foreach (var path in EnumerateProductionSources(hostRoot))
        {
            var relative = Path.GetRelativePath(hostRoot, path).Replace('\\', '/');
            var text = File.ReadAllText(path);
            if (!Regex.IsMatch(text, @"\bCart\w*", RegexOptions.Multiline))
            {
                continue;
            }

            if (!CartNamingAllowlist.ContainsKey(relative))
            {
                offenders.Add(relative);
            }
        }

        Assert.True(offenders.Count == 0, "unlisted Host file referencing Cart: " + string.Join("; ", offenders));
    }

    [Fact]
    public void Host_has_no_Cart_business_authority()
    {
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var forbidden = new List<string>();
        foreach (var path in EnumerateProductionSources(hostRoot))
        {
            var relative = Path.GetRelativePath(hostRoot, path).Replace('\\', '/');
            var text = File.ReadAllText(path);

            if (text.Contains("CartDbContext", StringComparison.Ordinal)
                && relative is not "Admin/ProductWorkspaceDevelopmentBootstrap.cs")
            {
                forbidden.Add($"{relative}: CartDbContext");
            }

            if (text.Contains("using Tooba.Cart.Domain", StringComparison.Ordinal)
                || text.Contains("global using Tooba.Cart.Domain", StringComparison.Ordinal))
            {
                forbidden.Add($"{relative}: Cart.Domain import");
            }

            if (text.Contains("Tooba.Cart.Infrastructure.Persistence", StringComparison.Ordinal)
                && relative is not "Admin/ProductWorkspaceDevelopmentBootstrap.cs")
            {
                forbidden.Add($"{relative}: Cart.Infrastructure persistence import");
            }

            if (Regex.IsMatch(text, @"\bShoppingCart\b|\bCartLine\b|\bCartStatus\b|\bCartConversionIntent\b"))
            {
                forbidden.Add($"{relative}: Cart domain type");
            }
        }

        Assert.True(forbidden.Count == 0, "Cart business authority in Host: " + string.Join("; ", forbidden));
    }

    [Fact]
    public void Host_has_no_Cart_specific_implementation_classes()
    {
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");

        // Cart owns its implementation; Host must not carry these files at all.
        Assert.False(File.Exists(Path.Combine(hostRoot, "CartExpiryHostedService.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "CartExpiryHostOptions.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Composition", "HostCartPersistenceHoursResolver.cs")));

        var hostSources = EnumerateProductionSources(hostRoot).ToArray();

        // Host must not implement Cart Application ports (consuming a Cart-owned port is allowed).
        Assert.DoesNotContain(hostSources, path =>
            Regex.IsMatch(File.ReadAllText(path), @"class\s+\w+[^{]*:\s*[^{]*\bICartPersistenceHours(Source|Resolver)\b"));

        // Host must not own Cart business defaults or worker options.
        Assert.DoesNotContain(hostSources, path =>
            Regex.IsMatch(File.ReadAllText(path), @"\bCartLifetimeOptions\b|\bCartExpiryOptions\b|\bCartCommerceDefaultsOptions\b"));

        // Host must not implement the Cart-owned expiry worker or reconciler.
        Assert.DoesNotContain(hostSources, path =>
            Regex.IsMatch(File.ReadAllText(path), @"\bICartExpiryReconciler\b"));
    }

    [Fact]
    public void Cart_owns_expiry_worker_and_its_worker_seams()
    {
        var cartRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Cart");

        var worker = File.ReadAllText(Path.Combine(
            cartRoot, "Tooba.Cart.Infrastructure", "Lifetime", "CartExpiryWorker.cs"));
        Assert.Contains("ICartExpiryReconciler", worker, StringComparison.Ordinal);
        Assert.Contains("ReconcileAsync(_options.BatchSize, cancellationToken)", worker, StringComparison.Ordinal);
        Assert.Contains("IOutboxPollTargetSource", worker, StringComparison.Ordinal);
        Assert.Contains("IWorkerCommerceContextFactory", worker, StringComparison.Ordinal);
        Assert.Contains("IBackgroundWorkerRegistry", worker, StringComparison.Ordinal);
        Assert.Contains("ICommerceContextAssigner", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("ICartDirectory", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpireDueCartsAsync", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("Guid.NewGuid()", worker, StringComparison.Ordinal);

        var options = File.ReadAllText(Path.Combine(
            cartRoot, "Tooba.Cart.Application", "Lifetime", "CartExpiryOptions.cs"));
        Assert.Contains("Tooba:CartExpiry", options, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            cartRoot, "Tooba.Cart.Infrastructure", "DependencyInjection", "CartModule.cs"));
        Assert.Contains("AddHostedService<CartExpiryWorker>", module, StringComparison.Ordinal);
        Assert.Contains("Configure<CartExpiryOptions>", module, StringComparison.Ordinal);
        Assert.Contains("ICartPersistenceHoursResolver, CatalogCartPersistenceHoursResolver", module, StringComparison.Ordinal);
        Assert.Contains("ICartCommerceContextResolver, CartCommerceContextResolver", module, StringComparison.Ordinal);
        // Cart registers no commerce policy defaults of its own.
        Assert.DoesNotContain("CartCommerceDefaultsOptions", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Cart:CommerceDefaults", module, StringComparison.Ordinal);

        var adapter = File.ReadAllText(Path.Combine(
            cartRoot, "Tooba.Cart.Infrastructure", "Lifetime", "CatalogCartPersistenceHoursResolver.cs"));
        Assert.Contains("ResolveOverrideHoursAsync", adapter, StringComparison.Ordinal);
        Assert.Contains("GetStoreCartPersistenceHoursAsync(cancellationToken)", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("GetAwaiter().GetResult()", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("CancellationToken.None", adapter, StringComparison.Ordinal);
    }

    [Fact]
    public void Cart_persistence_hours_path_is_fully_async()
    {
        var cartRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Cart");
        var files = new[]
        {
            Path.Combine(cartRoot, "Tooba.Cart.Application", "Ports", "ICartPersistenceHoursResolver.cs"),
            Path.Combine(cartRoot, "Tooba.Cart.Application", "Ports", "ICartPersistenceHoursSource.cs"),
            Path.Combine(cartRoot, "Tooba.Cart.Application", "Ports", "CartPersistenceHours.cs"),
            Path.Combine(cartRoot, "Tooba.Cart.Infrastructure", "Lifetime", "CatalogCartPersistenceHoursResolver.cs"),
            Path.Combine(cartRoot, "Tooba.Cart.Infrastructure", "Lifetime", "CartPersistenceHoursSource.cs"),
        };

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain(".Result", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Wait()", text, StringComparison.Ordinal);
            Assert.DoesNotContain("GetAwaiter().GetResult()", text, StringComparison.Ordinal);
            Assert.DoesNotContain("CancellationToken.None", text, StringComparison.Ordinal);
        }

        Assert.Contains(
            "Task<int?> ResolveOverrideHoursAsync(CancellationToken cancellationToken)",
            File.ReadAllText(files[0]),
            StringComparison.Ordinal);
        Assert.Contains(
            "Task<int> ResolvePersistenceHoursAsync(CancellationToken cancellationToken)",
            File.ReadAllText(files[1]),
            StringComparison.Ordinal);
    }

    [Fact]
    public void CreateGuestCart_uses_commerce_context_without_hardcoded_values()
    {
        var repoRoot = FindRepoRoot();
        var handler = File.ReadAllText(Path.Combine(
            repoRoot,
            "src", "backend", "Modules", "Cart", "Tooba.Cart.Application",
            "Commands", "CreateGuestCart", "CreateGuestCartCommand.cs"));

        Assert.Contains("ICartCommerceContextResolver", handler, StringComparison.Ordinal);
        Assert.Contains("commerceContext.Resolve()", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("\"IR\"", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("\"IRR\"", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("SalesChannel.Marketplace", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("SalesChannel.Direct", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void Cart_owns_no_commerce_policy_default_and_consumes_platform_authority()
    {
        var repoRoot = FindRepoRoot();
        var cartRoot = Path.Combine(repoRoot, "src", "backend", "Modules", "Cart");

        // Cart must not carry a commerce-defaults options type, section, or code-level channel literal.
        var cartProduction = EnumerateProductionSources(cartRoot)
            .Where(path => !path.Replace('\\', '/').Contains("/Tooba.Cart.Tests/", StringComparison.Ordinal))
            .ToArray();
        Assert.DoesNotContain(cartProduction, path =>
            Regex.IsMatch(File.ReadAllText(path), @"\bCartCommerceDefaultsOptions\b"));
        Assert.DoesNotContain(cartProduction, path =>
            Regex.IsMatch(File.ReadAllText(path), @"SalesChannel\.(Direct|Marketplace)\b"));
        Assert.DoesNotContain(cartProduction, path =>
            Regex.IsMatch(File.ReadAllText(path), @"""Cart:CommerceDefaults"""));

        // Cart must not keep a global Cart currency/market knob as effective store authority.
        // StoreContext.DefaultCurrency is consumed only as a default-selection input, never as a
        // Cart-owned policy default or a single-currency transaction invariant.
        Assert.DoesNotContain(cartProduction, path =>
            Regex.IsMatch(File.ReadAllText(path), @"\bDefaultMarket\b|\bDefaultSalesChannel\b|\bCartCommerceDefaultsOptions\b"));

        // Effective currency/channel come from the platform-boundary store commerce context.
        var resolver = File.ReadAllText(Path.Combine(
            cartRoot, "Tooba.Cart.Infrastructure", "Lifetime", "CartCommerceContextResolver.cs"));
        Assert.Contains("StoreCommerce", resolver, StringComparison.Ordinal);
        Assert.Contains("ICurrentStoreCommerceContext", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("ICurrentCommerceContext", resolver, StringComparison.Ordinal);
        Assert.Contains("store.DefaultCurrency", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("store.Currency", resolver, StringComparison.Ordinal);
        Assert.Contains("cart.commerce.market_unconfigured", resolver, StringComparison.Ordinal);
        Assert.Contains("cart.commerce.currency_unconfigured", resolver, StringComparison.Ordinal);
        Assert.Contains("cart.commerce.channel_unconfigured", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("IOptions<", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("CartCommerceDefaultsOptions", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("SalesChannel.Direct", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("SalesChannel.Marketplace", resolver, StringComparison.Ordinal);

        // Cart still owns zero dependency on Host; prior Host closure remains intact.
        Assert.DoesNotContain(cartProduction, path =>
            File.ReadAllText(path).Contains("Tooba.Host", StringComparison.Ordinal));
    }

    [Fact]
    public void StoreContext_owns_effective_store_commerce_and_BuildingBlocks_does_not()
    {
        var repoRoot = FindRepoRoot();
        var buildingBlocks = File.ReadAllText(Path.Combine(
            repoRoot, "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks", "CommerceContext.cs"));
        Assert.DoesNotContain("StoreCommerceContext", buildingBlocks, StringComparison.Ordinal);
        Assert.DoesNotContain("StoreCommerce", buildingBlocks, StringComparison.Ordinal);

        var storeContextRoot = Path.Combine(repoRoot, "src", "backend", "Modules", "StoreContext");
        var contractsRoot = Path.Combine(storeContextRoot, "Tooba.StoreContext.Contracts");
        var infraRoot = Path.Combine(storeContextRoot, "Tooba.StoreContext.Infrastructure");

        Assert.True(Directory.Exists(contractsRoot), contractsRoot);
        Assert.True(Directory.Exists(infraRoot), infraRoot);

        // Infrastructure root may only contain the module composition entry.
        var infraRootFiles = Directory.GetFiles(infraRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "StoreContextModule.cs" }, infraRootFiles);

        // Contracts must not depend on Cart or Host, and must expose the three seams.
        var contractsProject = File.ReadAllText(Path.Combine(contractsRoot, "Tooba.StoreContext.Contracts.csproj"));
        Assert.DoesNotContain("Cart", contractsProject, StringComparison.Ordinal);
        Assert.DoesNotContain("Host", contractsProject, StringComparison.Ordinal);

        var contractsSource = string.Concat(
            Directory.GetFiles(contractsRoot, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
        Assert.Contains("ICurrentStoreCommerceContext", contractsSource, StringComparison.Ordinal);
        Assert.Contains("IStoreCommerceContextAssigner", contractsSource, StringComparison.Ordinal);
        Assert.Contains("IWorkerStoreCommerceContextFactory", contractsSource, StringComparison.Ordinal);

        // Cart consumes StoreContext.Contracts and has no Host dependency.
        var cartInfraProject = File.ReadAllText(Path.Combine(
            repoRoot, "src", "backend", "Modules", "Cart", "Tooba.Cart.Infrastructure", "Tooba.Cart.Infrastructure.csproj"));
        Assert.Contains("Tooba.StoreContext.Contracts", cartInfraProject, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", cartInfraProject, StringComparison.Ordinal);

        // Host adapter must not carry a commerce-authority fallback literal.
        var adapter = File.ReadAllText(Path.Combine(
            repoRoot, "src", "backend", "Host", "Tooba.Host", "Outbox", "OutboxWorkerSeams.cs"));
        Assert.Contains("IWorkerStoreCommerceContextFactory", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("\"IR\"", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("\"IRR\"", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("SalesChannel.Direct", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("SalesChannel.Marketplace", adapter, StringComparison.Ordinal);
    }

    /// <summary>
    /// TB-TMAR-STORECONTEXT-GOLDEN-001 — StoreContext is a golden-certified platform context:
    /// default-currency semantics only, structure-certified under ARCH-COMPLETE-002, no ceremonial
    /// Application/Endpoints/MediatR, and no transaction single-currency invariant.
    /// </summary>
    [Fact]
    public void StoreContext_is_golden_certified_with_default_currency_semantics()
    {
        var repoRoot = FindRepoRoot();
        var storeContextRoot = Path.Combine(repoRoot, "src", "backend", "Modules", "StoreContext");

        // 1. Contract carries DefaultCurrency and no ambiguity-prone Currency.
        var contract = File.ReadAllText(Path.Combine(storeContextRoot, "Tooba.StoreContext.Contracts", "Current", "StoreCommerceContext.cs"));
        Assert.Contains("string? DefaultCurrency", contract, StringComparison.Ordinal);
        Assert.DoesNotContain("string? Currency", contract, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowedCurrencies", contract, StringComparison.Ordinal);
        Assert.DoesNotContain("SettlementCurrency", contract, StringComparison.Ordinal);
        Assert.DoesNotContain("PaymentCurrency", contract, StringComparison.Ordinal);

        // 2. No AllowedCurrencies / settlement / payment currency model anywhere in StoreContext.
        var storeContextSources = Directory
            .EnumerateFiles(storeContextRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Replace('\\', '/').Contains("/bin/", StringComparison.Ordinal)
                        && !p.Replace('\\', '/').Contains("/obj/", StringComparison.Ordinal))
            .ToArray();
        Assert.DoesNotContain(storeContextSources, path =>
            Regex.IsMatch(File.ReadAllText(path), @"\bAllowedCurrencies\b|\bSettlementCurrency\b|\bPaymentCurrency\b"));

        // 3. No ceremonial Application/Endpoints project.
        Assert.False(Directory.Exists(Path.Combine(storeContextRoot, "Tooba.StoreContext.Application")));
        Assert.False(Directory.Exists(Path.Combine(storeContextRoot, "Tooba.StoreContext.Endpoints")));
        Assert.DoesNotContain(storeContextSources, path =>
            File.ReadAllText(path).Contains("MediatR", StringComparison.Ordinal));

        // 4. Contracts root has zero .cs; Infrastructure root only the module entry.
        var contractsRootFiles = Directory.GetFiles(
            Path.Combine(storeContextRoot, "Tooba.StoreContext.Contracts"), "*.cs", SearchOption.TopDirectoryOnly);
        Assert.Empty(contractsRootFiles);

        var infraRootFiles = Directory
            .GetFiles(Path.Combine(storeContextRoot, "Tooba.StoreContext.Infrastructure"), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "StoreContextModule.cs" }, infraRootFiles);

        // 5. Path <-> namespace alignment for the capability folder.
        var contractText = File.ReadAllText(Path.Combine(
            storeContextRoot, "Tooba.StoreContext.Contracts", "Current", "StoreCommerceContext.cs"));
        Assert.Contains("namespace Tooba.StoreContext.Contracts.Current;", contractText, StringComparison.Ordinal);
        var accessorText = File.ReadAllText(Path.Combine(
            storeContextRoot, "Tooba.StoreContext.Infrastructure", "Current", "StoreCommerceContextAccessor.cs"));
        Assert.Contains("namespace Tooba.StoreContext.Infrastructure.Current;", accessorText, StringComparison.Ordinal);

        // 6. Structure manifest declares StoreContext under ARCH-COMPLETE-002.
        var manifest = File.ReadAllText(Path.Combine(repoRoot, "docs", "architecture", "tmar-module-structure-manifests.json"));
        Assert.Contains("\"module\": \"StoreContext\"", manifest, StringComparison.Ordinal);
        Assert.Contains("Tooba.StoreContext.Contracts", manifest, StringComparison.Ordinal);
        Assert.Contains("Tooba.StoreContext.Infrastructure", manifest, StringComparison.Ordinal);

        // 7. SoT certifies StoreContext as an internal platform context, not an HTTP module.
        var sot = File.ReadAllText(Path.Combine(repoRoot, "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("PLATFORM_CONTEXT_REFERENCE_PATTERN", sot, StringComparison.Ordinal);
        Assert.Contains("\"StoreContext\"", sot, StringComparison.Ordinal);
        Assert.Contains("INTERNAL_ONLY", sot, StringComparison.Ordinal);
        Assert.Contains("NOT_APPLICABLE_NO_APPLICATION_USE_CASE", sot, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_configuration_uses_canonical_store_commerce_default_currency_key()
    {
        var repoRoot = FindRepoRoot();
        var options = File.ReadAllText(Path.Combine(
            repoRoot, "src", "backend", "Host", "Tooba.Host", "Configuration", "ToobaPlatformOptions.cs"));
        Assert.Contains("public string? DefaultCurrency { get; set; }", options, StringComparison.Ordinal);
        Assert.DoesNotContain("public string? Currency { get; set; }", options, StringComparison.Ordinal);
        Assert.Contains("StoreCommerce:DefaultCurrency", options, StringComparison.Ordinal);
        Assert.DoesNotContain("StoreCommerce:Currency", options, StringComparison.Ordinal);
        Assert.Contains("Normalize(raw?.DefaultCurrency)", options, StringComparison.Ordinal);

        var devSettings = File.ReadAllText(Path.Combine(
            repoRoot, "src", "backend", "Host", "Tooba.Host", "appsettings.Development.json"));
        Assert.Contains("\"DefaultCurrency\"", devSettings, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Currency\":", devSettings, StringComparison.Ordinal);
    }

    [Fact]
    public void Cart_presentation_has_no_language_hardcoded_fallbacks()
    {
        var composer = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src", "backend", "Modules", "Cart", "Tooba.Cart.Application",
            "Presentation", "CartPresentationComposer.cs"));

        Assert.DoesNotContain("کالا", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("فروشنده", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Product\"", composer, StringComparison.Ordinal);
        Assert.Contains("string.Empty", composer, StringComparison.Ordinal);
        Assert.Contains("ICatalogCartPresentationLookup", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Cart_owns_expiry_reconciliation_and_persistence_policy()
    {
        var cartRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Cart");

        var reconciler = File.ReadAllText(Path.Combine(
            cartRoot, "Tooba.Cart.Infrastructure", "Lifetime", "CartExpiryReconciler.cs"));
        Assert.Contains("IClock", reconciler, StringComparison.Ordinal);
        Assert.Contains("_clock.UtcNow", reconciler, StringComparison.Ordinal);
        Assert.Contains("ExpireDueCartsAsync", reconciler, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", reconciler, StringComparison.Ordinal);

        var policy = File.ReadAllText(Path.Combine(
            cartRoot, "Tooba.Cart.Application", "Ports", "CartPersistenceHours.cs"));
        Assert.Contains("ICartPersistenceHoursResolver", policy, StringComparison.Ordinal);
        Assert.Contains("DefaultHours", policy, StringComparison.Ordinal);

        var source = File.ReadAllText(Path.Combine(
            cartRoot, "Tooba.Cart.Infrastructure", "Lifetime", "CartPersistenceHoursSource.cs"));
        Assert.Contains("CartLifetimeOptions", source, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            cartRoot, "Tooba.Cart.Infrastructure", "DependencyInjection", "CartModule.cs"));
        Assert.Contains("ICartExpiryReconciler, CartExpiryReconciler", module, StringComparison.Ordinal);
        Assert.Contains("ICartPersistenceHoursSource, CartPersistenceHoursSource", module, StringComparison.Ordinal);
        Assert.Contains("Configure<CartLifetimeOptions>", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_has_no_broad_Cart_global_usings_or_duplicate_routes()
    {
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var globalUsings = Directory.EnumerateFiles(hostRoot, "GlobalUsings*.cs", SearchOption.TopDirectoryOnly)
            .Select(File.ReadAllText);
        Assert.DoesNotContain(globalUsings, text => text.Contains("Tooba.Cart.", StringComparison.Ordinal));

        Assert.False(File.Exists(Path.Combine(hostRoot, "GlobalUsings.CartSettlementApp.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "GlobalUsings.CartSettlementDomain.cs")));

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.Contains("MapCartEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/cart\"", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/cart", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPatch(\"/cart", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete(\"/cart", program, StringComparison.Ordinal);
        Assert.DoesNotContain("CartLifetimeOptions", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Cart_owns_its_domain_and_requires_no_Host_orchestrator()
    {
        var cartRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Cart");
        var offenders = new List<string>();
        foreach (var path in EnumerateProductionSources(cartRoot))
        {
            var relative = Path.GetRelativePath(cartRoot, path).Replace('\\', '/');
            var text = File.ReadAllText(path);
            if (text.Contains("ElapsedTime", StringComparison.Ordinal)
                || text.Contains("ICartExpiryOrchestrator", StringComparison.Ordinal)
                || text.Contains("HostDatabase", StringComparison.Ordinal)
                || text.Contains("INpgsqlLastActivityTracker", StringComparison.Ordinal))
            {
                offenders.Add(relative);
            }
        }

        Assert.True(offenders.Count == 0, "Cart internals depending on Host orchestration: " + string.Join("; ", offenders));
    }

    private static IEnumerable<string> EnumerateProductionSources(string root)
    {
        foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var normalized = path.Replace('\\', '/');
            if (normalized.Contains("/bin/", StringComparison.Ordinal)
                || normalized.Contains("/obj/", StringComparison.Ordinal)
                || normalized.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return path;
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "PROJECT-STATE.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo.root.not_found");
    }
}
