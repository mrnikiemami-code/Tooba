using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Returns.Tests.Architecture;

public sealed class ReturnsArchitectureGuardTests
{
    private static readonly string[] AllowedDomainFolders = ["Aggregates", "Entities", "ValueObjects", "Events", "Policies"];
    private static readonly string[] AllowedApplicationFolders = ["Ports", "Models", "Commands", "Queries", "Errors"];
    private static readonly string[] AllowedContractsFolders = ["Events", "Settlement", "Errors"];
    private static readonly string[] AllowedInfrastructureFolders =
        ["Persistence", "Directories", "Adapters", "Events", "Messaging", "DependencyInjection", "Migrations",
            "Gateways", "Bridges", "Evaluators", "Observability", "Queries", "Errors"];
    private static readonly string[] AllowedEndpointsFolders = ["Customer", "Seller", "Admin", "Errors", "Resources"];

    private static readonly HashSet<string> HostDbContextAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
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

        throw new InvalidOperationException("repo.root.not_found");
    }

    private static string ModuleRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Returns");

    [Fact]
    public void Infrastructure_references_foreign_Contracts_only()
    {
        var refs = ProjectRefs("Tooba.Returns.Infrastructure");
        Assert.Contains(refs, r => r.Contains("Order.Contracts", StringComparison.Ordinal));
        Assert.Contains(refs, r => r.Contains("Fulfillment.Contracts", StringComparison.Ordinal));
        Assert.Contains(refs, r => r.Contains("Payment.Contracts", StringComparison.Ordinal));
        Assert.Contains(refs, r => r.Contains("Inventory.Contracts", StringComparison.Ordinal));
        Assert.Contains(refs, r => r.Contains("Wallet.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(refs, r => r.Contains("Order.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Fulfillment.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Payment.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Inventory.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Order.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Fulfillment.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Payment.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Payment.Domain", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("OrderDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("PaymentDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("FulfillmentDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("InventoryDbContext", StringComparison.Ordinal));
    }

    [Fact]
    public void Returns_golden_boundaries_and_physical_layout_remain_clean()
    {
        Assert.DoesNotContain(ProjectRefs("Tooba.Returns.Domain"), x => x.Contains("Tooba.Returns.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        AssertNoRootDump("Tooba.Returns.Domain", AllowedDomainFolders);
        AssertNoRootDump("Tooba.Returns.Application", AllowedApplicationFolders);
        AssertNoRootDump("Tooba.Returns.Contracts", AllowedContractsFolders);
        AssertNoRootDump("Tooba.Returns.Infrastructure", AllowedInfrastructureFolders);
        AssertNoRootDump("Tooba.Returns.Endpoints", AllowedEndpointsFolders);
        AssertNamespacesAlign("Tooba.Returns.Domain", "Tooba.Returns.Domain");
        AssertNamespacesAlign("Tooba.Returns.Application", "Tooba.Returns.Application");
        AssertNamespacesAlign("Tooba.Returns.Contracts", "Tooba.Returns.Contracts");
        AssertNamespacesAlign("Tooba.Returns.Infrastructure", "Tooba.Returns.Infrastructure");
        AssertNamespacesAlign("Tooba.Returns.Endpoints", "Tooba.Returns.Endpoints");

        var directory = File.ReadAllText(Path.Combine(ModuleRoot(), "Tooba.Returns.Infrastructure", "Directories", "ReturnDirectory.cs"));
        Assert.Contains("IClock", directory, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", directory, StringComparison.Ordinal);
        Assert.Contains("IPaymentReturnReader", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new SystemUtcClock()", directory, StringComparison.Ordinal);

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("ReturnsDbContext", StringComparison.Ordinal))
            .Where(path => !HostDbContextAllowlist.Contains(Path.GetFileName(path)))
            .ToList();
        Assert.True(hostHits.Count == 0, "Host ReturnsDbContext allowlist: " + string.Join("; ", hostHits));

        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Returns")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Returns", "ReturnEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Returns", "ReturnPanelComposer.cs")));

        var hostGrid = File.ReadAllText(Path.Combine(hostRoot, "Grid", "AdminListGridPolicies.cs"));
        Assert.DoesNotContain("AdminReturnWorkQueueRow", hostGrid, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminListGridPolicies.Returns", hostGrid, StringComparison.Ordinal);

        var programCs = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.Contains("MapReturnEndpoints()", programCs, StringComparison.Ordinal);
        Assert.DoesNotContain("ReturnPanelComposer", programCs, StringComparison.Ordinal);

        var bypass = AllProductionSources()
            .Where(x => x.Text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("Guid.NewGuid()", StringComparison.Ordinal)
                        || x.Text.Contains("UuidV7.New()", StringComparison.Ordinal)
                        || x.Text.Contains("StartActivity(", StringComparison.Ordinal)
                        || x.Text.Contains("PlatformHttpException", StringComparison.Ordinal))
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
            .Where(x => !x.Msg.StartsWith("returns.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("domain.", StringComparison.Ordinal))
            .Select(x => $"{x.Path}:{x.Msg}")
            .ToList();
        Assert.True(localized.Count == 0, "localized exception prose: " + string.Join("; ", localized));
    }

    [Fact]
    public void Returns_endpoints_cqrs_and_host_ownership_are_enforced()
    {
        Assert.True(Directory.Exists(Path.Combine(ModuleRoot(), "Tooba.Returns.Endpoints")));
        Assert.True(File.Exists(Path.Combine(ModuleRoot(), "Tooba.Returns.Endpoints", "Tooba.Returns.Endpoints.csproj")));

        var customer = File.ReadAllText(Path.Combine(ModuleRoot(), "Tooba.Returns.Endpoints", "Customer", "ReturnCustomerEndpoints.cs"));
        var seller = File.ReadAllText(Path.Combine(ModuleRoot(), "Tooba.Returns.Endpoints", "Seller", "ReturnSellerEndpoints.cs"));
        var admin = File.ReadAllText(Path.Combine(ModuleRoot(), "Tooba.Returns.Endpoints", "Admin", "ReturnAdminEndpoints.cs"));
        var module = File.ReadAllText(Path.Combine(ModuleRoot(), "Tooba.Returns.Endpoints", "ReturnEndpointModule.cs"));

        foreach (var endpoint in new[] { customer, seller, admin })
        {
            Assert.Contains("ISender sender", endpoint, StringComparison.Ordinal);
            Assert.Contains("ApiResponseFactory", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("exception.Message", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("new { title", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("ReturnsDbContext", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("AdminListGridPolicies", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (InvalidOperationException", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("ReturnPanelComposer", endpoint, StringComparison.Ordinal);
        }

        Assert.Contains("MapGroup(\"/v1/customer\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/seller\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/admin\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/returns\"", customer, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/returns\"", customer, StringComparison.Ordinal);
        Assert.Contains("CreateReturnCommand", customer, StringComparison.Ordinal);
        Assert.Contains("ApproveReturnCommand", seller, StringComparison.Ordinal);
        Assert.Contains("RejectReturnCommand", seller, StringComparison.Ordinal);
        Assert.Contains("QueryAdminReturnsGridQuery", admin, StringComparison.Ordinal);
        Assert.Contains("RetryReturnRefundCommand", admin, StringComparison.Ordinal);

        var endpointRefs = ProjectRefs("Tooba.Returns.Endpoints");
        Assert.Contains(endpointRefs, r => r.Contains("Returns.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("DbContext", StringComparison.OrdinalIgnoreCase));

        var application = Sources("Tooba.Returns.Application").ToList();
        Assert.Contains(application, x => x.Text.Contains("CreateReturnCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("ApproveReturnCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("RejectReturnCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("RetryReturnRefundCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("QueryAdminReturnsGridQuery", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("AdminReturnGridQueryPolicy", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("ReturnsExceptionMapper", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("IRequestHandler<", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("using MediatR", StringComparison.Ordinal));
        Assert.DoesNotContain(application, x =>
            x.Text.Contains("StartsWith(\"return.\"", StringComparison.Ordinal)
            || x.Text.Contains("StartsWith(\"returns.\"", StringComparison.Ordinal)
            || x.Text.Contains("StartsWith(\"refund.\"", StringComparison.Ordinal)
            || (x.Text.Contains(".Contains(\"", StringComparison.Ordinal) && x.Path.Contains("ExceptionMapper", StringComparison.Ordinal)));

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        Assert.True(File.Exists(Path.Combine(hostRoot, "Customer", "HostReturnCustomerAuthorizer.cs")));
        Assert.True(File.Exists(Path.Combine(hostRoot, "Seller", "HostReturnSellerAuthorizer.cs")));
        Assert.True(File.Exists(Path.Combine(hostRoot, "Admin", "HostReturnAdminAuthorizer.cs")));
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders)
    {
        var root = Path.Combine(ModuleRoot(), project);
        var rootCs = Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => name is not null
                           && !name.EndsWith("EndpointModule.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.True(rootCs.Length == 0, $"{project} root dumping-ground: " + string.Join(", ", rootCs));
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj" or "artifacts") continue;
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
        Sources("Tooba.Returns.Domain")
            .Concat(Sources("Tooba.Returns.Application"))
            .Concat(Sources("Tooba.Returns.Contracts"))
            .Concat(Sources("Tooba.Returns.Infrastructure"))
            .Concat(Sources("Tooba.Returns.Endpoints"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(ModuleRoot(), projectFolder);
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
        var csproj = Path.Combine(ModuleRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
