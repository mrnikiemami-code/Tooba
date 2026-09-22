using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Payment.Tests.Architecture;

public sealed class PaymentArchitectureGuardTests
{
    private static readonly string[] AllowedDomainFolders = ["Aggregates", "Entities", "ValueObjects", "Events", "Policies"];
    private static readonly string[] AllowedApplicationFolders = ["Ports", "Models", "Commands", "Queries", "Errors", "Orchestration"];
    private static readonly string[] AllowedContractsFolders = ["Admin", "Customer", "Events", "Dtos", "Hold", "Ports", "Returns", "Settlement", "Storefront"];
    private static readonly string[] AllowedInfrastructureFolders =
        ["Persistence", "Directories", "Adapters", "Providers", "Events", "Messaging", "DependencyInjection", "Gateways", "Migrations"];
    private static readonly string[] AllowedEndpointsFolders = ["Storefront", "Admin", "Webhooks", "Errors", "Resources"];

    private static readonly HashSet<string> HostDbContextAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "MarketplaceDevelopmentBootstrap.cs",
        "ProductWorkspaceDevelopmentBootstrap.cs",
        "Program.cs",
        "ModuleMigrationRegistry.cs",
    };

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md"))
                || Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string PaymentRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Payment");

    [Fact]
    public void Domain_csproj_has_no_application_infrastructure_or_endpoints_reference()
    {
        var refs = ProjectRefs("Tooba.Payment.Domain");
        Assert.DoesNotContain(refs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Endpoints", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Contracts", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Wallet.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Payment_consumes_Wallet_only_through_Contracts()
    {
        foreach (var project in new[] { "Tooba.Payment.Domain", "Tooba.Payment.Application", "Tooba.Payment.Infrastructure", "Tooba.Payment.Endpoints" })
        {
            var refs = ProjectRefs(project);
            Assert.DoesNotContain(refs, r => r.Contains("Wallet.Application", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(refs, r => r.Contains("Wallet.Domain", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(refs, r => r.Contains("Wallet.Infrastructure", StringComparison.OrdinalIgnoreCase));
        }

        var appRefs = ProjectRefs("Tooba.Payment.Application");
        Assert.Contains(appRefs, r => r.Contains("Wallet.Contracts", StringComparison.Ordinal));
        var infraRefs = ProjectRefs("Tooba.Payment.Infrastructure");
        Assert.Contains(infraRefs, r => r.Contains("Wallet.Contracts", StringComparison.Ordinal));

        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("WalletDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Wallet.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Wallet.Domain", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Wallet.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void Payment_golden_boundaries_and_physical_layout_remain_clean()
    {
        Assert.True(Directory.Exists(Path.Combine(PaymentRoot(), "Tooba.Payment.Contracts")));
        Assert.True(Directory.Exists(Path.Combine(PaymentRoot(), "Tooba.Payment.Endpoints")));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));

        var directory = File.ReadAllText(Path.Combine(PaymentRoot(), "Tooba.Payment.Infrastructure", "Directories", "PaymentDirectory.cs"));
        Assert.Contains("IClock", directory, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new SystemUtcClock()", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new UuidV7IdGenerator()", directory, StringComparison.Ordinal);

        var walletGateway = File.ReadAllText(Path.Combine(PaymentRoot(), "Tooba.Payment.Infrastructure", "Providers", "WalletPaymentGateway.cs"));
        Assert.Contains("IWalletOrderPaymentPort", walletGateway, StringComparison.Ordinal);
        Assert.Contains("IModuleCallTracer", walletGateway, StringComparison.Ordinal);
        Assert.Contains("IClock", walletGateway, StringComparison.Ordinal);

        AssertNoRootDump("Tooba.Payment.Domain", AllowedDomainFolders);
        AssertNoRootDump("Tooba.Payment.Application", AllowedApplicationFolders);
        AssertNoRootDump("Tooba.Payment.Contracts", AllowedContractsFolders);
        AssertNoRootDump("Tooba.Payment.Infrastructure", AllowedInfrastructureFolders);
        AssertNoRootDump("Tooba.Payment.Endpoints", AllowedEndpointsFolders);
        AssertNamespacesAlign("Tooba.Payment.Domain", "Tooba.Payment.Domain");
        AssertNamespacesAlign("Tooba.Payment.Application", "Tooba.Payment.Application");
        AssertNamespacesAlign("Tooba.Payment.Contracts", "Tooba.Payment.Contracts");
        AssertNamespacesAlign("Tooba.Payment.Infrastructure", "Tooba.Payment.Infrastructure");
        AssertNamespacesAlign("Tooba.Payment.Endpoints", "Tooba.Payment.Endpoints");

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("PaymentDbContext", StringComparison.Ordinal))
            .Where(path => !HostDbContextAllowlist.Contains(Path.GetFileName(path)))
            .ToList();
        Assert.True(hostHits.Count == 0, "Host PaymentDbContext production authority: " + string.Join("; ", hostHits));

        var bypass = AllProductionSources()
            .Where(x => x.Text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("Guid.NewGuid()", StringComparison.Ordinal)
                        || x.Text.Contains("UuidV7.New()", StringComparison.Ordinal)
                        || x.Text.Contains("StartActivity(", StringComparison.Ordinal)
                        || x.Text.Contains("PlatformHttpException", StringComparison.Ordinal)
                        || x.Text.Contains("?? new SystemUtcClock()", StringComparison.Ordinal)
                        || x.Text.Contains("?? new UuidV7IdGenerator()", StringComparison.Ordinal)
                        || x.Text.Contains("?? new ModuleCallTracer()", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(bypass.Count == 0, string.Join("; ", bypass));

        var silentCatch = AllProductionSources()
            .Where(x => Regex.IsMatch(x.Text, @"catch\s*\(\s*Exception\s*\)\s*\{|catch\s*\{\s*\}|catch\s*\([^)]+\)\s*\{\s*\}", RegexOptions.Multiline))
            .Select(x => x.Path)
            .ToList();
        Assert.True(silentCatch.Count == 0, "silent/empty catch: " + string.Join("; ", silentCatch));

        var localized = AllProductionSources()
            .SelectMany(x => Regex.Matches(x.Text, @"throw new \w+Exception\(\s*""([^""]*)""\s*\)")
                .Select(m => (x.Path, Msg: m.Groups[1].Value)))
            .Where(x => Regex.IsMatch(x.Msg, @"[\u0600-\u06FF]") || x.Msg.Contains(' ', StringComparison.Ordinal))
            .Where(x => !x.Msg.StartsWith("payment.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("admin.payment.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("inventory.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("domain.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("checkout.", StringComparison.Ordinal))
            .Select(x => $"{x.Path}:{x.Msg}")
            .ToList();
        Assert.True(localized.Count == 0, "localized exception prose: " + string.Join("; ", localized));
    }

    [Fact]
    public void Payment_endpoints_cqrs_and_host_ownership_are_enforced()
    {
        Assert.True(Directory.Exists(Path.Combine(PaymentRoot(), "Tooba.Payment.Endpoints")));
        Assert.True(File.Exists(Path.Combine(PaymentRoot(), "Tooba.Payment.Endpoints", "Tooba.Payment.Endpoints.csproj")));

        var storefront = File.ReadAllText(Path.Combine(PaymentRoot(), "Tooba.Payment.Endpoints", "Storefront", "PaymentStorefrontEndpoints.cs"));
        var admin = File.ReadAllText(Path.Combine(PaymentRoot(), "Tooba.Payment.Endpoints", "Admin", "PaymentAdminEndpoints.cs"));
        var webhook = File.ReadAllText(Path.Combine(PaymentRoot(), "Tooba.Payment.Endpoints", "Webhooks", "PaymentWebhookEndpoints.cs"));
        var module = File.ReadAllText(Path.Combine(PaymentRoot(), "Tooba.Payment.Endpoints", "PaymentEndpointModule.cs"));

        foreach (var endpoint in new[] { storefront, admin, webhook })
        {
            Assert.Contains("ISender sender", endpoint, StringComparison.Ordinal);
            Assert.Contains("ApiResponseFactory", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("IPaymentDirectory", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("IPaymentAdminDirectory", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("exception.Message", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("PaymentDbContext", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (InvalidOperationException", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("Guid.NewGuid()", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("DateTimeOffset.UtcNow", endpoint, StringComparison.Ordinal);
        }

        Assert.Contains("MapGroup(\"/v1/storefront\")", storefront, StringComparison.Ordinal);
        Assert.Contains("/payment-methods", storefront, StringComparison.Ordinal);
        Assert.Contains("InitiateStorefrontPaymentCommand", storefront, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/admin\")", admin, StringComparison.Ordinal);
        Assert.Contains("/payments/", admin, StringComparison.Ordinal);
        Assert.Contains("QueryAdminPaymentsGridQuery", admin, StringComparison.Ordinal);
        Assert.Contains("/v1/payments/webhooks/", webhook, StringComparison.Ordinal);
        Assert.Contains("ProcessPaymentWebhookCommand", webhook, StringComparison.Ordinal);
        Assert.Contains("MapPaymentEndpoints", module, StringComparison.Ordinal);

        var endpointRefs = ProjectRefs("Tooba.Payment.Endpoints");
        Assert.Contains(endpointRefs, r => r.Contains("Payment.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));

        var application = Sources("Tooba.Payment.Application").ToList();
        Assert.Contains(application, x => x.Text.Contains("InitiateStorefrontPaymentCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("ProcessPaymentWebhookCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("ReconcileStalePaymentsCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("QueryAdminPaymentsGridQuery", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("IRequestHandler<", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("using MediatR", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("PaymentExceptionMapper", StringComparison.Ordinal));
        Assert.DoesNotContain(application, x =>
            x.Text.Contains(".Contains(\"", StringComparison.Ordinal) && x.Path.Contains("ExceptionMapper", StringComparison.Ordinal));

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        Assert.False(File.Exists(Path.Combine(hostRoot, "Payments", "PaymentWebhookEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "StorefrontPaymentComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Grid", "AdminPaymentsGridQueryEngine.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Admin", "HostPaymentAdminOrderEnrichmentAdapter.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "HostPaymentUnpaidRetrySupplyAdapter.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "HostPaymentProofMediaAdapter.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "HostStorefrontCheckoutPaymentAccessAdapter.cs")));
        Assert.True(File.Exists(Path.Combine(hostRoot, "Storefront", "HostPaymentStorefrontAuthorizer.cs")));
        Assert.True(File.Exists(Path.Combine(hostRoot, "Admin", "HostPaymentAdminAuthorizer.cs")));

        var storefrontHost = File.ReadAllText(Path.Combine(hostRoot, "Storefront", "StorefrontEndpoints.cs"));
        Assert.DoesNotContain("MapPost(\"/checkout/{checkoutId:guid}/payments\"", storefrontHost, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/payment-methods\"", storefrontHost, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPaymentException", storefrontHost, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecutePaymentAsync", storefrontHost, StringComparison.Ordinal);

        var adminHost = File.ReadAllText(Path.Combine(hostRoot, "Admin", "AdminPanelEndpoints.cs"));
        Assert.DoesNotContain("MapGet(\"/payments/{paymentId:guid}\"", adminHost, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/payments/query\"", adminHost, StringComparison.Ordinal);

        var programCs = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.Contains("MapPaymentEndpoints()", programCs, StringComparison.Ordinal);
        Assert.Contains("AddPaymentEndpointPresentation()", programCs, StringComparison.Ordinal);
        Assert.Contains("HostPaymentStorefrontAuthorizer", programCs, StringComparison.Ordinal);
        Assert.Contains("HostPaymentAdminAuthorizer", programCs, StringComparison.Ordinal);
        Assert.Contains("InitiateStorefrontPaymentCommand", programCs, StringComparison.Ordinal);
        var pending = File.ReadAllText(Path.Combine(hostRoot, "Storefront", "StorefrontPendingPaymentComposer.cs"));
        Assert.DoesNotContain("Tooba.Payment.Application.Ports", pending, StringComparison.Ordinal);
        Assert.Contains("Tooba.Payment.Contracts.Storefront", pending, StringComparison.Ordinal);
        var forbiddenHostIdentifiers = new[]
        {
            "IPaymentDirectory", "IPaymentAdminDirectory", "IPaymentQueryDirectory",
            "IPaymentExpiryDirectory", "IOrderPaymentProjection", "IPaymentHoldSettingsDirectory",
        };
        var hostBoundaryViolations = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => Path.GetFileName(path) is not "HostCheckoutActorPolicyAdapter.cs" and not "Program.cs")
            .Select(path => (Path: path, Text: File.ReadAllText(path)))
            .Where(x => x.Text.Contains("using Tooba.Payment.Application.Ports", StringComparison.Ordinal)
                || forbiddenHostIdentifiers.Any(identifier =>
                    Regex.IsMatch(x.Text, $@"\b{Regex.Escape(identifier)}\b")))
            .Select(x => Path.GetRelativePath(hostRoot, x.Path))
            .ToArray();
        Assert.True(hostBoundaryViolations.Length == 0,
            "Host Payment.Application.Ports boundary violations: " + string.Join(", ", hostBoundaryViolations));
        Assert.True(Directory.Exists(Path.Combine(PaymentRoot(), "Tooba.Payment.Application", "Orchestration")));
        Assert.False(File.Exists(Path.Combine(PaymentRoot(), "Tooba.Payment.Application", "Models", "StorefrontPaymentOrchestrator.cs")));

        foreach (var project in new[] { "Tooba.Payment.Application", "Tooba.Payment.Infrastructure" })
        {
            var refs = ProjectRefs(project);
            foreach (var foreign in new[] { "Order", "Media", "Inventory" })
                Assert.DoesNotContain(refs, r => r.Contains($"Tooba.{foreign}.Application", StringComparison.Ordinal)
                    || r.Contains($"Tooba.{foreign}.Infrastructure", StringComparison.Ordinal)
                    || r.Contains($"Tooba.{foreign}.Domain", StringComparison.Ordinal));
        }

        var worker = File.ReadAllText(Path.Combine(hostRoot, "PaymentReconciliationHostedService.cs"));
        Assert.Contains("ReconcileStalePaymentsCommand", worker, StringComparison.Ordinal);
        Assert.Contains("ISender", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("Guid.NewGuid()", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("StartActivity(", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("IPaymentReconciliationDirectory", worker, StringComparison.Ordinal);
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders)
    {
        var root = Path.Combine(PaymentRoot(), project);
        var rootCs = Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => name is not null
                           && !name.EndsWith("EndpointModule.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.True(rootCs.Length == 0, $"{project} root dumping-ground: " + string.Join(", ", rootCs));
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj" or "artifacts" || name.StartsWith('.')) continue;
            Assert.Contains(name, allowedFolders);
        }
    }

    private static void AssertNamespacesAlign(string projectFolder, string nsPrefix)
    {
        var violations = new List<string>();
        foreach (var (path, text) in Sources(projectFolder))
        {
            var ns = Regex.Match(text, @"^namespace\s+([\w.]+)", RegexOptions.Multiline).Groups[1].Value;
            if (string.IsNullOrEmpty(ns) || !ns.StartsWith(nsPrefix, StringComparison.Ordinal))
            {
                violations.Add($"{path}: ns={ns}");
                continue;
            }

            var rel = path.Replace('\\', '/');
            var marker = projectFolder.Replace('\\', '/') + "/";
            var idx = rel.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) continue;
            var under = rel[(idx + marker.Length)..];
            var folder = under.Split('/')[0];
            if (folder.EndsWith(".cs", StringComparison.Ordinal)) continue;
            var expected = nsPrefix + "." + folder;
            if (!ns.StartsWith(expected, StringComparison.Ordinal))
                violations.Add($"{path}: ns={ns} expectedPrefix={expected}");
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    private static IEnumerable<(string Path, string Text)> AllProductionSources() =>
        Sources("Tooba.Payment.Domain")
            .Concat(Sources("Tooba.Payment.Application"))
            .Concat(Sources("Tooba.Payment.Contracts"))
            .Concat(Sources("Tooba.Payment.Infrastructure"))
            .Concat(Sources("Tooba.Payment.Endpoints"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(PaymentRoot(), projectFolder);
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
                continue;
            if (n.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase) || n.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
                continue;
            yield return (Path.GetRelativePath(RepoRoot(), file), File.ReadAllText(file));
        }
    }

    private static IReadOnlyList<string> ProjectRefs(string projectFolder)
    {
        var csproj = Path.Combine(PaymentRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
