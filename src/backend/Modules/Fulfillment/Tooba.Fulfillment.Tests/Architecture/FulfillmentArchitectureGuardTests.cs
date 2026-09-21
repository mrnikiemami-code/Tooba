using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Fulfillment.Tests.Architecture;

public sealed class FulfillmentArchitectureGuardTests
{
    private static readonly string[] AllowedDomainFolders = ["Aggregates", "Entities", "ValueObjects", "Events", "Policies"];
    private static readonly string[] AllowedApplicationFolders = ["Ports", "Models", "Shipping", "Commands", "Queries"];
    private static readonly string[] AllowedContractsFolders = ["Events", "Returns", "Errors"];
    private static readonly string[] AllowedInfrastructureFolders =
        ["Persistence", "Directories", "Adapters", "Events", "Messaging", "DependencyInjection", "Migrations",
            "Gateways", "Bridges", "Handlers", "Shipping", "Observability", "Queries", "Errors"];

    private static readonly HashSet<string> HostDbContextAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Program.cs",
        "ProductWorkspaceDevelopmentBootstrap.cs",
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
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Fulfillment");

    [Fact]
    public void Infrastructure_references_foreign_Contracts_only()
    {
        var refs = ProjectRefs("Tooba.Fulfillment.Infrastructure");
        Assert.Contains(refs, r => r.Contains("Order.Contracts", StringComparison.Ordinal));
        Assert.Contains(refs, r => r.Contains("Inventory.Contracts", StringComparison.Ordinal));
        Assert.Contains(refs, r => r.Contains("Payment.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(refs, r => r.Contains("Order.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Inventory.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Payment.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Order.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Inventory.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Payment.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Order.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Inventory.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Payment.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("OrderDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("InventoryDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("PaymentDbContext", StringComparison.Ordinal));
    }

    [Fact]
    public void Fulfillment_golden_boundaries_and_physical_layout_remain_clean()
    {
        Assert.DoesNotContain(ProjectRefs("Tooba.Fulfillment.Domain"), x => x.Contains("Tooba.Fulfillment.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        AssertNoRootDump("Tooba.Fulfillment.Domain", AllowedDomainFolders);
        AssertNoRootDump("Tooba.Fulfillment.Application", AllowedApplicationFolders);
        AssertNoRootDump("Tooba.Fulfillment.Contracts", AllowedContractsFolders);
        AssertNoRootDump("Tooba.Fulfillment.Infrastructure", AllowedInfrastructureFolders);
        AssertNamespacesAlign("Tooba.Fulfillment.Domain", "Tooba.Fulfillment.Domain");
        AssertNamespacesAlign("Tooba.Fulfillment.Application", "Tooba.Fulfillment.Application");
        AssertNamespacesAlign("Tooba.Fulfillment.Contracts", "Tooba.Fulfillment.Contracts");
        AssertNamespacesAlign("Tooba.Fulfillment.Infrastructure", "Tooba.Fulfillment.Infrastructure");

        var directory = File.ReadAllText(Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Infrastructure", "Directories", "FulfillmentDirectory.cs"));
        Assert.Contains("IClock", directory, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new SystemUtcClock()", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new UuidV7IdGenerator()", directory, StringComparison.Ordinal);

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("FulfillmentDbContext", StringComparison.Ordinal))
            .Where(path => !HostDbContextAllowlist.Contains(Path.GetFileName(path)))
            .ToList();
        Assert.True(hostHits.Count == 0, "Host FulfillmentDbContext allowlist: " + string.Join("; ", hostHits));

        var hostRootForLocator = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var fulfillmentEndpoint = File.ReadAllText(Path.Combine(hostRootForLocator, "Fulfillment", "FulfillmentEndpoints.cs"));
        Assert.DoesNotContain("RequestServices.GetRequiredService", fulfillmentEndpoint, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", fulfillmentEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException(403", fulfillmentEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("detail = ex.Message", fulfillmentEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(new { title", fulfillmentEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", fulfillmentEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("FulfillmentDbContext", fulfillmentEndpoint, StringComparison.Ordinal);
        Assert.Contains("ExecuteAdminFulfillmentBulkCommand", fulfillmentEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminFulfillmentWorkQueueComposer", fulfillmentEndpoint, StringComparison.Ordinal);

        Assert.False(
            File.Exists(Path.Combine(hostRootForLocator, "Admin", "AdminFulfillmentWorkQueueComposer.cs")),
            "AdminFulfillmentWorkQueueComposer must be deleted — bulk ownership is Application-owned.");

        var shippingEndpoint = File.ReadAllText(Path.Combine(hostRootForLocator, "Admin", "ShippingServiceEndpoints.cs"));
        Assert.Contains("ApiResponseFactory", shippingEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(new { title", shippingEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("errorCode = ex.Message", shippingEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (InvalidOperationException ex)", shippingEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("Localization.Application", shippingEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("ILanguageDirectory", shippingEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("LoadDetailAsync", shippingEndpoint, StringComparison.Ordinal);
        Assert.Contains("ListShippingServicesQuery", shippingEndpoint, StringComparison.Ordinal);
        Assert.Contains("GetShippingServiceQuery", shippingEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("ListEnabledMethodsTreeAsync", shippingEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("DefaultColor", shippingEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("DefaultOptions", shippingEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("IShippingCatalogReader", shippingEndpoint, StringComparison.Ordinal);

        Assert.False(
            File.Exists(Path.Combine(hostRootForLocator, "Admin", "HostAdminOrderFulfillmentOperations.cs")),
            "HostAdminOrderFulfillmentOperations must be deleted — Order owns IAdminOrderFulfillmentOperations.");

        var hostAdminSources = Directory.EnumerateFiles(Path.Combine(hostRootForLocator, "Admin"), "*.cs")
            .Select(File.ReadAllText)
            .ToList();
        Assert.DoesNotContain(
            hostAdminSources,
            text => text.Contains(": IAdminOrderFulfillmentOperations", StringComparison.Ordinal)
                    || text.Contains(", IAdminOrderFulfillmentOperations", StringComparison.Ordinal));

        var orderOps = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Fulfillment", "AdminOrderFulfillmentOperations.cs"));
        Assert.Contains("IAdminOrderFulfillmentOperations", orderOps, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", orderOps, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminOrderOperationsComposer", orderOps, StringComparison.Ordinal);

        var treeQuery = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Application", "Shipping", "ListEnabledShippingMethodsTreeQuery.cs"));
        Assert.Contains("ListEnabledShippingMethodsTreeQuery", treeQuery, StringComparison.Ordinal);
        Assert.Contains("ListEnabledShippingMethodsTreeHandler", treeQuery, StringComparison.Ordinal);

        var orderOpsEndpoint = File.ReadAllText(Path.Combine(hostRootForLocator, "Admin", "AdminOrderOperationsEndpoints.cs"));
        Assert.Contains("ListEnabledShippingMethodsTreeQuery", orderOpsEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("ListEnabledMethodsTreeAsync", orderOpsEndpoint, StringComparison.Ordinal);

        var panelComposer = File.ReadAllText(Path.Combine(hostRootForLocator, "Fulfillment", "FulfillmentPanelComposer.cs"));
        Assert.DoesNotContain("FulfillmentDbContext", panelComposer, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", panelComposer, StringComparison.Ordinal);
        Assert.DoesNotContain("PartyDbContext", panelComposer, StringComparison.Ordinal);

        var bypass = AllProductionSources()
            .Where(x => x.Text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("Guid.NewGuid()", StringComparison.Ordinal)
                        || x.Text.Contains("UuidV7.New()", StringComparison.Ordinal)
                        || x.Text.Contains("StartActivity(", StringComparison.Ordinal)
                        || x.Text.Contains("PlatformHttpException", StringComparison.Ordinal)
                        || x.Text.Contains("?? new SystemUtcClock()", StringComparison.Ordinal)
                        || x.Text.Contains("?? new UuidV7IdGenerator()", StringComparison.Ordinal))
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
            .Where(x => !x.Msg.StartsWith("fulfillment.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("shipping_service.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("shipping_service_option.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("domain.", StringComparison.Ordinal))
            .Select(x => $"{x.Path}:{x.Msg}")
            .ToList();
        Assert.True(localized.Count == 0, "localized exception prose: " + string.Join("; ", localized));
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders)
    {
        var root = Path.Combine(ModuleRoot(), project);
        var rootCs = Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).ToArray();
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
        Sources("Tooba.Fulfillment.Domain")
            .Concat(Sources("Tooba.Fulfillment.Application"))
            .Concat(Sources("Tooba.Fulfillment.Contracts"))
            .Concat(Sources("Tooba.Fulfillment.Infrastructure"));

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
